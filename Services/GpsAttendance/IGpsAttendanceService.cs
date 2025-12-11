using BusinessObject.DTOs.GpsAttendance;

namespace Services.GpsAttendance;

/// <summary>
/// Service for GPS-based attendance check-in and check-out
/// </summary>
public interface IGpsAttendanceService
{
    /// <summary>
    /// Check-in to an activity using GPS coordinates
    /// Check-in is allowed within the first 10 minutes after activity starts
    /// </summary>
    Task<GpsCheckInResponseDto> CheckInAsync(int userId, GpsCheckInRequestDto request);

    /// <summary>
    /// Check-out from an activity using GPS coordinates
    /// Check-out is allowed within the last 10 minutes before activity ends
    /// </summary>
    Task<GpsCheckOutResponseDto> CheckOutAsync(int userId, GpsCheckOutRequestDto request);

    /// <summary>
    /// Configure GPS settings for an activity
    /// </summary>
    Task<bool> ConfigureActivityGpsAsync(ActivityGpsConfigDto config);

    /// <summary>
    /// Get GPS configuration for an activity
    /// </summary>
    Task<ActivityGpsConfigDto?> GetGpsConfigAsync(int activityId);

    /// <summary>
    /// Get current attendance status for a user and activity
    /// </summary>
    Task<GpsAttendanceStatusDto?> GetAttendanceStatusAsync(int userId, int activityId);

    /// <summary>
    /// Validate GPS coordinates
    /// </summary>
    bool ValidateCoordinates(double latitude, double longitude);

    /// <summary>
    /// Validate check-in radius
    /// </summary>
    bool ValidateRadius(int radius);
}
