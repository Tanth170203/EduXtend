using BusinessObject.DTOs.Activity;
using BusinessObject.Models;
using BusinessObject.Enum;
using Repositories.Activities;
using Repositories.Students;
using Services.MovementRecords;
using Microsoft.Extensions.Logging;

namespace Services.Activities
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repo;
        private readonly IStudentRepository _studentRepo;
        private readonly IMovementRecordService _movementRecordService;
        private readonly ILogger<ActivityService> _logger;
        
        public ActivityService(
            IActivityRepository repo,
            IStudentRepository studentRepo,
            IMovementRecordService movementRecordService,
            ILogger<ActivityService> logger)
        {
            _repo = repo;
            _studentRepo = studentRepo;
            _movementRecordService = movementRecordService;
            _logger = logger;
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

        public async Task<ActivityDetailDto?> GetActivityByIdAsync(int id)
        {
            var activity = await _repo.GetByIdWithDetailsAsync(id);
            if (activity == null) return null;

            var registrationCount = await _repo.GetRegistrationCountAsync(id);
            var attendanceCount = await _repo.GetAttendanceCountAsync(id);
            var feedbackCount = await _repo.GetFeedbackCountAsync(id);

            // Determine if user can register (allow during upcoming and ongoing)
            var isFullDetail = activity.MaxParticipants.HasValue && registrationCount >= activity.MaxParticipants.Value;
            var canRegister = activity.Status == "Approved" &&
                              activity.EndTime > DateTime.UtcNow &&
                              !isFullDetail;

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
                HasAttended = false
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
			
			// Check membership for club-only activities
			if (!basic.IsPublic && basic.ClubId.HasValue)
			{
				var isMember = await _repo.IsUserMemberOfClubAsync(currentUserId.Value, basic.ClubId.Value);
				if (!isMember)
				{
					basic.CanRegister = false;
				}
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
                CreatedAt = DateTime.UtcNow
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
            // Get all activities where CreatedById = managerUserId AND ClubId is not null
            var allActivities = await _repo.GetAllAsync();
            var managerActivities = allActivities.Where(a => a.CreatedById == managerUserId && a.ClubId.HasValue).ToList();
            return await MapToListDto(managerActivities);
        }

        public async Task<ActivityDetailDto> ClubCreateAsync(int managerUserId, int clubId, ClubCreateActivityDto dto)
        {
            if (dto.StartTime >= dto.EndTime)
                throw new ArgumentException("StartTime must be earlier than EndTime");

            // Check if activity type is Club Activity (internal activities)
            bool isClubActivity = dto.Type == ActivityType.ClubMeeting || 
                                 dto.Type == ActivityType.ClubTraining || 
                                 dto.Type == ActivityType.ClubWorkshop;

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
                Status = isClubActivity ? "Approved" : "PendingApproval", // Auto-approve club activities
                MaxParticipants = dto.MaxParticipants,
                MovementPoint = dto.MovementPoint,
                CreatedAt = DateTime.UtcNow
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
            // Reset approval if was rejected
            if (existing.Status == "Rejected")
            {
                existing.Status = "PendingApproval";
                existing.ApprovedById = null;
                existing.ApprovedAt = null;
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
				// simply switch back to Registered
				existing.Status = "Registered";
				return (true, "Activity registered");
			}
			if (activity.MaxParticipants.HasValue)
			{
				var current = await _repo.GetRegistrationCountAsync(activityId);
				if (current >= activity.MaxParticipants.Value) return (false, "Registration is full");
			}

			// Visibility check
			if (!activity.IsPublic)
			{
				if (!activity.ClubId.HasValue) return (false, "This activity is for Club members only");
				var isMember = await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
				if (!isMember) return (false, "This activity is for Club members only");
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
					CanRegister = false,
					IsRegistered = true,
					IsFull = isFull,
					HasAttended = hasAttended,
					HasFeedback = hasFeedback
				});
			}
			return list;
		}

		public async Task<(bool success, string message)> SubmitFeedbackAsync(int userId, int activityId, int rating, string? comment)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return (false, "Activity not found");
			if (rating < 1 || rating > 5) return (false, "Rating must be 1-5");

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
                var attendanceCount = await _repo.GetMarkedAttendanceCountAsync(activity.Id);
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
                    CanRegister = canRegister,
                    IsRegistered = false, // TODO: Check if current user is registered
                    IsFull = isFull,
                    AttendanceCount = attendanceCount
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

			var list = await _repo.GetRegistrantsWithAttendanceAsync(activityId);
			return list.Select(x => new AdminActivityRegistrantDto
			{
				UserId = x.UserId,
				FullName = x.FullName,
				Email = x.Email,
				IsPresent = x.IsPresent,
				ParticipationScore = x.ParticipationScore
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
		
		// Kiểm tra xem có phải hoạt động CLB nội bộ không
		bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
		                              activity.Type == ActivityType.ClubTraining || 
		                              activity.Type == ActivityType.ClubWorkshop;
		
		// CHỈ cộng điểm phong trào cho Event/Competition/Volunteer, KHÔNG cộng cho Club internal activities
		if (isPresent && participationScore.HasValue && activity.Status == "Approved" && !isClubInternalActivity)
		{
			try
			{
				_logger.LogInformation("[ADMIN ATTENDANCE] Adding movement score for ActivityId={ActivityId}, UserId={UserId}, Score={Score}, Type={Type}", 
					activityId, targetUserId, participationScore.Value, activity.Type);
				
				// Convert UserId → StudentId
				var student = await _studentRepo.GetByUserIdAsync(targetUserId);
				if (student == null)
				{
					_logger.LogWarning("[ADMIN ATTENDANCE] User {UserId} is not a student, skipping movement score", targetUserId);
				}
				else
				{
					// Tìm tiêu chí "Hoạt động xã hội, từ thiện, tình nguyện" thuộc Group 2
					var criterionId = 10; // ID tiêu chí tham gia hoạt động Đoàn/Hội
					
					await _movementRecordService.AddScoreFromAttendanceAsync(
						studentId: student.Id, // ✅ Dùng Student.Id, không phải UserId
						criterionId: criterionId,
						points: participationScore.Value,
						activityId: activityId
					);
					
					_logger.LogInformation("[ADMIN ATTENDANCE] Successfully added movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
			}
			catch (Exception ex)
			{
				// Log error nhưng vẫn trả về success cho attendance
				_logger.LogError(ex, "[ADMIN ATTENDANCE] Failed to add movement score for ActivityId={ActivityId}, UserId={UserId}, CriterionId=10", 
					activityId, targetUserId);
			}
		}
		else
		{
			_logger.LogInformation("[ADMIN ATTENDANCE] Skipped movement scoring: isPresent={IsPresent}, hasScore={HasScore}, status={Status}, isClubInternal={IsClubInternal}, type={Type}",
				isPresent, participationScore.HasValue, activity.Status, isClubInternalActivity, activity.Type);
		}
		
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

		var list = await _repo.GetRegistrantsWithAttendanceAsync(activityId);
		return list.Select(x => new AdminActivityRegistrantDto
		{
			UserId = x.UserId,
			FullName = x.FullName,
			Email = x.Email,
			IsPresent = x.IsPresent,
			ParticipationScore = x.ParticipationScore
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
		
		// Kiểm tra xem có phải hoạt động CLB nội bộ không
		bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
		                              activity.Type == ActivityType.ClubTraining || 
		                              activity.Type == ActivityType.ClubWorkshop;
		
		// CHỈ cộng điểm phong trào cho Event/Competition/Volunteer, KHÔNG cộng cho Club internal activities
		if (isPresent && participationScore.HasValue && activity.Status == "Approved" && !isClubInternalActivity)
		{
			try
			{
				_logger.LogInformation("[CLUB ATTENDANCE] Adding movement score for ActivityId={ActivityId}, UserId={UserId}, Score={Score}, Type={Type}", 
					activityId, targetUserId, participationScore.Value, activity.Type);
				
				// Convert UserId → StudentId
				var student = await _studentRepo.GetByUserIdAsync(targetUserId);
				if (student == null)
				{
					_logger.LogWarning("[CLUB ATTENDANCE] User {UserId} is not a student, skipping movement score", targetUserId);
				}
				else
				{
					var criterionId = 10; // ID tiêu chí tham gia hoạt động Đoàn/Hội
					
					await _movementRecordService.AddScoreFromAttendanceAsync(
						studentId: student.Id, // ✅ Dùng Student.Id, không phải UserId
						criterionId: criterionId,
						points: participationScore.Value,
						activityId: activityId
					);
					
					_logger.LogInformation("[CLUB ATTENDANCE] Successfully added movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[CLUB ATTENDANCE] Failed to add movement score for ActivityId={ActivityId}, UserId={UserId}, CriterionId=10", 
					activityId, targetUserId);
			}
		}
		else
		{
			_logger.LogInformation("[CLUB ATTENDANCE] Skipped movement scoring: isPresent={IsPresent}, hasScore={HasScore}, status={Status}, isClubInternal={IsClubInternal}, type={Type}",
				isPresent, participationScore.HasValue, activity.Status, isClubInternalActivity, activity.Type);
		}
		
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
    }
}

