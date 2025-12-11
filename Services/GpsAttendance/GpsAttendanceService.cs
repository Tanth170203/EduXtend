using BusinessObject.DTOs.GpsAttendance;
using BusinessObject.Enum;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.GpsAttendance;

/// <summary>
/// Service for GPS-based attendance check-in and check-out
/// ParticipationScore is stored in ActivityAttendances table
/// Student scoring (MovementRecordDetails) is handled when Activity is completed
/// Club scoring (ClubMovementRecordDetails) is handled when Activity is completed
/// </summary>
public class GpsAttendanceService : IGpsAttendanceService
{
    private readonly EduXtendContext _context;
    private readonly IHaversineCalculator _haversineCalculator;
    private readonly ILogger<GpsAttendanceService> _logger;

    public GpsAttendanceService(
        EduXtendContext context, 
        IHaversineCalculator haversineCalculator,
        ILogger<GpsAttendanceService> logger)
    {
        _context = context;
        _haversineCalculator = haversineCalculator;
        _logger = logger;
    }

    public async Task<GpsCheckInResponseDto> CheckInAsync(int userId, GpsCheckInRequestDto request)
    {
        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId);

        if (activity == null)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "Activity not found"
            };
        }

        // Check if GPS check-in is enabled
        if (!activity.IsGpsCheckInEnabled)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "This activity does not support GPS attendance"
            };
        }

        // Check if user is registered for the activity
        var isRegistered = await _context.ActivityRegistrations
            .AnyAsync(r => r.ActivityId == request.ActivityId && r.UserId == userId);

        if (!isRegistered)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "You have not registered for this activity"
            };
        }

        // Check if already checked in
        var existingAttendance = await _context.ActivityAttendances
            .FirstOrDefaultAsync(a => a.ActivityId == request.ActivityId && a.UserId == userId);

        if (existingAttendance != null && existingAttendance.CheckInMethod == "GPS")
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "You have already checked in via GPS for this activity",
                CheckedAt = existingAttendance.CheckedAt
            };
        }

        // Check if within check-in time window (first 10 minutes after start)
        // Use local time since activity times are stored in local timezone
        var now = DateTime.Now;
        var checkInWindowEnd = activity.StartTime.AddMinutes(activity.CheckInWindowMinutes);

        if (now < activity.StartTime)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = $"Activity has not started yet. Starts at: {activity.StartTime:HH:mm dd/MM/yyyy}"
            };
        }

        if (now > checkInWindowEnd)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = $"Check-in window has closed. Check-in time: {activity.StartTime:HH:mm} - {checkInWindowEnd:HH:mm}"
            };
        }

        // Validate coordinates
        if (!ValidateCoordinates(request.Latitude, request.Longitude))
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "Invalid GPS coordinates"
            };
        }

        // Check if activity has GPS coordinates configured
        if (!activity.GpsLatitude.HasValue || !activity.GpsLongitude.HasValue)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = "Activity GPS location has not been configured"
            };
        }

        // Calculate distance using Haversine formula
        var distance = _haversineCalculator.CalculateDistance(
            activity.GpsLatitude.Value, activity.GpsLongitude.Value,
            request.Latitude, request.Longitude);

        var activityLocation = new GpsCoordinateDto
        {
            Latitude = activity.GpsLatitude.Value,
            Longitude = activity.GpsLongitude.Value
        };

        var userLocation = new GpsCoordinateDto
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        // Check if within allowed radius
        if (distance > activity.GpsCheckInRadius)
        {
            return new GpsCheckInResponseDto
            {
                Success = false,
                Message = $"You are {distance:F0}m away from the location (allowed: {activity.GpsCheckInRadius}m)",
                DistanceMeters = distance,
                AllowedRadius = activity.GpsCheckInRadius,
                ActivityLocation = activityLocation,
                UserLocation = userLocation
            };
        }

        // Create or update attendance record
        // GPS attendance requires BOTH check-in AND check-out to be considered successful
        // IsPresent = false until check-out is completed
        if (existingAttendance == null)
        {
            existingAttendance = new ActivityAttendance
            {
                ActivityId = request.ActivityId,
                UserId = userId,
                IsPresent = false, // Not present until check-out completed
                CheckedAt = now,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                CheckInAccuracy = request.Accuracy,
                DistanceFromActivity = distance,
                CheckInMethod = "GPS",
                ParticipationScore = 5, // Score will only be awarded after check-out
                CheckedById = null // Self check-in, no admin/manager
            };
            _context.ActivityAttendances.Add(existingAttendance);
        }
        else
        {
            // Update existing attendance with GPS data
            existingAttendance.CheckInLatitude = request.Latitude;
            existingAttendance.CheckInLongitude = request.Longitude;
            existingAttendance.CheckInAccuracy = request.Accuracy;
            existingAttendance.DistanceFromActivity = distance;
            existingAttendance.CheckInMethod = "GPS";
            existingAttendance.CheckedAt = now;
            existingAttendance.IsPresent = false; // Not present until check-out completed
            existingAttendance.ParticipationScore = 5;
        }

        await _context.SaveChangesAsync();

        // Note: GPS attendance requires BOTH check-in AND check-out
        // IsPresent will be set to true only after successful check-out
        // ParticipationScore will be added to MovementRecordDetails when Activity is completed
        _logger.LogInformation("[GPS CHECK-IN] Check-in recorded for UserId={UserId}, ActivityId={ActivityId}. Attendance will be marked present after check-out.", 
            userId, request.ActivityId);

        return new GpsCheckInResponseDto
        {
            Success = true,
            Message = "Check-in successful!",
            DistanceMeters = distance,
            AllowedRadius = activity.GpsCheckInRadius,
            CheckedAt = now,
            ActivityLocation = activityLocation,
            UserLocation = userLocation
        };
    }


    public async Task<GpsCheckOutResponseDto> CheckOutAsync(int userId, GpsCheckOutRequestDto request)
    {
        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId);

        if (activity == null)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "Activity not found"
            };
        }

        // Check if GPS check-in is enabled
        if (!activity.IsGpsCheckInEnabled)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "This activity does not support GPS attendance"
            };
        }

        // Check if user has checked in
        var attendance = await _context.ActivityAttendances
            .FirstOrDefaultAsync(a => a.ActivityId == request.ActivityId && a.UserId == userId);

        if (attendance == null || attendance.CheckInMethod != "GPS")
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "You have not checked in via GPS for this activity"
            };
        }

        // Check if already checked out
        if (attendance.CheckOutTime.HasValue)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "You have already checked out",
                CheckedOutAt = attendance.CheckOutTime,
                CheckedInAt = attendance.CheckedAt
            };
        }

        // Check if within check-out time window (last 10 minutes before end)
        // Use local time since activity times are stored in local timezone
        var now = DateTime.Now;
        var checkOutWindowStart = activity.EndTime.AddMinutes(-activity.CheckOutWindowMinutes);

        if (now < checkOutWindowStart)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = $"Check-out window not open yet. Check-out time: {checkOutWindowStart:HH:mm} - {activity.EndTime:HH:mm}"
            };
        }

        if (now > activity.EndTime)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "Activity has ended"
            };
        }

        // Validate coordinates
        if (!ValidateCoordinates(request.Latitude, request.Longitude))
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "Invalid GPS coordinates"
            };
        }

        // Check if activity has GPS coordinates configured
        if (!activity.GpsLatitude.HasValue || !activity.GpsLongitude.HasValue)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = "Activity GPS location has not been configured"
            };
        }

        // Calculate distance using Haversine formula
        var distance = _haversineCalculator.CalculateDistance(
            activity.GpsLatitude.Value, activity.GpsLongitude.Value,
            request.Latitude, request.Longitude);

        var activityLocation = new GpsCoordinateDto
        {
            Latitude = activity.GpsLatitude.Value,
            Longitude = activity.GpsLongitude.Value
        };

        var userLocation = new GpsCoordinateDto
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        // Check if within allowed radius
        if (distance > activity.GpsCheckInRadius)
        {
            return new GpsCheckOutResponseDto
            {
                Success = false,
                Message = $"You are {distance:F0}m away from the location (allowed: {activity.GpsCheckInRadius}m)",
                DistanceMeters = distance,
                AllowedRadius = activity.GpsCheckInRadius,
                ActivityLocation = activityLocation,
                UserLocation = userLocation
            };
        }

        // Update attendance record with check-out data
        // GPS attendance is now complete - mark as present
        attendance.CheckOutLatitude = request.Latitude;
        attendance.CheckOutLongitude = request.Longitude;
        attendance.CheckOutAccuracy = request.Accuracy;
        attendance.CheckOutTime = now;
        attendance.IsPresent = true; // Now mark as present after successful check-out

        await _context.SaveChangesAsync();

        _logger.LogInformation("[GPS CHECK-OUT] Attendance completed for UserId={UserId}, ActivityId={ActivityId}. IsPresent=true, ParticipationScore={Score}. Score will be awarded when activity completes.", 
            userId, request.ActivityId, attendance.ParticipationScore);

        return new GpsCheckOutResponseDto
        {
            Success = true,
            Message = "Check-out successful! Attendance recorded.",
            DistanceMeters = distance,
            AllowedRadius = activity.GpsCheckInRadius,
            CheckedOutAt = now,
            CheckedInAt = attendance.CheckedAt,
            ActivityLocation = activityLocation,
            UserLocation = userLocation
        };
    }


    public async Task<bool> ConfigureActivityGpsAsync(ActivityGpsConfigDto config)
    {
        var activity = await _context.Activities.FindAsync(config.ActivityId);
        if (activity == null) return false;

        // Validate coordinates if enabling GPS
        if (config.IsEnabled)
        {
            if (!config.Latitude.HasValue || !config.Longitude.HasValue)
                return false;

            if (!ValidateCoordinates(config.Latitude.Value, config.Longitude.Value))
                return false;

            if (!ValidateRadius(config.Radius))
                return false;
        }

        activity.GpsLatitude = config.Latitude;
        activity.GpsLongitude = config.Longitude;
        activity.GpsCheckInRadius = config.Radius;
        activity.IsGpsCheckInEnabled = config.IsEnabled;
        activity.CheckInWindowMinutes = config.CheckInWindowMinutes;
        activity.CheckOutWindowMinutes = config.CheckOutWindowMinutes;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ActivityGpsConfigDto?> GetGpsConfigAsync(int activityId)
    {
        var activity = await _context.Activities.FindAsync(activityId);
        if (activity == null) return null;

        return new ActivityGpsConfigDto
        {
            ActivityId = activity.Id,
            Latitude = activity.GpsLatitude,
            Longitude = activity.GpsLongitude,
            Radius = activity.GpsCheckInRadius,
            IsEnabled = activity.IsGpsCheckInEnabled,
            CheckInWindowMinutes = activity.CheckInWindowMinutes,
            CheckOutWindowMinutes = activity.CheckOutWindowMinutes
        };
    }

    public async Task<GpsAttendanceStatusDto?> GetAttendanceStatusAsync(int userId, int activityId)
    {
        var activity = await _context.Activities.FindAsync(activityId);
        if (activity == null) return null;

        var attendance = await _context.ActivityAttendances
            .FirstOrDefaultAsync(a => a.ActivityId == activityId && a.UserId == userId);

        // Use local time since activity times are stored in local timezone
        var now = DateTime.Now;
        var checkInWindowStart = activity.StartTime;
        var checkInWindowEnd = activity.StartTime.AddMinutes(activity.CheckInWindowMinutes);
        var checkOutWindowStart = activity.EndTime.AddMinutes(-activity.CheckOutWindowMinutes);
        var checkOutWindowEnd = activity.EndTime;

        var isCheckInWindowOpen = now >= checkInWindowStart && now <= checkInWindowEnd;
        var isCheckOutWindowOpen = now >= checkOutWindowStart && now <= checkOutWindowEnd;

        int? checkInRemainingSeconds = null;
        int? checkOutRemainingSeconds = null;

        if (isCheckInWindowOpen)
        {
            checkInRemainingSeconds = (int)(checkInWindowEnd - now).TotalSeconds;
        }

        if (isCheckOutWindowOpen)
        {
            checkOutRemainingSeconds = (int)(checkOutWindowEnd - now).TotalSeconds;
        }

        return new GpsAttendanceStatusDto
        {
            ActivityId = activity.Id,
            ActivityTitle = activity.Title,
            ActivityStartTime = activity.StartTime,
            ActivityEndTime = activity.EndTime,
            IsGpsCheckInEnabled = activity.IsGpsCheckInEnabled,
            ActivityLocation = activity.GpsLatitude.HasValue && activity.GpsLongitude.HasValue
                ? new GpsCoordinateDto { Latitude = activity.GpsLatitude.Value, Longitude = activity.GpsLongitude.Value }
                : null,
            AllowedRadius = activity.GpsCheckInRadius,
            HasCheckedIn = attendance?.CheckInMethod == "GPS",
            CheckInTime = attendance?.CheckedAt,
            CheckInDistance = attendance?.DistanceFromActivity,
            HasCheckedOut = attendance?.CheckOutTime.HasValue ?? false,
            CheckOutTime = attendance?.CheckOutTime,
            IsCheckInWindowOpen = isCheckInWindowOpen,
            IsCheckOutWindowOpen = isCheckOutWindowOpen,
            CheckInWindowStart = checkInWindowStart,
            CheckInWindowEnd = checkInWindowEnd,
            CheckOutWindowStart = checkOutWindowStart,
            CheckOutWindowEnd = checkOutWindowEnd,
            CheckInRemainingSeconds = checkInRemainingSeconds,
            CheckOutRemainingSeconds = checkOutRemainingSeconds
        };
    }

    public bool ValidateCoordinates(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 &&
               longitude >= -180 && longitude <= 180;
    }

    public bool ValidateRadius(int radius)
    {
        return radius >= 50 && radius <= 1000;
    }
}
