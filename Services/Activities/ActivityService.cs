using BusinessObject.DTOs.Activity;
using BusinessObject.Models;
using BusinessObject.Enum;
using Repositories.Activities;
using Repositories.Students;
using Repositories.Clubs;
using Repositories.ActivitySchedules;
using Repositories.ActivityScheduleAssignments;
using Services.MovementRecords;
using Services.ClubMovementRecords;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Services.Activities
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repo;
        private readonly IStudentRepository _studentRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IActivityScheduleRepository _scheduleRepo;
        private readonly IActivityScheduleAssignmentRepository _assignmentRepo;
        private readonly IMovementRecordService _movementRecordService;
        private readonly IClubMovementRecordService _clubMovementRecordService;
        private readonly ILogger<ActivityService> _logger;
        
        public ActivityService(
            IActivityRepository repo,
            IStudentRepository studentRepo,
            IClubRepository clubRepo,
            IActivityScheduleRepository scheduleRepo,
            IActivityScheduleAssignmentRepository assignmentRepo,
            IMovementRecordService movementRecordService,
            IClubMovementRecordService clubMovementRecordService,
            ILogger<ActivityService> logger)
        {
            _repo = repo;
            _studentRepo = studentRepo;
            _clubRepo = clubRepo;
            _scheduleRepo = scheduleRepo;
            _assignmentRepo = assignmentRepo;
            _movementRecordService = movementRecordService;
            _clubMovementRecordService = clubMovementRecordService;
            _logger = logger;
        }

        /// <summary>
        /// Auto-complete activity if EndTime has passed and status is "Approved"
        /// This ensures immediate completion without waiting for background service
        /// </summary>
        private async Task<Activity?> AutoCompleteIfEndedAsync(Activity? activity)
        {
            if (activity == null) return null;
            
            // Check if activity should be auto-completed
            if (activity.Status == "Approved" && activity.EndTime < DateTime.Now)
            {
                _logger.LogInformation("[AUTO-COMPLETE] Activity {ActivityId} has ended (EndTime: {EndTime}). Auto-completing...", 
                    activity.Id, activity.EndTime);
                
                try
                {
                    // Complete the activity and award points
                    var result = await CompleteActivityAsync(activity.Id, 0); // userId = 0 for system
                    
                    if (result.success)
                    {
                        _logger.LogInformation("[AUTO-COMPLETE] Activity {ActivityId} completed successfully. ClubPoints: {ClubPoints}", 
                            activity.Id, result.organizingClubPoints);
                        
                        // Reload activity to get updated status
                        activity = await _repo.GetByIdAsync(activity.Id);
                    }
                    else
                    {
                        _logger.LogWarning("[AUTO-COMPLETE] Failed to auto-complete Activity {ActivityId}: {Message}", 
                            activity.Id, result.message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTO-COMPLETE] Error auto-completing Activity {ActivityId}", activity.Id);
                }
            }
            
            return activity;
        }

        private async Task<string> GenerateAttendanceCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var code = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
                    
                var exists = await _repo.IsAttendanceCodeExistsAsync(code);
                if (!exists) return code;
            }
            
            throw new Exception("Failed to generate unique attendance code after 5 attempts");
        }

        public async Task<List<ActivityListItemDto>> GetAllActivitiesAsync()
        {
            var activities = await _repo.GetAllAsync();
            return await MapToListDto(activities);
        }

        public async Task<List<ActivityListItemDto>> SearchActivitiesAsync(
            string? searchTerm, 
            string? type, 
            string? status, 
            bool? isPublic, 
            int? clubId)
        {
            var activities = await _repo.SearchActivitiesAsync(searchTerm, type, status, isPublic, clubId);
            return await MapToListDto(activities);
        }

        public async Task<BusinessObject.DTOs.Common.PaginatedResultDto<ActivityListItemDto>> SearchActivitiesAsync(
            string? searchTerm, 
            string? type, 
            string? status, 
            bool? isPublic, 
            int? clubId,
            int page,
            int pageSize)
        {
            // Get total count first
            var allActivities = await _repo.SearchActivitiesAsync(searchTerm, type, status, isPublic, clubId);
            var totalCount = allActivities.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Ensure page is within bounds
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Get paginated data
            var pagedActivities = allActivities
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var items = await MapToListDto(pagedActivities);
            
            return new BusinessObject.DTOs.Common.PaginatedResultDto<ActivityListItemDto>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<ActivityDetailDto?> GetActivityByIdAsync(int id)
        {
            var activity = await _repo.GetByIdWithDetailsAsync(id);
            if (activity == null) return null;

            // Auto-complete if activity has ended
            activity = await AutoCompleteIfEndedAsync(activity);
            if (activity == null) return null;

            var registrationCount = await _repo.GetRegistrationCountAsync(id);
            var attendanceCount = await _repo.GetAttendanceCountAsync(id);
            var feedbackCount = await _repo.GetFeedbackCountAsync(id);
            var hasEvaluation = await _repo.HasEvaluationAsync(id);

            // Determine if user can register (allow during upcoming and ongoing)
            var isFullDetail = activity.MaxParticipants.HasValue && registrationCount >= activity.MaxParticipants.Value;
            var canRegister = activity.Status == "Approved" &&
                              activity.EndTime > DateTime.UtcNow &&
                              !isFullDetail;

            // Load schedules for complex activities
            List<ActivityScheduleDto>? schedules = null;
            if (IsComplexActivity(activity.Type))
            {
                schedules = await GetActivitySchedulesAsync(id);
            }

            return new ActivityDetailDto
            {
                Id = activity.Id,
                Title = activity.Title,
                Description = activity.Description,
                Location = activity.Location,
                ImageUrl = activity.ImageUrl,
                StartTime = activity.StartTime,
                EndTime = activity.EndTime,
                Type = activity.Type.ToString(),
                Status = activity.Status,
                MovementPoint = activity.MovementPoint,
                MaxParticipants = activity.MaxParticipants,
                CurrentParticipants = registrationCount,
                IsPublic = activity.IsPublic,
                RequiresApproval = activity.RequiresApproval,
                CreatedAt = activity.CreatedAt,
                ApprovedAt = activity.ApprovedAt,
                ClubId = activity.ClubId,
                ClubName = activity.Club?.Name,
                ClubLogo = activity.Club?.LogoUrl,
                ClubBanner = activity.Club?.BannerUrl,
                // Collaboration fields
                ClubCollaborationId = activity.ClubCollaborationId,
                CollaboratingClubName = activity.CollaboratingClub?.Name,
                CollaborationPoint = activity.CollaborationPoint,
                CollaborationStatus = activity.CollaborationStatus,
                CollaborationRejectionReason = activity.CollaborationRejectionReason,
                CreatedById = activity.CreatedById,
                CreatedByName = activity.CreatedBy.FullName,
                ApprovedById = activity.ApprovedById,
                ApprovedByName = activity.ApprovedBy?.FullName,
                RejectionReason = activity.RejectionReason,
                RegisteredCount = registrationCount,
                AttendedCount = attendanceCount,
                FeedbackCount = feedbackCount,
                CanRegister = canRegister,
                IsRegistered = false,
                HasAttended = false,
                AttendanceCode = activity.AttendanceCode,
                HasEvaluation = hasEvaluation,
                Schedules = schedules
            };
        }

	public async Task<ActivityDetailDto?> GetActivityByIdAsync(int id, int? currentUserId)
	{
		var basic = await GetActivityByIdAsync(id);
		if (basic == null) return null;

		if (currentUserId.HasValue)
		{
			basic.IsRegistered = await _repo.IsRegisteredAsync(id, currentUserId.Value);
			// Consider user "attended state" as: has any attendance record (present or absent)
			basic.HasAttended = await _repo.HasAnyAttendanceRecordAsync(id, currentUserId.Value);
			
			// Get the full activity to check collaboration eligibility
			var activity = await _repo.GetByIdAsync(id);
			if (activity != null && !basic.IsPublic)
			{
				var canRegister = await CanUserRegisterAsync(currentUserId.Value, activity);
				if (!canRegister)
				{
					basic.CanRegister = false;
				}
			}
			
			// Load schedules for complex activities
			if (activity != null && IsComplexActivity(activity.Type))
			{
				basic.Schedules = await GetActivitySchedulesAsync(id);
			}
		}
		else
		{
			// Load schedules for complex activities even without user context
			var activity = await _repo.GetByIdAsync(id);
			if (activity != null && IsComplexActivity(activity.Type))
			{
				basic.Schedules = await GetActivitySchedulesAsync(id);
			}
		}

		return basic;
	}

        public async Task<List<ActivityListItemDto>> GetActivitiesByClubIdAsync(int clubId)
        {
            var activities = await _repo.GetActivitiesByClubIdAsync(clubId);
            return await MapToListDto(activities);
        }

        public async Task<ActivityDetailDto> AdminCreateAsync(int adminUserId, AdminCreateActivityDto dto)
        {
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            // Validate collaboration settings
            await ValidateCollaborationSettingsAsync(
                dto.Type, 
                "Admin", 
                null, // Admin activities don't have organizing club
                dto.ClubCollaborationId, 
                dto.CollaborationPoint, 
                dto.MovementPoint);

            // Generate unique attendance code
            var attendanceCode = await GenerateAttendanceCodeAsync();

            var activity = new Activity
            {
                // Admin-created activity: ClubId null, approval fields null
                ClubId = null,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                ImageUrl = dto.ImageUrl,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Type = dto.Type,
                RequiresApproval = false, // Admin-created path has no approval
                CreatedById = adminUserId,
                IsPublic = dto.IsPublic,
                Status = "Approved", // Admin activities are considered approved
                ApprovedById = null,
                ApprovedAt = null,
                MaxParticipants = dto.MaxParticipants,
                MovementPoint = dto.MovementPoint,
                AttendanceCode = attendanceCode,
                CreatedAt = DateTime.UtcNow,
                // Collaboration fields
                ClubCollaborationId = dto.ClubCollaborationId,
                CollaborationPoint = dto.CollaborationPoint,
                // GPS Location fields for GPS-based attendance
                GpsLatitude = dto.GpsLatitude,
                GpsLongitude = dto.GpsLongitude,
                // GPS Check-in configuration
                IsGpsCheckInEnabled = dto.IsGpsCheckInEnabled && dto.GpsLatitude.HasValue && dto.GpsLongitude.HasValue,
                GpsCheckInRadius = dto.GpsCheckInRadius > 0 ? dto.GpsCheckInRadius : 100,
                CheckInWindowMinutes = dto.CheckInWindowMinutes > 0 ? dto.CheckInWindowMinutes : 10,
                CheckOutWindowMinutes = dto.CheckOutWindowMinutes > 0 ? dto.CheckOutWindowMinutes : 10
            };

            var created = await _repo.CreateAsync(activity);
            var detail = await GetActivityByIdAsync(created.Id);
            return detail!;
        }

        public async Task<ActivityDetailDto?> AdminUpdateAsync(int adminUserId, int id, AdminUpdateActivityDto dto)
        {
            if (id != dto.Id) throw new ArgumentException("ID mismatch");
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            // Validate collaboration settings
            await ValidateCollaborationSettingsAsync(
                dto.Type, 
                "Admin", 
                null, // Admin activities don't have organizing club
                dto.ClubCollaborationId, 
                dto.CollaborationPoint, 
                dto.MovementPoint);

            // Keep admin rules: cleared approval/club fields, keep CreatedById as original or set to admin if missing
            existing.ClubId = null;
            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.Location = dto.Location;
            existing.ImageUrl = dto.ImageUrl;
            existing.StartTime = dto.StartTime;
            existing.EndTime = dto.EndTime;
            existing.Type = dto.Type;
            existing.RequiresApproval = false;
            existing.CreatedById = existing.CreatedById == 0 ? adminUserId : existing.CreatedById;
            existing.IsPublic = dto.IsPublic;
            existing.Status = "Approved"; // stay approved
            existing.ApprovedById = null;
            existing.ApprovedAt = null;
            existing.MaxParticipants = dto.MaxParticipants;
            existing.MovementPoint = dto.MovementPoint;
            
            // Update collaboration fields
            existing.ClubCollaborationId = dto.ClubCollaborationId;
            existing.CollaborationPoint = dto.CollaborationPoint;

            await _repo.UpdateAsync(existing);
            return await GetActivityByIdAsync(existing.Id);
        }

        public async Task<bool> AdminDeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<ActivityDetailDto?> ApproveActivityAsync(int adminUserId, int activityId)
        {
            var existing = await _repo.GetByIdAsync(activityId);
            if (existing == null) return null;

            existing.Status = "Approved";
            existing.ApprovedById = adminUserId;
            existing.ApprovedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(existing);
            return await GetActivityByIdAsync(existing.Id);
        }

        public async Task<ActivityDetailDto?> RejectActivityAsync(int adminUserId, int activityId, string rejectionReason)
        {
            var existing = await _repo.GetByIdAsync(activityId);
            if (existing == null) return null;

            existing.Status = "Rejected";
            existing.ApprovedById = adminUserId;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.RejectionReason = rejectionReason;

            await _repo.UpdateAsync(existing);
            return await GetActivityByIdAsync(existing.Id);
        }

        // ================== CLUB MANAGER CRUD ==================
        public async Task<List<ActivityListItemDto>> GetActivitiesByManagerIdAsync(int managerUserId)
        {
            // Get manager's club ID from Club table
            var managedClub = await _clubRepo.GetManagedClubByUserIdAsync(managerUserId);
            if (managedClub == null)
            {
                // Manager doesn't manage any club
                return new List<ActivityListItemDto>();
            }
            
            var clubId = managedClub.Id;
            
            // Get all activities where:
            // 1. ClubId = clubId (all activities owned by this club, regardless of which manager created them)
            // 2. OR ClubCollaborationId = clubId AND CollaborationStatus = "Accepted" (collaborated activities)
            var allActivities = await _repo.GetAllAsync();
            var managerActivities = allActivities.Where(a => 
                (a.ClubId == clubId) ||
                (a.ClubCollaborationId == clubId && a.CollaborationStatus == "Accepted")
            ).ToList();
            
            return await MapToListDtoForManager(managerActivities, clubId);
        }
        
        private async Task<List<ActivityListItemDto>> MapToListDtoForManager(List<Activity> activities, int managerClubId)
        {
            var result = new List<ActivityListItemDto>();
            
            foreach (var activity in activities)
            {
                var registrationCount = await _repo.GetRegistrationCountAsync(activity.Id);
                var attendanceCount = await _repo.GetAttendanceCountAsync(activity.Id);
                var isFull = activity.MaxParticipants.HasValue && registrationCount >= activity.MaxParticipants.Value;
                
                // Check if this is a collaborated activity (current club is the collaborating club, not owner)
                bool isCollaborated = activity.ClubCollaborationId == managerClubId && activity.ClubId != managerClubId;
                
                result.Add(new ActivityListItemDto
                {
                    Id = activity.Id,
                    Title = activity.Title,
                    Description = activity.Description,
                    Location = activity.Location,
                    ImageUrl = activity.ImageUrl,
                    StartTime = activity.StartTime,
                    EndTime = activity.EndTime,
                    Type = activity.Type.ToString(),
                    Status = activity.Status,
                    MovementPoint = activity.MovementPoint,
                    MaxParticipants = activity.MaxParticipants,
                    CurrentParticipants = registrationCount,
                    IsPublic = activity.IsPublic,
                    RequiresApproval = activity.RequiresApproval,
                    ClubId = activity.ClubId,
                    ClubName = activity.Club?.Name,
                    ClubLogo = activity.Club?.LogoUrl,
                    ClubCollaborationId = activity.ClubCollaborationId,
                    CollaboratingClubName = activity.CollaboratingClub?.Name,
                    CollaborationPoint = activity.CollaborationPoint,
                    CollaborationStatus = activity.CollaborationStatus,
                    CollaborationRejectionReason = activity.CollaborationRejectionReason,
                    IsCollaboratedActivity = isCollaborated,
                    AttendanceCode = activity.AttendanceCode,
                    CanRegister = false,
                    IsRegistered = false,
                    IsFull = isFull,
                    HasAttended = false,
                    HasFeedback = false,
                    AttendedCount = attendanceCount
                });
            }
            
            return result;
        }

        public async Task<ActivityDetailDto> ClubCreateAsync(int managerUserId, int clubId, ClubCreateActivityDto dto)
        {
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            // Log GPS config for debugging
            _logger.LogInformation("[ClubCreateAsync] GPS Config from DTO: Lat={Lat}, Lng={Lng}, Enabled={Enabled}, Radius={Radius}, CheckIn={CheckIn}, CheckOut={CheckOut}",
                dto.GpsLatitude, dto.GpsLongitude, dto.IsGpsCheckInEnabled,
                dto.GpsCheckInRadius, dto.CheckInWindowMinutes, dto.CheckOutWindowMinutes);

            // Validate collaboration settings
            await ValidateCollaborationSettingsAsync(
                dto.Type, 
                "ClubManager", 
                clubId,
                dto.ClubCollaborationId, 
                dto.CollaborationPoint, 
                dto.MovementPoint);

            // Check if activity type is Club Activity (internal activities)
            bool isClubActivity = dto.Type == ActivityType.ClubMeeting || 
                                 dto.Type == ActivityType.ClubTraining || 
                                 dto.Type == ActivityType.ClubWorkshop;

            // Generate unique attendance code
            var attendanceCode = await GenerateAttendanceCodeAsync();

            var activity = new Activity
            {
                ClubId = clubId,
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                ImageUrl = dto.ImageUrl,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Type = dto.Type,
                RequiresApproval = !isClubActivity, // Club internal activities don't require approval
                CreatedById = managerUserId,
                IsPublic = dto.IsPublic,
                // For collaboration activities, wait for partner acceptance before admin approval
                Status = isClubActivity ? "Approved" : 
                        (dto.ClubCollaborationId.HasValue ? "PendingCollaboration" : "PendingApproval"),
                MaxParticipants = dto.MaxParticipants,
                MovementPoint = dto.MovementPoint,
                AttendanceCode = attendanceCode,
                CreatedAt = DateTime.UtcNow,
                // Collaboration fields
                ClubCollaborationId = dto.ClubCollaborationId,
                CollaborationPoint = dto.CollaborationPoint,
                // Set collaboration status to Pending if a club is invited
                CollaborationStatus = dto.ClubCollaborationId.HasValue ? "Pending" : null,
                // GPS Location fields for GPS-based attendance
                GpsLatitude = dto.GpsLatitude,
                GpsLongitude = dto.GpsLongitude,
                // GPS Check-in configuration
                IsGpsCheckInEnabled = dto.IsGpsCheckInEnabled && dto.GpsLatitude.HasValue && dto.GpsLongitude.HasValue,
                GpsCheckInRadius = dto.GpsCheckInRadius > 0 ? dto.GpsCheckInRadius : 100,
                CheckInWindowMinutes = dto.CheckInWindowMinutes > 0 ? dto.CheckInWindowMinutes : 10,
                CheckOutWindowMinutes = dto.CheckOutWindowMinutes > 0 ? dto.CheckOutWindowMinutes : 10
            };

            var created = await _repo.CreateAsync(activity);

            // If mandatory club activity, auto-register all club members
            if (isClubActivity && dto.IsMandatory)
            {
                var clubMembers = await _repo.GetClubMemberUserIdsAsync(clubId);
                foreach (var member in clubMembers)
                {
                    try
                    {
                        // Check if already registered to avoid duplicates
                        var isRegistered = await _repo.IsRegisteredAsync(created.Id, member.UserId);
                        if (!isRegistered)
                        {
                            await _repo.AddRegistrationAsync(created.Id, member.UserId);
                        }
                    }
                    catch
                    {
                        // Continue registering other members even if one fails
                    }
                }
            }

            var detail = await GetActivityByIdAsync(created.Id);
            return detail!;
        }

        public async Task<ActivityDetailDto?> ClubUpdateAsync(int managerUserId, int id, ClubCreateActivityDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;
            if (existing.ClubId == null) return null; // not a club activity
            if (existing.CreatedById != managerUserId) return null; // not owner

            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            // Validate collaboration settings
            await ValidateCollaborationSettingsAsync(
                dto.Type, 
                "ClubManager", 
                existing.ClubId.Value,
                dto.ClubCollaborationId, 
                dto.CollaborationPoint, 
                dto.MovementPoint);

            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.Location = dto.Location;
            existing.ImageUrl = dto.ImageUrl;
            existing.StartTime = dto.StartTime;
            existing.EndTime = dto.EndTime;
            existing.Type = dto.Type;
            existing.IsPublic = dto.IsPublic;
            existing.MaxParticipants = dto.MaxParticipants;
            existing.MovementPoint = dto.MovementPoint;
            
            // Handle collaboration changes
            bool collaborationChanged = existing.ClubCollaborationId != dto.ClubCollaborationId;
            
            // Update collaboration fields
            existing.ClubCollaborationId = dto.ClubCollaborationId;
            existing.CollaborationPoint = dto.CollaborationPoint;
            
            // Reset approval if was rejected by admin
            if (existing.Status == "Rejected")
            {
                existing.Status = "PendingApproval";
                existing.ApprovedById = null;
                existing.ApprovedAt = null;
                existing.RejectionReason = null;
            }
            
            // Reset collaboration if was rejected by partner club or collaboration changed
            if (existing.Status == "CollaborationRejected" || 
                (collaborationChanged && existing.CollaborationStatus == "Rejected"))
            {
                if (dto.ClubCollaborationId.HasValue)
                {
                    // Re-send invitation to new or same club
                    existing.Status = "PendingCollaboration";
                    existing.CollaborationStatus = "Pending";
                    existing.CollaborationRejectionReason = null;
                }
                else
                {
                    // No collaboration anymore, reset to normal flow
                    existing.Status = "PendingApproval";
                    existing.CollaborationStatus = null;
                    existing.CollaborationRejectionReason = null;
                }
            }

            await _repo.UpdateAsync(existing);
            return await GetActivityByIdAsync(existing.Id);
        }

        public async Task<bool> ClubDeleteAsync(int managerUserId, int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            if (existing.ClubId == null) return false;
            if (existing.CreatedById != managerUserId) return false;
            return await _repo.DeleteAsync(id);
        }

		public async Task<(bool success, string message)> RegisterAsync(int userId, int activityId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return (false, "Activity not found");
			if (activity.Status != "Approved") return (false, "Activity is not open for registration");
			if (activity.EndTime <= DateTime.UtcNow) return (false, "Activity has ended");

			// Allow re-register after Cancelled
			var existing = await _repo.GetRegistrationAsync(activityId, userId);
			if (existing != null && existing.Status == "Cancelled")
			{
				// Update status back to Registered and save to database
				var updated = await _repo.UpdateRegistrationAsync(activityId, userId, "Registered");
				if (updated)
				{
					return (true, "Activity registered");
				}
				return (false, "Failed to re-register activity");
			}
			if (activity.MaxParticipants.HasValue)
			{
				var current = await _repo.GetRegistrationCountAsync(activityId);
				if (current >= activity.MaxParticipants.Value) return (false, "Registration is full");
			}

			// Visibility check with collaboration support
			if (!activity.IsPublic)
			{
				// For ClubCollaboration activities, check membership in either organizing or collaborating club
				if (activity.Type == ActivityType.ClubCollaboration && activity.ClubCollaborationId.HasValue)
				{
					bool isOrganizerMember = false;
					bool isCollaboratorMember = false;
					
					// Check organizing club membership
					if (activity.ClubId.HasValue)
					{
						isOrganizerMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
					}
					
					// Check collaborating club membership
					isCollaboratorMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubCollaborationId.Value);
					
					// Allow registration if member of either club
					if (!isOrganizerMember && !isCollaboratorMember)
					{
						return (false, "This activity is for members of the organizing or collaborating clubs only");
					}
				}
				else
				{
					// Default club-only check for non-collaboration activities
					if (!activity.ClubId.HasValue) return (false, "This activity is for Club members only");
					var isMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
					if (!isMember) return (false, "This activity is for Club members only");
				}
			}

			// Duplicate registration check
			var already = await _repo.IsRegisteredAsync(activityId, userId);
			if (already) return (true, "Activity already registered");

			await _repo.AddRegistrationAsync(activityId, userId);
			return (true, "Activity registered");
		}

		public async Task<(bool success, string message)> UnregisterAsync(int userId, int activityId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return (false, "Activity not found");
			if (activity.EndTime <= DateTime.UtcNow) return (false, "Activity has ended");

			var reg = await _repo.GetRegistrationAsync(activityId, userId);
			if (reg == null || reg.Status != "Registered") return (false, "You are not registered for this activity");

			var ok = await _repo.CancelRegistrationAsync(activityId, userId);
			return ok ? (true, "Unregistered successfully") : (false, "Unable to unregister");
		}

		public async Task<List<ActivityListItemDto>> GetMyRegistrationsAsync(int userId)
		{
			var regs = await _repo.GetUserRegistrationsAsync(userId);
			var list = new List<ActivityListItemDto>();
			foreach (var reg in regs)
			{
				var a = reg.Activity;
				var count = await _repo.GetRegistrationCountAsync(a.Id);
				var attendanceCount = await _repo.GetAttendanceCountAsync(a.Id);
				var hasEvaluation = await _repo.HasEvaluationAsync(a.Id);
				var isFull = a.MaxParticipants.HasValue && count >= a.MaxParticipants.Value;
				var hasAttended = await _repo.HasAttendanceAsync(a.Id, userId) || reg.Status == "Attended";
				var hasFeedback = await _repo.HasFeedbackAsync(a.Id, userId);
				list.Add(new ActivityListItemDto
				{
					Id = a.Id,
					Title = a.Title,
					Description = a.Description,
					Location = a.Location,
					ImageUrl = a.ImageUrl,
					StartTime = a.StartTime,
					EndTime = a.EndTime,
					Type = a.Type.ToString(),
					Status = a.Status,
					MovementPoint = a.MovementPoint,
					MaxParticipants = a.MaxParticipants,
					CurrentParticipants = count,
					IsPublic = a.IsPublic,
					RequiresApproval = a.RequiresApproval,
					ClubId = a.ClubId,
					ClubName = a.Club?.Name,
					ClubLogo = a.Club?.LogoUrl,
					// Collaboration fields
					ClubCollaborationId = a.ClubCollaborationId,
					CollaboratingClubName = a.CollaboratingClub?.Name,
					CollaborationPoint = a.CollaborationPoint,
					CanRegister = false,
					IsRegistered = true,
					IsFull = isFull,
					HasAttended = hasAttended,
					HasFeedback = hasFeedback,
					AttendedCount = attendanceCount,
					HasEvaluation = hasEvaluation
				});
			}
			return list;
		}

		public async Task<(bool success, string message)> SubmitFeedbackAsync(int userId, int activityId, int rating, string? comment)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return (false, "Activity not found");
			if (rating < 1 || rating > 5) return (false, "Rating must be 1-5");

			// Auto-complete if activity has ended - ensures scoring happens immediately
			await AutoCompleteIfEndedAsync(activity);

			// must have attended
			var attended = await _repo.HasAttendanceAsync(activityId, userId);
			if (!attended) return (false, "Only attendees can submit feedback");

			// upsert feedback: create if not exists, else update
			var existing = await _repo.GetFeedbackAsync(activityId, userId);
			if (existing == null)
			{
				await _repo.AddFeedbackAsync(activityId, userId, rating, comment);
				return (true, "Feedback submitted");
			}
			else
			{
				existing.Rating = rating;
				existing.Comment = comment;
				existing.CreatedAt = DateTime.UtcNow;
				await _repo.UpdateFeedbackAsync(existing);
				return (true, "Feedback updated");
			}
		}

		public async Task<ActivityFeedbackDto?> GetMyFeedbackAsync(int userId, int activityId)
		{
			var fb = await _repo.GetFeedbackAsync(activityId, userId);
			if (fb == null) return null;
			return new ActivityFeedbackDto
			{
				ActivityId = activityId,
				Rating = fb.Rating,
				Comment = fb.Comment
			};
		}

        private async Task<List<ActivityListItemDto>> MapToListDto(List<Activity> activities)
        {
            var result = new List<ActivityListItemDto>();

            foreach (var activity in activities)
            {
                var registrationCount = await _repo.GetRegistrationCountAsync(activity.Id);
                var attendanceCount = await _repo.GetAttendanceCountAsync(activity.Id);
                var hasEvaluation = await _repo.HasEvaluationAsync(activity.Id);
                var isFull = activity.MaxParticipants.HasValue && registrationCount >= activity.MaxParticipants.Value;
                var canRegister = activity.Status == "Approved" &&
                                  activity.EndTime > DateTime.UtcNow &&
                                  !isFull;

                result.Add(new ActivityListItemDto
                {
                    Id = activity.Id,
                    Title = activity.Title,
                    Description = activity.Description,
                    Location = activity.Location,
                    ImageUrl = activity.ImageUrl,
                    StartTime = activity.StartTime,
                    EndTime = activity.EndTime,
                    Type = activity.Type.ToString(),
                    Status = activity.Status,
                    MovementPoint = activity.MovementPoint,
                    MaxParticipants = activity.MaxParticipants,
                    CurrentParticipants = registrationCount,
                    IsPublic = activity.IsPublic,
                    RequiresApproval = activity.RequiresApproval,
                    ClubId = activity.ClubId,
                    ClubName = activity.Club?.Name,
                    ClubLogo = activity.Club?.LogoUrl,
                    // Collaboration fields
                    ClubCollaborationId = activity.ClubCollaborationId,
                    CollaboratingClubName = activity.CollaboratingClub?.Name,
                    CollaborationPoint = activity.CollaborationPoint,
                    AttendanceCode = activity.AttendanceCode, // Include for Admin/Manager
                    CanRegister = canRegister,
                    IsRegistered = false, // TODO: Check if current user is registered
                    IsFull = isFull,
                    AttendedCount = attendanceCount,
                    HasEvaluation = hasEvaluation
                });
            }

            return result;
        }

		public async Task<List<AdminActivityRegistrantDto>> GetRegistrantsAsync(int adminUserId, int activityId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return new List<AdminActivityRegistrantDto>();
			// Only allow admin to manage attendance for admin-created activities (ClubId == null)
			if (activity.ClubId != null) return new List<AdminActivityRegistrantDto>();

			// Auto-complete if activity has ended - ensures scoring happens immediately
			await AutoCompleteIfEndedAsync(activity);

		var list = await _repo.GetRegistrantsWithAttendanceAsync(activityId);
		return list.Select(x => new AdminActivityRegistrantDto
		{
			UserId = x.UserId,
			FullName = x.FullName,
			Email = x.Email,
			IsPresent = x.IsPresent,
			ParticipationScore = x.ParticipationScore,
			CheckInMethod = x.CheckInMethod,
			GpsCheckInTime = x.GpsCheckInTime,
			GpsCheckOutTime = x.GpsCheckOutTime,
			CheckInDistance = x.CheckInDistance
		}).ToList();
		}

	public async Task<(bool success, string message)> SetAttendanceAsync(int adminUserId, int activityId, int targetUserId, bool isPresent, int? participationScore = null)
	{
		var activity = await _repo.GetByIdAsync(activityId);
		if (activity == null) return (false, "Activity not found");
		if (activity.ClubId != null) return (false, "Attendance for club activities is managed by Club Manager");
		
		// Validate participation score
		if (isPresent && participationScore.HasValue)
		{
			if (participationScore.Value < 3 || participationScore.Value > 5)
			{
				return (false, "Participation score must be between 3 and 5");
			}
		}
		
		await _repo.SetAttendanceAsync(activityId, targetUserId, isPresent, participationScore, adminUserId);
		
		// If activity is already completed, update MovementRecordDetails immediately
		if (activity.Status == "Completed")
		{
			// Skip club internal activities
			bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
			                              activity.Type == ActivityType.ClubTraining || 
			                              activity.Type == ActivityType.ClubWorkshop;
			
			if (!isClubInternalActivity)
			{
				try
				{
					var student = await _studentRepo.GetByUserIdAsync(targetUserId);
					if (student != null)
					{
						if (isPresent && participationScore.HasValue)
						{
							// Add or update score in MovementRecordDetails
							// criterionId: 4 = "Tham gia sự kiện" (Group 2 - Hoạt động chính trị xã hội)
							await _movementRecordService.AddScoreFromAttendanceAsync(
								studentId: student.Id,
								criterionId: 4,
								points: participationScore.Value,
								activityId: activityId
							);
							_logger.LogInformation("[ADMIN ATTENDANCE] Updated MovementRecordDetails for StudentId={StudentId}, Score={Score}", 
								student.Id, participationScore.Value);
						}
						else if (!isPresent)
						{
							// Remove score when marked absent
							await _movementRecordService.RemoveScoreFromAttendanceAsync(
								studentId: student.Id,
								activityId: activityId
							);
							_logger.LogInformation("[ADMIN ATTENDANCE] Removed score from MovementRecordDetails for StudentId={StudentId}", student.Id);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[ADMIN ATTENDANCE] Failed to update MovementRecordDetails");
				}
			}
		}
		else
		{
			_logger.LogInformation("[ADMIN ATTENDANCE] Activity not completed yet. Score will be added to MovementRecordDetails when activity completes.");
		}
		
		_logger.LogInformation("[ADMIN ATTENDANCE] Attendance updated for ActivityId={ActivityId}, UserId={UserId}, IsPresent={IsPresent}, ParticipationScore={Score}",
			activityId, targetUserId, isPresent, participationScore);
		
		return (true, "Attendance updated");
	}

	// ================= CLUB MANAGER ATTENDANCE =================
	public async Task<List<AdminActivityRegistrantDto>> GetClubRegistrantsAsync(int managerUserId, int activityId)
	{
		var activity = await _repo.GetByIdAsync(activityId);
		if (activity == null) return new List<AdminActivityRegistrantDto>();
		
		// Only allow club managers to manage attendance for their club activities
		if (!activity.ClubId.HasValue) return new List<AdminActivityRegistrantDto>();
		
		// Check if user is manager of this club
		var isManager = await _repo.IsUserManagerOfClubAsync(managerUserId, activity.ClubId.Value);
		if (!isManager)
		{
			throw new UnauthorizedAccessException("You are not the manager of this club");
		}

		// Auto-complete if activity has ended - ensures scoring happens immediately
		await AutoCompleteIfEndedAsync(activity);

		var list = await _repo.GetRegistrantsWithAttendanceAsync(activityId);
		return list.Select(x => new AdminActivityRegistrantDto
		{
			UserId = x.UserId,
			FullName = x.FullName,
			Email = x.Email,
			IsPresent = x.IsPresent,
			ParticipationScore = x.ParticipationScore,
			CheckInMethod = x.CheckInMethod,
			GpsCheckInTime = x.GpsCheckInTime,
			GpsCheckOutTime = x.GpsCheckOutTime,
			CheckInDistance = x.CheckInDistance
		}).ToList();
	}

	public async Task<(bool success, string message)> SetClubAttendanceAsync(int managerUserId, int activityId, int targetUserId, bool isPresent, int? participationScore = null)
	{
		var activity = await _repo.GetByIdAsync(activityId);
		if (activity == null) return (false, "Activity not found");
		
		if (!activity.ClubId.HasValue) return (false, "This is not a club activity");
		
		// Check if user is manager of this club
		var isManager = await _repo.IsUserManagerOfClubAsync(managerUserId, activity.ClubId.Value);
		if (!isManager)
		{
			throw new UnauthorizedAccessException("You are not the manager of this club");
		}
		
		// Validate participation score
		if (isPresent && participationScore.HasValue)
		{
			if (participationScore.Value < 3 || participationScore.Value > 5)
			{
				return (false, "Participation score must be between 3 and 5");
			}
		}

		await _repo.SetAttendanceAsync(activityId, targetUserId, isPresent, participationScore, managerUserId);
		
		// If activity is already completed, update MovementRecordDetails immediately
		if (activity.Status == "Completed")
		{
			// Skip club internal activities
			bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
			                              activity.Type == ActivityType.ClubTraining || 
			                              activity.Type == ActivityType.ClubWorkshop;
			
			if (!isClubInternalActivity)
			{
				try
				{
					var student = await _studentRepo.GetByUserIdAsync(targetUserId);
					if (student != null)
					{
						if (isPresent && participationScore.HasValue)
						{
							// Add or update score in MovementRecordDetails
							// criterionId: 4 = "Tham gia sự kiện" (Group 2 - Hoạt động chính trị xã hội)
							await _movementRecordService.AddScoreFromAttendanceAsync(
								studentId: student.Id,
								criterionId: 4,
								points: participationScore.Value,
								activityId: activityId
							);
							_logger.LogInformation("[CLUB ATTENDANCE] Updated MovementRecordDetails for StudentId={StudentId}, Score={Score}", 
								student.Id, participationScore.Value);
						}
						else if (!isPresent)
						{
							// Remove score when marked absent
							await _movementRecordService.RemoveScoreFromAttendanceAsync(
								studentId: student.Id,
								activityId: activityId
							);
							_logger.LogInformation("[CLUB ATTENDANCE] Removed score from MovementRecordDetails for StudentId={StudentId}", student.Id);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[CLUB ATTENDANCE] Failed to update MovementRecordDetails");
				}
			}
		}
		else
		{
			_logger.LogInformation("[CLUB ATTENDANCE] Activity not completed yet. Score will be added to MovementRecordDetails when activity completes.");
		}
		
		_logger.LogInformation("[CLUB ATTENDANCE] Attendance updated for ActivityId={ActivityId}, UserId={UserId}, IsPresent={IsPresent}, ParticipationScore={Score}",
			activityId, targetUserId, isPresent, participationScore);
		
		return (true, "Attendance updated");
	}

		public async Task<List<AdminActivityFeedbackDto>> GetActivityFeedbacksAsync(int adminUserId, int activityId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return new List<AdminActivityFeedbackDto>();
			// Admin can view feedback for both admin and club activities
			var raw = await _repo.GetFeedbacksAsync(activityId);
			return raw.Select(x => new AdminActivityFeedbackDto
			{
				UserId = x.UserId,
				FullName = x.FullName,
				Email = x.Email,
				Rating = x.Rating,
				Comment = x.Comment,
				CreatedAt = x.CreatedAt
			}).ToList();
		}

		// ================= STUDENT SELF CHECK-IN =================
		public async Task<(bool success, string message)> CheckInWithCodeAsync(int userId, int activityId, string attendanceCode)
		{
			// Validate Activity exists
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) 
				return (false, "Activity not found");
			
			// Validate time window (Requirements 3.1, 3.2, 3.3)
			// Use local time for comparison since DB stores local time
			var now = DateTime.Now;
			_logger.LogInformation("[CHECK-IN] Current time: {Now}, Activity StartTime: {StartTime}, EndTime: {EndTime}", 
				now, activity.StartTime, activity.EndTime);
			
			if (now < activity.StartTime) 
				return (false, "Chưa đến thời gian điểm danh");
			if (now > activity.EndTime) 
				return (false, "Đã hết thời gian điểm danh");
			
			// Validate AttendanceCode matches (Requirements 2.1, 2.2)
			_logger.LogInformation("[CHECK-IN] Comparing codes - Input: '{InputCode}', Activity: '{ActivityCode}'", 
				attendanceCode, activity.AttendanceCode);
			
			if (string.IsNullOrWhiteSpace(activity.AttendanceCode) || activity.AttendanceCode != attendanceCode) 
				return (false, "Mã điểm danh không chính xác");
			
			// Validate User has registered for Activity (Requirements 2.4)
			var isRegistered = await _repo.IsRegisteredAsync(activityId, userId);
			if (!isRegistered) 
				return (false, "Bạn chưa đăng ký tham gia hoạt động này");
			
			// Validate User hasn't already checked in (Requirements 2.5)
			var hasAttendance = await _repo.GetAttendanceAsync(activityId, userId);
			if (hasAttendance != null) 
				return (false, "Bạn đã điểm danh rồi");
			
			// Create ActivityAttendance with ParticipationScore = 5 and CheckedById = null (Requirements 2.3)
			const int participationScore = 5;
			await _repo.CreateAttendanceAsync(activityId, userId, true, participationScore, null);
			
			// Note: ParticipationScore will be added to MovementRecordDetails when Activity is completed
			// This ensures scores are only awarded after the activity ends
			_logger.LogInformation("[STUDENT CHECK-IN] Attendance recorded for ActivityId={ActivityId}, UserId={UserId}, ParticipationScore={Score}. Score will be awarded when activity completes.", 
				activityId, userId, participationScore);
			
			return (true, "Điểm danh thành công");
		}

		// ================= UPDATE PARTICIPATION SCORE =================
		public async Task<(bool success, string message)> UpdateParticipationScoreAsync(
			int adminOrManagerUserId, 
			int activityId, 
			int targetUserId, 
			int participationScore)
		{
			// Validate ParticipationScore in range 3-5 (Requirements 4.3)
			if (participationScore < 3 || participationScore > 5)
				return (false, "Điểm phải từ 3 đến 5");
			
			// Validate Activity exists
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) 
				return (false, "Activity not found");
			
			// Validate User has permission (Admin or Manager of the Club) (Requirements 4.1, 4.2)
			// If activity belongs to a club, check if user is manager
			if (activity.ClubId.HasValue)
			{
				var isManager = await _repo.IsUserManagerOfClubAsync(adminOrManagerUserId, activity.ClubId.Value);
				// Note: Admin check would need to be done at controller level with role check
			}
			
			// Validate target user has checked in (Requirements 4.4)
			var attendance = await _repo.GetAttendanceAsync(activityId, targetUserId);
			if (attendance == null) 
				return (false, "Sinh viên chưa điểm danh");
			
			// Update ParticipationScore in ActivityAttendance (Requirements 4.4)
			attendance.ParticipationScore = participationScore;
			await _repo.UpdateAttendanceAsync(attendance);
			
			// Only update MovementRecordDetails if activity is already completed
			// If activity is not completed yet, score will be added when activity completes
			if (activity.Status == "Completed")
			{
				// Skip club internal activities
				bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
				                              activity.Type == ActivityType.ClubTraining || 
				                              activity.Type == ActivityType.ClubWorkshop;
				
				if (!isClubInternalActivity)
				{
					try
					{
						var student = await _studentRepo.GetByUserIdAsync(targetUserId);
						if (student != null)
						{
							await _movementRecordService.UpdateScoreFromAttendanceAsync(
								studentId: student.Id,
								activityId: activityId,
								newPoints: participationScore
							);
							_logger.LogInformation("[UPDATE SCORE] Updated MovementRecordDetails for StudentId={StudentId}, NewScore={Score}", 
								student.Id, participationScore);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "[UPDATE SCORE] Failed to update MovementRecordDetails");
					}
				}
			}
			else
			{
				_logger.LogInformation("[UPDATE SCORE] Activity not completed yet. Score will be added to MovementRecordDetails when activity completes.");
			}
			
			_logger.LogInformation("[UPDATE SCORE] ParticipationScore updated for ActivityId={ActivityId}, UserId={UserId}, NewScore={Score}",
				activityId, targetUserId, participationScore);
			
			return (true, "Cập nhật điểm thành công");
		}

		// ================= AUTO MARK ABSENT =================
		public async Task<(int markedCount, string message)> AutoMarkAbsentAsync(int activityId, int? userId = null)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
				return (0, "Activity not found");
			
			// If userId is provided (ClubManager), verify they manage this activity's club
			if (userId.HasValue && activity.ClubId.HasValue)
			{
				var isManager = await _repo.IsUserManagerOfClubAsync(userId.Value, activity.ClubId.Value);
				if (!isManager)
					return (0, "You don't have permission to manage this activity");
			}
			
			// Optional: Check if activity has ended (commented out to allow manual marking anytime)
			// if (DateTime.Now < activity.EndTime)
			// 	return (0, "Activity has not ended yet");
			
			_logger.LogInformation("[AUTO MARK ABSENT] Processing activity {ActivityId}, EndTime: {EndTime}, Now: {Now}", 
				activityId, activity.EndTime, DateTime.Now);
			
			// Get all registrants
			var registrants = await _repo.GetRegistrantsWithAttendanceAsync(activityId);
			
			// Find those who registered but haven't been marked (IsPresent = null or false)
			var notMarked = registrants.Where(r => r.IsPresent != true).ToList();
			
			if (!notMarked.Any())
				return (0, "All registrants have been marked");
			
			int markedCount = 0;
			foreach (var registrant in notMarked)
			{
				// Mark as absent (IsPresent = false, no score, checkedById = null for system auto-mark)
				await _repo.SetAttendanceAsync(activityId, registrant.UserId, false, null, null);
				markedCount++;
				
				_logger.LogInformation("[AUTO MARK ABSENT] Marked user {UserId} as absent for activity {ActivityId}", 
					registrant.UserId, activityId);
			}
			
			return (markedCount, $"Marked {markedCount} registrant(s) as absent");
		}

		// ================= HELPER METHODS =================
		public async Task<bool> IsUserManagerOfClubAsync(int userId, int clubId)
		{
			return await _repo.IsUserManagerOfClubAsync(userId, clubId);
		}

		// ================= EVALUATION METHODS =================
		/// <summary>
		/// Creates a new evaluation for a completed activity
		/// Authorization: Only ClubManager who manages the club can create evaluations (Requirements 6.1, 6.2)
		/// Validation: Activity must be completed, no existing evaluation, reason required if actual < expected
		/// </summary>
		public async Task<ActivityEvaluationDto> CreateEvaluationAsync(int managerId, int activityId, CreateActivityEvaluationDto dto)
		{
			// Get activity
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
				throw new KeyNotFoundException("Activity not found");
			
			// Check activity status is "Completed"
			if (activity.Status != "Completed")
				throw new InvalidOperationException("Only completed activities can be evaluated");
			
			// Check no existing evaluation
			var hasEvaluation = await _repo.HasEvaluationAsync(activityId);
			if (hasEvaluation)
				throw new InvalidOperationException("This activity has already been evaluated");
			
			// Authorization check (Requirement 6.1, 6.2): Verify user is manager of club
			if (!activity.ClubId.HasValue)
				throw new InvalidOperationException("Cannot evaluate school-wide activities");
			
			var isManager = await _repo.IsUserManagerOfClubAsync(managerId, activity.ClubId.Value);
			if (!isManager)
				throw new UnauthorizedAccessException("You do not have permission to evaluate this activity");
			
			// Validate reason when ActualParticipants < ExpectedParticipants
			if (dto.ActualParticipants < dto.ExpectedParticipants && string.IsNullOrWhiteSpace(dto.Reason))
				throw new ArgumentException("Please provide a reason when actual participants is less than expected");
			
			// Calculate average score (including Success score)
			var averageScore = (dto.CommunicationScore + dto.OrganizationScore + dto.HostScore + dto.SpeakerScore + dto.Success) / 5.0;
			
			// Create evaluation entity
			var evaluation = new ActivityEvaluation
			{
				ActivityId = activityId,
				ExpectedParticipants = dto.ExpectedParticipants,
				ActualParticipants = dto.ActualParticipants,
				Reason = dto.Reason,
				CommunicationScore = dto.CommunicationScore,
				OrganizationScore = dto.OrganizationScore,
				HostScore = dto.HostScore,
				SpeakerScore = dto.SpeakerScore,
				Success = dto.Success,
				Limitations = dto.Limitations,
				ImprovementMeasures = dto.ImprovementMeasures,
				AverageScore = averageScore,
				CreatedById = managerId,
				CreatedAt = DateTime.UtcNow
			};
			
			// Save to database
			var created = await _repo.CreateEvaluationAsync(evaluation);
			
			// Get full evaluation with navigation properties
			var fullEvaluation = await _repo.GetEvaluationByActivityIdAsync(activityId);
			if (fullEvaluation == null)
				throw new InvalidOperationException("Failed to retrieve created evaluation");
			
			// Map to DTO
			return new ActivityEvaluationDto
			{
				Id = fullEvaluation.Id,
				ActivityId = fullEvaluation.ActivityId,
				ActivityTitle = fullEvaluation.Activity.Title,
				ActivityStartTime = fullEvaluation.Activity.StartTime,
				ActivityEndTime = fullEvaluation.Activity.EndTime,
				ExpectedParticipants = fullEvaluation.ExpectedParticipants,
				ActualParticipants = fullEvaluation.ActualParticipants,
				Reason = fullEvaluation.Reason,
				CommunicationScore = fullEvaluation.CommunicationScore,
				OrganizationScore = fullEvaluation.OrganizationScore,
				HostScore = fullEvaluation.HostScore,
				SpeakerScore = fullEvaluation.SpeakerScore,
				AverageScore = fullEvaluation.AverageScore,
				Success = fullEvaluation.Success,
				Limitations = fullEvaluation.Limitations,
				ImprovementMeasures = fullEvaluation.ImprovementMeasures,
				CreatedById = fullEvaluation.CreatedById,
				CreatedByName = fullEvaluation.CreatedBy.FullName,
				CreatedAt = fullEvaluation.CreatedAt,
				UpdatedAt = fullEvaluation.UpdatedAt
			};
		}

		/// <summary>
		/// Updates an existing evaluation for an activity
		/// Authorization: Only ClubManager who manages the club can update evaluations (Requirements 6.1, 6.2)
		/// Validation: Reason required if actual < expected
		/// </summary>
		public async Task<ActivityEvaluationDto?> UpdateEvaluationAsync(int managerId, int activityId, CreateActivityEvaluationDto dto)
		{
			// Get activity
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
				throw new KeyNotFoundException("Activity not found");
			
			// Authorization check (Requirement 6.1, 6.2): Verify user is manager of club
			if (!activity.ClubId.HasValue)
				throw new InvalidOperationException("Cannot evaluate school-wide activities");
			
			var isManager = await _repo.IsUserManagerOfClubAsync(managerId, activity.ClubId.Value);
			if (!isManager)
				throw new UnauthorizedAccessException("You do not have permission to evaluate this activity");
			
			// Get existing evaluation
			var evaluation = await _repo.GetEvaluationByActivityIdAsync(activityId);
			if (evaluation == null)
				return null;
			
			// Validate reason when ActualParticipants < ExpectedParticipants
			if (dto.ActualParticipants < dto.ExpectedParticipants && string.IsNullOrWhiteSpace(dto.Reason))
				throw new ArgumentException("Please provide a reason when actual participants is less than expected");
			
			// Calculate average score (including Success score)
			var averageScore = (dto.CommunicationScore + dto.OrganizationScore + dto.HostScore + dto.SpeakerScore + dto.Success) / 5.0;
			
			// Update evaluation entity
			evaluation.ExpectedParticipants = dto.ExpectedParticipants;
			evaluation.ActualParticipants = dto.ActualParticipants;
			evaluation.Reason = dto.Reason;
			evaluation.CommunicationScore = dto.CommunicationScore;
			evaluation.OrganizationScore = dto.OrganizationScore;
			evaluation.HostScore = dto.HostScore;
			evaluation.SpeakerScore = dto.SpeakerScore;
			evaluation.Success = dto.Success;
			evaluation.Limitations = dto.Limitations;
			evaluation.ImprovementMeasures = dto.ImprovementMeasures;
			evaluation.AverageScore = averageScore;
			evaluation.UpdatedAt = DateTime.UtcNow;
			
			// Save to database
			var updated = await _repo.UpdateEvaluationAsync(evaluation);
			if (updated == null)
				return null;
			
			// Map to DTO
			return new ActivityEvaluationDto
			{
				Id = updated.Id,
				ActivityId = updated.ActivityId,
				ActivityTitle = updated.Activity.Title,
				ActivityStartTime = updated.Activity.StartTime,
				ActivityEndTime = updated.Activity.EndTime,
				ExpectedParticipants = updated.ExpectedParticipants,
				ActualParticipants = updated.ActualParticipants,
				Reason = updated.Reason,
				CommunicationScore = updated.CommunicationScore,
				OrganizationScore = updated.OrganizationScore,
				HostScore = updated.HostScore,
				SpeakerScore = updated.SpeakerScore,
				AverageScore = updated.AverageScore,
				Success = updated.Success,
				Limitations = updated.Limitations,
				ImprovementMeasures = updated.ImprovementMeasures,
				CreatedById = updated.CreatedById,
				CreatedByName = updated.CreatedBy.FullName,
				CreatedAt = updated.CreatedAt,
				UpdatedAt = updated.UpdatedAt
			};
		}

		/// <summary>
		/// Retrieves an evaluation for an activity
		/// Authorization: 
		/// - ClubManager can view evaluations for their club (Requirement 6.2)
		/// - Admin can view all evaluations (read-only) (Requirement 6.4)
		/// </summary>
		public async Task<ActivityEvaluationDto?> GetEvaluationAsync(int userId, int activityId, bool isAdmin = false)
		{
			// Get activity
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
				throw new KeyNotFoundException("Activity not found");
			
			// Authorization check
			// Admin can view all evaluations (read-only) - Requirement 6.4
			if (!isAdmin)
			{
				// For non-admin users (ClubManager), verify they are manager of the club (Requirement 6.2)
				if (!activity.ClubId.HasValue)
					throw new UnauthorizedAccessException("You do not have permission to view this evaluation");
				
				var isManager = await _repo.IsUserManagerOfClubAsync(userId, activity.ClubId.Value);
				if (!isManager)
					throw new UnauthorizedAccessException("You do not have permission to view this evaluation");
			}
			
			// Get evaluation
			var evaluation = await _repo.GetEvaluationByActivityIdAsync(activityId);
			if (evaluation == null)
				return null;
			
			// Map to DTO
			return new ActivityEvaluationDto
			{
				Id = evaluation.Id,
				ActivityId = evaluation.ActivityId,
				ActivityTitle = evaluation.Activity.Title,
				ActivityStartTime = evaluation.Activity.StartTime,
				ActivityEndTime = evaluation.Activity.EndTime,
				ExpectedParticipants = evaluation.ExpectedParticipants,
				ActualParticipants = evaluation.ActualParticipants,
				Reason = evaluation.Reason,
				CommunicationScore = evaluation.CommunicationScore,
				OrganizationScore = evaluation.OrganizationScore,
				HostScore = evaluation.HostScore,
				SpeakerScore = evaluation.SpeakerScore,
				AverageScore = evaluation.AverageScore,
				Success = evaluation.Success,
				Limitations = evaluation.Limitations,
				ImprovementMeasures = evaluation.ImprovementMeasures,
				CreatedById = evaluation.CreatedById,
				CreatedByName = evaluation.CreatedBy.FullName,
				CreatedAt = evaluation.CreatedAt,
				UpdatedAt = evaluation.UpdatedAt
			};
		}

		public async Task<int> AutoCompleteActivitiesAsync()
		{
			// Get all activities with Status="Approved" AND EndTime < Now
			var allActivities = await _repo.GetAllAsync();
			var now = DateTime.UtcNow;
			
			var activitiesToComplete = allActivities
				.Where(a => a.Status == "Approved" && a.EndTime < now)
				.ToList();
			
			int count = 0;
			foreach (var activity in activitiesToComplete)
			{
				try
				{
					await _repo.UpdateActivityStatusAsync(activity.Id, "Completed");
					count++;
					_logger.LogInformation("[AUTO COMPLETE] Updated activity {ActivityId} to Completed", activity.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[AUTO COMPLETE] Failed to update activity {ActivityId}", activity.Id);
				}
			}
			
			_logger.LogInformation("[AUTO COMPLETE] Completed {Count} activities", count);
			return count;
		}

		// Helper method to check if activity type requires schedules
		private bool IsComplexActivity(ActivityType type)
		{
			return type != ActivityType.ClubMeeting && 
			       type != ActivityType.ClubTraining && 
			       type != ActivityType.ClubWorkshop;
		}

		// Validation helper for schedule time
		private void ValidateScheduleTime(string startTimeStr, string endTimeStr, string title)
		{
			if (!TimeSpan.TryParse(startTimeStr, out var startTime))
			{
				throw new ArgumentException("Invalid start time format");
			}

			if (!TimeSpan.TryParse(endTimeStr, out var endTime))
			{
				throw new ArgumentException("Invalid end time format");
			}

			if (endTime <= startTime)
			{
				throw new ArgumentException("Schedule end time must be after start time");
			}

			if (string.IsNullOrWhiteSpace(title))
			{
				throw new ArgumentException("Schedule title is required");
			}

			if (title.Length > 500)
			{
				throw new ArgumentException("Schedule title cannot exceed 500 characters");
			}
		}

		// Validation helper for assignment
		private void ValidateAssignment(CreateActivityScheduleAssignmentDto assignmentDto)
		{
			if (!assignmentDto.UserId.HasValue && string.IsNullOrWhiteSpace(assignmentDto.ResponsibleName))
			{
				throw new ArgumentException("Assignment must have either UserId or ResponsibleName");
			}

			if (!string.IsNullOrWhiteSpace(assignmentDto.ResponsibleName) && assignmentDto.ResponsibleName.Length > 200)
			{
				throw new ArgumentException("ResponsibleName cannot exceed 200 characters");
			}

			if (!string.IsNullOrWhiteSpace(assignmentDto.Role) && assignmentDto.Role.Length > 100)
			{
				throw new ArgumentException("Role cannot exceed 100 characters");
			}
		}

		// Validation helper for assignment (update version)
		private void ValidateAssignment(UpdateActivityScheduleAssignmentDto assignmentDto)
		{
			if (!assignmentDto.UserId.HasValue && string.IsNullOrWhiteSpace(assignmentDto.ResponsibleName))
			{
				throw new ArgumentException("Assignment must have either UserId or ResponsibleName");
			}

			if (!string.IsNullOrWhiteSpace(assignmentDto.ResponsibleName) && assignmentDto.ResponsibleName.Length > 200)
			{
				throw new ArgumentException("ResponsibleName cannot exceed 200 characters");
			}

			if (!string.IsNullOrWhiteSpace(assignmentDto.Role) && assignmentDto.Role.Length > 100)
			{
				throw new ArgumentException("Role cannot exceed 100 characters");
			}
		}

		// Validation helper for schedule time range within activity
		private void ValidateScheduleTimeRange(Activity activity, string startTimeStr, string endTimeStr, string title)
		{
			var scheduleStart = TimeSpan.Parse(startTimeStr);
			var scheduleEnd = TimeSpan.Parse(endTimeStr);

			var activityStartDate = activity.StartTime.Date;
			var activityEndDate = activity.EndTime.Date;
			var activityStartTime = activity.StartTime.TimeOfDay;
			var activityEndTime = activity.EndTime.TimeOfDay;

			// For single-day activities, validate schedule times are within activity time range
			if (activityStartDate == activityEndDate)
			{
				if (scheduleStart < activityStartTime || scheduleEnd > activityEndTime)
				{
					throw new ArgumentException($"Schedule '{title}' time ({startTimeStr} - {endTimeStr}) is outside the activity time range. All schedule times must be between {activity.StartTime:HH:mm} and {activity.EndTime:HH:mm}. Please adjust the schedule times or update the activity time range.");
				}
			}
			// For multi-day activities, schedules can be at any time during the event
			// Just ensure schedule times are valid (end > start), already checked in ValidateScheduleTime
		}

		// ================= SCHEDULE MANAGEMENT =================
		
		// Add schedules to existing activity
		public async Task AddSchedulesToActivityAsync(int activityId, List<CreateActivityScheduleDto> schedules)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
			{
				throw new ArgumentException("Activity not found");
			}

			// Validate activity type
			if (!IsComplexActivity(activity.Type))
			{
				throw new InvalidOperationException("Club Activities (Internal) cannot have schedules");
			}

			// Validate each schedule
			foreach (var scheduleDto in schedules)
			{
				ValidateScheduleTime(scheduleDto.StartTime, scheduleDto.EndTime, scheduleDto.Title);
				// ValidateScheduleTimeRange(activity, scheduleDto.StartTime, scheduleDto.EndTime, scheduleDto.Title); // Removed: Allow schedules outside activity time range

				// Validate description and notes length
				if (!string.IsNullOrWhiteSpace(scheduleDto.Description) && scheduleDto.Description.Length > 1000)
				{
					throw new ArgumentException("Schedule description cannot exceed 1000 characters");
				}

				if (!string.IsNullOrWhiteSpace(scheduleDto.Notes) && scheduleDto.Notes.Length > 1000)
				{
					throw new ArgumentException("Schedule notes cannot exceed 1000 characters");
				}

				// Validate assignments
				foreach (var assignmentDto in scheduleDto.Assignments)
				{
					ValidateAssignment(assignmentDto);
				}
			}

			// Sort schedules by start time
			var sortedSchedules = schedules.OrderBy(s => s.StartTime).ToList();

			// Create schedules with order
			for (int i = 0; i < sortedSchedules.Count; i++)
			{
				var scheduleDto = sortedSchedules[i];
				var schedule = new ActivitySchedule
				{
					ActivityId = activityId,
					StartTime = TimeSpan.Parse(scheduleDto.StartTime),
					EndTime = TimeSpan.Parse(scheduleDto.EndTime),
					Title = scheduleDto.Title,
					Description = scheduleDto.Description,
					Notes = scheduleDto.Notes,
					Order = i + 1
				};

				var createdSchedule = await _scheduleRepo.AddAsync(schedule);

				// Add assignments
				foreach (var assignmentDto in scheduleDto.Assignments)
				{
					var assignment = new ActivityScheduleAssignment
					{
						ActivityScheduleId = createdSchedule.Id,
						UserId = assignmentDto.UserId,
						ResponsibleName = assignmentDto.ResponsibleName,
						Role = assignmentDto.Role
					};

					await _assignmentRepo.AddAsync(assignment);
				}
			}
		}

		// Update activity schedules
		public async Task UpdateActivitySchedulesAsync(int activityId, List<UpdateActivityScheduleDto> schedules)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
			{
				throw new ArgumentException("Activity not found");
			}

			// Validate activity type
			if (!IsComplexActivity(activity.Type))
			{
				throw new InvalidOperationException("Club Activities (Internal) cannot have schedules");
			}

			// Validate each schedule
			foreach (var scheduleDto in schedules)
			{
				ValidateScheduleTime(scheduleDto.StartTime, scheduleDto.EndTime, scheduleDto.Title);
				
				// Validate all schedules must be within activity time range
				// ValidateScheduleTimeRange(activity, scheduleDto.StartTime, scheduleDto.EndTime, scheduleDto.Title); // Removed: Allow schedules outside activity time range

				// Validate description and notes length
				if (!string.IsNullOrWhiteSpace(scheduleDto.Description) && scheduleDto.Description.Length > 1000)
				{
					throw new ArgumentException("Schedule description cannot exceed 1000 characters");
				}

				if (!string.IsNullOrWhiteSpace(scheduleDto.Notes) && scheduleDto.Notes.Length > 1000)
				{
					throw new ArgumentException("Schedule notes cannot exceed 1000 characters");
				}

				// Validate assignments
				foreach (var assignmentDto in scheduleDto.Assignments)
				{
					ValidateAssignment(assignmentDto);
				}
			}

			// Get existing schedules (with tracking for update)
			var existingSchedules = await _scheduleRepo.GetByActivityIdAsync(activityId);

			// Determine which schedules to delete, update, or add
			var existingIds = existingSchedules.Select(s => s.Id).ToHashSet();
			var updatedIds = schedules
				.Where(s => s.Id.HasValue)
				.Select(s => s.Id!.Value)
				.ToHashSet();

			// Delete removed schedules (cascade delete assignments)
			var toDelete = existingSchedules
				.Where(s => !updatedIds.Contains(s.Id))
				.ToList();

			foreach (var schedule in toDelete)
			{
				await _scheduleRepo.DeleteAsync(schedule.Id);
			}

			// Sort schedules by start time
			var sortedSchedules = schedules.OrderBy(s => s.StartTime).ToList();

			// Update or add schedules
			for (int i = 0; i < sortedSchedules.Count; i++)
			{
				var scheduleDto = sortedSchedules[i];

				if (scheduleDto.Id.HasValue)
				{
					// Update existing schedule
					var schedule = existingSchedules.FirstOrDefault(s => s.Id == scheduleDto.Id.Value);
					if (schedule != null)
					{
						schedule.StartTime = TimeSpan.Parse(scheduleDto.StartTime);
						schedule.EndTime = TimeSpan.Parse(scheduleDto.EndTime);
						schedule.Title = scheduleDto.Title;
						schedule.Description = scheduleDto.Description;
						schedule.Notes = scheduleDto.Notes;
						schedule.Order = i + 1;

						await _scheduleRepo.UpdateAsync(schedule);

						// Update assignments
						await UpdateScheduleAssignmentsAsync(schedule.Id, scheduleDto.Assignments);
					}
				}
				else
				{
					// Add new schedule
					var schedule = new ActivitySchedule
					{
						ActivityId = activityId,
						StartTime = TimeSpan.Parse(scheduleDto.StartTime),
						EndTime = TimeSpan.Parse(scheduleDto.EndTime),
						Title = scheduleDto.Title,
						Description = scheduleDto.Description,
						Notes = scheduleDto.Notes,
						Order = i + 1
					};

					var createdSchedule = await _scheduleRepo.AddAsync(schedule);

					// Add assignments
					foreach (var assignmentDto in scheduleDto.Assignments)
					{
						var assignment = new ActivityScheduleAssignment
						{
							ActivityScheduleId = createdSchedule.Id,
							UserId = assignmentDto.UserId,
							ResponsibleName = assignmentDto.ResponsibleName,
							Role = assignmentDto.Role
						};

						await _assignmentRepo.AddAsync(assignment);
					}
				}
			}
		}

		// Update schedule assignments
		private async Task UpdateScheduleAssignmentsAsync(int scheduleId, List<UpdateActivityScheduleAssignmentDto> assignments)
		{
			// Get existing assignments
			var existingAssignments = await _assignmentRepo.GetByScheduleIdAsync(scheduleId);

			var existingIds = existingAssignments.Select(a => a.Id).ToHashSet();
			var updatedIds = assignments
				.Where(a => a.Id.HasValue)
				.Select(a => a.Id!.Value)
				.ToHashSet();

			// Delete removed assignments
			var toDelete = existingAssignments
				.Where(a => !updatedIds.Contains(a.Id))
				.ToList();

			foreach (var assignment in toDelete)
			{
				await _assignmentRepo.DeleteAsync(assignment.Id);
			}

			// Update or add assignments
			foreach (var assignmentDto in assignments)
			{
				if (assignmentDto.Id.HasValue)
				{
					// Update existing assignment
					var assignment = existingAssignments.FirstOrDefault(a => a.Id == assignmentDto.Id.Value);
					if (assignment != null)
					{
						assignment.UserId = assignmentDto.UserId;
						assignment.ResponsibleName = assignmentDto.ResponsibleName;
						assignment.Role = assignmentDto.Role;

						await _assignmentRepo.UpdateAsync(assignment);
					}
				}
				else
				{
					// Add new assignment
					var assignment = new ActivityScheduleAssignment
					{
						ActivityScheduleId = scheduleId,
						UserId = assignmentDto.UserId,
						ResponsibleName = assignmentDto.ResponsibleName,
						Role = assignmentDto.Role
					};

					await _assignmentRepo.AddAsync(assignment);
				}
			}
		}

		// Get activity schedules with assignments
		public async Task<List<ActivityScheduleDto>> GetActivitySchedulesAsync(int activityId)
		{
			var schedules = await _scheduleRepo.GetByActivityIdAsync(activityId);
			var result = new List<ActivityScheduleDto>();

			foreach (var schedule in schedules.OrderBy(s => s.Order))
			{
				var assignments = await _assignmentRepo.GetByScheduleIdAsync(schedule.Id);

				result.Add(new ActivityScheduleDto
				{
					Id = schedule.Id,
					StartTime = schedule.StartTime.ToString(@"hh\:mm"),
					EndTime = schedule.EndTime.ToString(@"hh\:mm"),
					Title = schedule.Title,
					Description = schedule.Description,
					Notes = schedule.Notes,
					Assignments = assignments.Select(a => new ActivityScheduleAssignmentDto
					{
						Id = a.Id,
						ResponsibleName = a.ResponsibleName,
						Role = a.Role
					}).ToList()
				});
			}

			return result;
		}

		private async Task<int> GetParticipantPointsAsync(int userId, int activityId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null)
			{
				_logger.LogWarning("[GET PARTICIPANT POINTS] Activity {ActivityId} not found", activityId);
				return 0;
			}
			
			// For collaboration activities
			if (activity.Type == ActivityType.ClubCollaboration && activity.ClubCollaborationId.HasValue)
			{
				// Check if user is from organizing club (gets Movement Points)
				if (activity.ClubId.HasValue && 
					await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value))
				{
					_logger.LogInformation("[GET PARTICIPANT POINTS] User {UserId} is from organizing club {ClubId}, awarding Movement Points: {Points}", 
						userId, activity.ClubId.Value, (int)activity.MovementPoint);
					return (int)activity.MovementPoint; // BTC gets Movement Points
				}
				
				// Check if user is from collaborating club (gets Collaboration Points)
				if (await _repo.IsUserMemberOfClubAsync(userId, activity.ClubCollaborationId.Value))
				{
					_logger.LogInformation("[GET PARTICIPANT POINTS] User {UserId} is from collaborating club {ClubId}, awarding Collaboration Points: {Points}", 
						userId, activity.ClubCollaborationId.Value, activity.CollaborationPoint ?? 0);
					return activity.CollaborationPoint ?? 0; // Collaborator gets Collaboration Points
				}
			}
			
			// Default point assignment for non-collaboration activities
			_logger.LogInformation("[GET PARTICIPANT POINTS] User {UserId} gets default Movement Points: {Points}", 
				userId, (int)activity.MovementPoint);
			return (int)activity.MovementPoint;
		}

		private async Task<bool> CanUserRegisterAsync(int userId, Activity activity)
		{
			// Public activities - anyone can register
			if (activity.IsPublic) return true;
			
			// Non-public activities
			if (activity.Type == ActivityType.ClubCollaboration && activity.ClubCollaborationId.HasValue)
			{
				// Check if user is member of organizing club OR collaborating club
				bool isOrganizerMember = false;
				bool isCollaboratorMember = false;
				
				if (activity.ClubId.HasValue)
				{
					isOrganizerMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
				}
				
				isCollaboratorMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubCollaborationId.Value);
				
				return isOrganizerMember || isCollaboratorMember;
			}
			
			// Default club-only check
			if (activity.ClubId.HasValue)
			{
				return await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
			}
			
			return false;
		}

		// ================= COLLABORATION VALIDATION =================
		public async Task ValidateCollaborationSettingsAsync(
			ActivityType type, 
			string userRole, 
			int? organizingClubId,
			int? clubCollaborationId, 
			int? collaborationPoint, 
			double movementPoint)
		{
			bool isClubCollaboration = type == ActivityType.ClubCollaboration;
			bool isSchoolCollaboration = type == ActivityType.SchoolCollaboration;
			bool isAdmin = userRole == "Admin";
			bool isClubManager = userRole == "ClubManager";

			// Not a collaboration type - no validation needed
			if (!isClubCollaboration && !isSchoolCollaboration)
			{
				return;
			}

			// Admin creating ClubCollaboration or SchoolCollaboration
			if (isAdmin && (isClubCollaboration || isSchoolCollaboration))
			{
				// ClubCollaborationId is required
				if (!clubCollaborationId.HasValue)
				{
					throw new ArgumentException("Collaborating club must be selected for collaboration activities");
				}

				// Verify club exists
				var club = await _repo.GetClubByIdAsync(clubCollaborationId.Value);
				if (club == null)
				{
					throw new ArgumentException("Selected collaborating club does not exist");
				}

				// CollaborationPoint is required and must be 1-3
				if (!collaborationPoint.HasValue)
				{
					throw new ArgumentException("Collaboration point must be set for collaboration activities");
				}

				if (collaborationPoint.Value < 1 || collaborationPoint.Value > 3)
				{
					throw new ArgumentException("Collaboration point must be between 1 and 3");
				}

				// MovementPoint should not be set for Admin collaboration activities
				// (Admin activities don't have organizing club, so no movement points)
			}

			// Club Manager creating ClubCollaboration
			if (isClubManager && isClubCollaboration)
			{
				// ClubCollaborationId is required
				if (!clubCollaborationId.HasValue)
				{
					throw new ArgumentException("Collaborating club must be selected for club collaboration activities");
				}

				// Verify club exists
				var club = await _repo.GetClubByIdAsync(clubCollaborationId.Value);
				if (club == null)
				{
					throw new ArgumentException("Selected collaborating club does not exist");
				}

				// Cannot select same club as organizer and collaborator
				if (organizingClubId.HasValue && clubCollaborationId.Value == organizingClubId.Value)
				{
					throw new ArgumentException("Cannot collaborate with your own club");
				}

				// CollaborationPoint is required and must be 1-3
				if (!collaborationPoint.HasValue)
				{
					throw new ArgumentException("Collaboration point must be set for club collaboration activities");
				}

				if (collaborationPoint.Value < 1 || collaborationPoint.Value > 3)
				{
					throw new ArgumentException("Collaboration point must be between 1 and 3");
				}

				// MovementPoint is required and must be 1-10
				if (movementPoint < 1 || movementPoint > 10)
				{
					throw new ArgumentException("Movement point must be between 1 and 10 for club collaboration activities");
				}
			}

			// Club Manager creating SchoolCollaboration
			if (isClubManager && isSchoolCollaboration)
			{
				// ClubCollaborationId should be null
				if (clubCollaborationId.HasValue)
				{
					throw new ArgumentException("Collaborating club should not be set for school collaboration activities");
				}

				// CollaborationPoint should be null
				if (collaborationPoint.HasValue)
				{
					throw new ArgumentException("Collaboration point should not be set for school collaboration activities");
				}

				// MovementPoint is required and must be 1-10
				if (movementPoint < 1 || movementPoint > 10)
				{
					throw new ArgumentException("Movement point must be between 1 and 10 for school collaboration activities");
				}
			}
		}

		public async Task<List<BusinessObject.DTOs.Club.ClubListItemDto>> GetAvailableCollaboratingClubsAsync(int excludeClubId)
		{
			var clubs = await _repo.GetAvailableCollaboratingClubsAsync(excludeClubId);
			
			return clubs.Select(c => new BusinessObject.DTOs.Club.ClubListItemDto
			{
				Id = c.Id,
				Name = c.Name,
				LogoUrl = c.LogoUrl,
				MemberCount = c.MemberCount,
				// Set default values for other required fields
				SubName = null,
				CategoryName = "",
				IsActive = true,
				IsRecruitmentOpen = false,
				FoundedDate = DateTime.MinValue,
				Description = null,
				ActivityCount = 0,
				IsManager = false,
				IsMember = false
			}).ToList();
		}

		// ===== COLLABORATION INVITATIONS =====
		
		public async Task<List<CollaborationInvitationDto>> GetCollaborationInvitationsAsync(int clubId)
		{
			var activities = await _repo.GetPendingCollaborationInvitationsAsync(clubId);
			
			return activities.Select(a => new CollaborationInvitationDto
			{
				ActivityId = a.Id,
				Title = a.Title,
				OrganizingClubId = a.ClubId ?? 0,
				OrganizingClubName = a.Club?.Name ?? "",
				OrganizingClubLogoUrl = a.Club?.LogoUrl,
				StartTime = a.StartTime,
				EndTime = a.EndTime,
				CollaborationPoint = a.CollaborationPoint,
				ImageUrl = a.ImageUrl,
				Description = a.Description,
				Location = a.Location,
				CreatedAt = a.CreatedAt
			}).ToList();
		}

		public async Task<int> GetPendingInvitationCountAsync(int clubId)
		{
			var invitations = await _repo.GetPendingCollaborationInvitationsAsync(clubId);
			return invitations.Count;
		}

		public async Task<(bool success, string message)> AcceptCollaborationAsync(int activityId, int userId, int clubId)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			
			if (activity == null)
				return (false, "Activity not found");
			
			// Verify this club is the invited club
			if (activity.ClubCollaborationId != clubId)
				return (false, "This club is not invited to collaborate on this activity");
			
			// Check if already responded
			if (activity.CollaborationStatus != null && activity.CollaborationStatus != "Pending")
				return (false, $"Collaboration invitation has already been {activity.CollaborationStatus.ToLower()}");
			
			// Accept the collaboration
			activity.CollaborationStatus = "Accepted";
			activity.CollaborationRespondedAt = DateTime.UtcNow;
			activity.CollaborationRespondedBy = userId;
			
			// Move activity to PendingApproval status (ready for admin review)
			if (activity.Status == "PendingCollaboration")
			{
				activity.Status = "PendingApproval";
			}
			
			await _repo.UpdateAsync(activity);
			
			return (true, "Collaboration accepted successfully. Activity is now pending admin approval.");
		}

		public async Task<(bool success, string message)> RejectCollaborationAsync(int activityId, int userId, int clubId, string reason)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			
			if (activity == null)
				return (false, "Activity not found");
			
			// Verify this club is the invited club
			if (activity.ClubCollaborationId != clubId)
				return (false, "This club is not invited to collaborate on this activity");
			
			// Check if already responded
			if (activity.CollaborationStatus != null && activity.CollaborationStatus != "Pending")
				return (false, $"Collaboration invitation has already been {activity.CollaborationStatus.ToLower()}");
			
			// Reject the collaboration
			activity.CollaborationStatus = "Rejected";
			activity.CollaborationRejectionReason = reason;
			activity.CollaborationRespondedAt = DateTime.UtcNow;
			activity.CollaborationRespondedBy = userId;
			
			// Mark activity as CollaborationRejected (won't go to admin approval)
			if (activity.Status == "PendingCollaboration")
			{
				activity.Status = "CollaborationRejected";
			}
			
			await _repo.UpdateAsync(activity);
			
			return (true, "Collaboration rejected. The organizing club has been notified.");
		}

		// ================= ACTIVITY COMPLETION =================
		
		public async Task<(bool success, string message, double organizingClubPoints, double? collaboratingClubPoints)> CompleteActivityAsync(int activityId, int userId)
		{
			// Use a database transaction to ensure atomicity (Requirement 9.3)
			using var transaction = await _repo.BeginTransactionAsync();
			
			try
			{
				// Validate activity exists
				var activity = await _repo.GetByIdAsync(activityId);
				if (activity == null)
				{
					_logger.LogWarning("[COMPLETE ACTIVITY] Activity {ActivityId} not found", activityId);
					return (false, "Activity not found", 0, null);
				}

				// Log activity and club details for debugging (Requirement 9.2)
				_logger.LogInformation("[COMPLETE ACTIVITY] Starting completion for Activity {ActivityId}, ClubId={ClubId}, CollaborationClubId={CollaborationClubId}, Type={Type}, User={UserId}",
					activityId, activity.ClubId, activity.ClubCollaborationId, activity.Type, userId);

				// Validate activity status is "Approved" (Requirement 2.2)
				if (activity.Status != "Approved")
				{
					_logger.LogWarning("[COMPLETE ACTIVITY] Activity {ActivityId} status is {Status}, must be Approved. ClubId={ClubId}", 
						activityId, activity.Status, activity.ClubId);
					return (false, "Activity must be approved before completion", 0, null);
				}

				// Validate activity has ended (Requirement 2.3)
				if (activity.EndTime > DateTime.Now)
				{
					_logger.LogWarning("[COMPLETE ACTIVITY] Activity {ActivityId} has not ended yet. EndTime: {EndTime}, Now: {Now}, ClubId={ClubId}", 
						activityId, activity.EndTime, DateTime.Now, activity.ClubId);
					return (false, "Activity has not ended yet", 0, null);
				}

				// Validate not already completed - with concurrency handling (Requirement 9.4, 9.5)
				if (activity.Status == "Completed")
				{
					_logger.LogWarning("[COMPLETE ACTIVITY] Activity {ActivityId} is already completed. Possible concurrent completion attempt. ClubId={ClubId}, User={UserId}", 
						activityId, activity.ClubId, userId);
					return (false, "Activity is already completed", 0, null);
				}

				// Update activity status to "Completed" (Requirement 2.5)
				activity.Status = "Completed";
				await _repo.UpdateAsync(activity);
				
				_logger.LogInformation("[COMPLETE ACTIVITY] Activity {ActivityId} marked as Completed by User {UserId}. ClubId={ClubId}", 
					activityId, userId, activity.ClubId);

				// ========== AWARD STUDENT POINTS FROM ParticipationScore ==========
				// Skip club internal activities - they don't give student movement points
				bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
				                              activity.Type == ActivityType.ClubTraining || 
				                              activity.Type == ActivityType.ClubWorkshop;
				
				if (!isClubInternalActivity)
				{
					try
					{
						// Get all attendances with ParticipationScore for this activity
						var attendances = await _repo.GetAttendancesWithScoreAsync(activityId);
						_logger.LogInformation("[COMPLETE ACTIVITY] Found {Count} attendances with ParticipationScore for Activity {ActivityId}", 
							attendances.Count, activityId);
						
						foreach (var attendance in attendances)
						{
							if (attendance.IsPresent && attendance.ParticipationScore.HasValue && attendance.ParticipationScore.Value > 0)
							{
								var student = await _studentRepo.GetByUserIdAsync(attendance.UserId);
								if (student != null)
								{
									// criterionId: 4 = "Tham gia sự kiện" (Group 2 - Hoạt động chính trị xã hội)
									await _movementRecordService.AddScoreFromAttendanceAsync(
										studentId: student.Id,
										criterionId: 4,
										points: attendance.ParticipationScore.Value,
										activityId: activityId
									);
									_logger.LogInformation("[COMPLETE ACTIVITY] Added {Score} points to MovementRecordDetails for StudentId={StudentId}, ActivityId={ActivityId}", 
										attendance.ParticipationScore.Value, student.Id, activityId);
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "[COMPLETE ACTIVITY] Failed to award student points for Activity {ActivityId}. Error: {ErrorMessage}", 
							activityId, ex.Message);
						// Continue with club scoring even if student scoring fails
					}
				}
				else
				{
					_logger.LogInformation("[COMPLETE ACTIVITY] Skipped student scoring for club internal activity: type={Type}", activity.Type);
				}

				// ========== AWARD CLUB POINTS FROM MovementPoint ==========
				double organizingPoints = 0;
				double? collaboratingPoints = null;

				// Handle case where activity has no club (Requirement 9.1)
				if (!activity.ClubId.HasValue)
				{
					_logger.LogInformation("[COMPLETE ACTIVITY] Activity {ActivityId} has no organizing club. No club points will be awarded.", activityId);
				}
				else
				{
					try
					{
						// Call ClubMovementRecordService to calculate and award points
						var pointsResult = await _clubMovementRecordService.AwardActivityPointsAsync(activity);
						organizingPoints = pointsResult.organizingPoints;
						collaboratingPoints = pointsResult.collaboratingPoints;
						
						_logger.LogInformation("[COMPLETE ACTIVITY] Club points awarded for Activity {ActivityId}: Organizing={OrganizingPoints} (ClubId={ClubId}), Collaborating={CollaboratingPoints} (ClubId={CollaborationClubId})", 
							activityId, organizingPoints, activity.ClubId, collaboratingPoints, activity.ClubCollaborationId);
					}
					catch (Exception ex)
					{
						// Log detailed error with activity and club information (Requirement 9.2)
						_logger.LogError(ex, "[COMPLETE ACTIVITY] Failed to award club points for Activity {ActivityId}, ClubId={ClubId}, CollaborationClubId={CollaborationClubId}, Type={Type}. Error: {ErrorMessage}. Rolling back transaction.", 
							activityId, activity.ClubId, activity.ClubCollaborationId, activity.Type, ex.Message);
						
						// Rollback transaction on database errors (Requirement 9.3)
						await transaction.RollbackAsync();
						return (false, "An error occurred while calculating points. The activity was not completed.", 0, null);
					}
				}

				// Commit the transaction if everything succeeded
				await transaction.CommitAsync();
				
				_logger.LogInformation("[COMPLETE ACTIVITY] Transaction committed successfully for Activity {ActivityId}", activityId);

				// Build success message (Requirement 8.5)
				string message = "Activity completed successfully";
				if (organizingPoints > 0 || collaboratingPoints.HasValue)
				{
					if (activity.ClubId.HasValue && organizingPoints > 0)
					{
						message += $". {activity.Club?.Name ?? "Organizing club"} earned {organizingPoints} points";
					}
					if (collaboratingPoints.HasValue && collaboratingPoints.Value > 0)
					{
						message += $". {activity.CollaboratingClub?.Name ?? "Collaborating club"} earned {collaboratingPoints.Value} points";
					}
				}
				else if (activity.ClubId.HasValue)
				{
					message += ". No additional points awarded (weekly or semester limit may have been reached)";
				}

				return (true, message, organizingPoints, collaboratingPoints ?? 0);
			}
			catch (DbUpdateConcurrencyException ex)
			{
				// Handle concurrency conflicts (Requirement 9.4)
				_logger.LogError(ex, "[COMPLETE ACTIVITY] Concurrency conflict while completing Activity {ActivityId}. Another user may have modified the activity simultaneously.", activityId);
				await transaction.RollbackAsync();
				return (false, "The activity was modified by another user. Please refresh and try again.", 0, null);
			}
			catch (Exception ex)
			{
				// Handle unexpected errors with detailed logging (Requirement 9.3, 9.2)
				_logger.LogError(ex, "[COMPLETE ACTIVITY] Unexpected error completing Activity {ActivityId}. Error: {ErrorMessage}. Stack trace: {StackTrace}", 
					activityId, ex.Message, ex.StackTrace);
				
				// Ensure transaction is rolled back
				try
				{
					await transaction.RollbackAsync();
					_logger.LogInformation("[COMPLETE ACTIVITY] Transaction rolled back for Activity {ActivityId}", activityId);
				}
				catch (Exception rollbackEx)
				{
					_logger.LogError(rollbackEx, "[COMPLETE ACTIVITY] Failed to rollback transaction for Activity {ActivityId}", activityId);
				}
				
				return (false, "An error occurred while completing the activity", 0, null);
			}
		}
    }
}

