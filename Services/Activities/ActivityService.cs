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
                HasAttended = false,
                AttendanceCode = activity.AttendanceCode
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
                Status = isClubActivity ? "Approved" : "PendingApproval", // Auto-approve club activities
                MaxParticipants = dto.MaxParticipants,
                MovementPoint = dto.MovementPoint,
                AttendanceCode = attendanceCode,
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
				var attendanceCount = await _repo.GetAttendanceCountAsync(a.Id);
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
					HasFeedback = hasFeedback,
					AttendedCount = attendanceCount
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
                var attendanceCount = await _repo.GetAttendanceCountAsync(activity.Id);
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
                    AttendanceCode = activity.AttendanceCode, // Include for Admin/Manager
                    CanRegister = canRegister,
                    IsRegistered = false, // TODO: Check if current user is registered
                    IsFull = isFull,
                    AttendedCount = attendanceCount
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
		
		// Convert UserId → StudentId
		var student = await _studentRepo.GetByUserIdAsync(targetUserId);
		if (student == null)
		{
			_logger.LogWarning("[ADMIN ATTENDANCE] User {UserId} is not a student, skipping movement score", targetUserId);
			return (true, "Attendance updated");
		}
		
		// CHỈ xử lý điểm phong trào cho Event/Competition/Volunteer, KHÔNG cho Club internal activities
		if (activity.Status == "Approved" && !isClubInternalActivity)
		{
			try
			{
				if (isPresent && participationScore.HasValue)
				{
					// Cộng hoặc cập nhật điểm khi Present
					_logger.LogInformation("[ADMIN ATTENDANCE] Adding movement score for ActivityId={ActivityId}, UserId={UserId}, Score={Score}, Type={Type}", 
						activityId, targetUserId, participationScore.Value, activity.Type);
					
					var criterionId = 10; // ID tiêu chí tham gia hoạt động Đoàn/Hội
					
					await _movementRecordService.AddScoreFromAttendanceAsync(
						studentId: student.Id,
						criterionId: criterionId,
						points: participationScore.Value,
						activityId: activityId
					);
					
					_logger.LogInformation("[ADMIN ATTENDANCE] Successfully added movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
				else if (!isPresent)
				{
					// Xóa điểm khi đổi sang Absent
					_logger.LogInformation("[ADMIN ATTENDANCE] Removing movement score for ActivityId={ActivityId}, UserId={UserId} (marked as Absent)", 
						activityId, targetUserId);
					
					await _movementRecordService.RemoveScoreFromAttendanceAsync(
						studentId: student.Id,
						activityId: activityId
					);
					
					_logger.LogInformation("[ADMIN ATTENDANCE] Successfully removed movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
			}
			catch (Exception ex)
			{
				// Log error nhưng vẫn trả về success cho attendance
				_logger.LogError(ex, "[ADMIN ATTENDANCE] Failed to update movement score for ActivityId={ActivityId}, UserId={UserId}", 
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
		
		// Convert UserId → StudentId
		var student = await _studentRepo.GetByUserIdAsync(targetUserId);
		if (student == null)
		{
			_logger.LogWarning("[CLUB ATTENDANCE] User {UserId} is not a student, skipping movement score", targetUserId);
			return (true, "Attendance updated");
		}
		
		// CHỈ xử lý điểm phong trào cho Event/Competition/Volunteer, KHÔNG cho Club internal activities
		if (activity.Status == "Approved" && !isClubInternalActivity)
		{
			try
			{
				if (isPresent && participationScore.HasValue)
				{
					// Cộng hoặc cập nhật điểm khi Present
					_logger.LogInformation("[CLUB ATTENDANCE] Adding movement score for ActivityId={ActivityId}, UserId={UserId}, Score={Score}, Type={Type}", 
						activityId, targetUserId, participationScore.Value, activity.Type);
					
					var criterionId = 10; // ID tiêu chí tham gia hoạt động Đoàn/Hội
					
					await _movementRecordService.AddScoreFromAttendanceAsync(
						studentId: student.Id,
						criterionId: criterionId,
						points: participationScore.Value,
						activityId: activityId
					);
					
					_logger.LogInformation("[CLUB ATTENDANCE] Successfully added movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
				else if (!isPresent)
				{
					// Xóa điểm khi đổi sang Absent
					_logger.LogInformation("[CLUB ATTENDANCE] Removing movement score for ActivityId={ActivityId}, UserId={UserId} (marked as Absent)", 
						activityId, targetUserId);
					
					await _movementRecordService.RemoveScoreFromAttendanceAsync(
						studentId: student.Id,
						activityId: activityId
					);
					
					_logger.LogInformation("[CLUB ATTENDANCE] Successfully removed movement score for StudentId={StudentId} (UserId={UserId})", 
						student.Id, targetUserId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[CLUB ATTENDANCE] Failed to update movement score for ActivityId={ActivityId}, UserId={UserId}", 
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
			await _repo.CreateAttendanceAsync(activityId, userId, true, 5, null);
			
			// Add movement score (Requirements 5.1, 5.2)
			try
			{
				var student = await _studentRepo.GetByUserIdAsync(userId);
				if (student != null)
				{
					// Check if this is a club internal activity
					bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
					                              activity.Type == ActivityType.ClubTraining || 
					                              activity.Type == ActivityType.ClubWorkshop;
					
					// Only add movement score for Event/Competition/Volunteer, NOT for club internal activities
					if (activity.Status == "Approved" && !isClubInternalActivity)
					{
						_logger.LogInformation("[STUDENT CHECK-IN] Adding movement score for ActivityId={ActivityId}, StudentId={StudentId}, Score=5, Type={Type}", 
							activityId, student.Id, activity.Type);
						
						await _movementRecordService.AddScoreFromAttendanceAsync(
							studentId: student.Id,
							criterionId: 10, // Tiêu chí tham gia hoạt động Đoàn/Hội
							points: 5,
							activityId: activityId
						);
						
						_logger.LogInformation("[STUDENT CHECK-IN] Successfully added movement score for StudentId={StudentId}", student.Id);
					}
					else
					{
						_logger.LogInformation("[STUDENT CHECK-IN] Skipped movement scoring: status={Status}, isClubInternal={IsClubInternal}, type={Type}",
							activity.Status, isClubInternalActivity, activity.Type);
					}
				}
				else
				{
					_logger.LogWarning("[STUDENT CHECK-IN] User {UserId} is not a student, skipping movement score", userId);
				}
			}
			catch (Exception ex)
			{
				// Log error but still return success for attendance
				_logger.LogError(ex, "[STUDENT CHECK-IN] Failed to add movement score for ActivityId={ActivityId}, UserId={UserId}", 
					activityId, userId);
			}
			
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
			// Check if user is Admin by checking if they have admin role
			// For now, we'll check if they're manager of the club
			bool hasPermission = false;
			
			// If activity belongs to a club, check if user is manager
			if (activity.ClubId.HasValue)
			{
				var isManager = await _repo.IsUserManagerOfClubAsync(adminOrManagerUserId, activity.ClubId.Value);
				hasPermission = isManager;
			}
			
			// Note: Admin check would need to be done at controller level with role check
			// For now, we assume if not club manager, they might be admin (checked by controller)
			
			// Validate target user has checked in (Requirements 4.4)
			var attendance = await _repo.GetAttendanceAsync(activityId, targetUserId);
			if (attendance == null) 
				return (false, "Sinh viên chưa điểm danh");
			
			// Store old score for movement record update
			var oldScore = attendance.ParticipationScore ?? 5;
			
			// Update ParticipationScore in ActivityAttendance (Requirements 4.4)
			attendance.ParticipationScore = participationScore;
			await _repo.UpdateAttendanceAsync(attendance);
			
			// Update movement score (Requirements 5.3, 5.4)
			try
			{
				var student = await _studentRepo.GetByUserIdAsync(targetUserId);
				if (student != null)
				{
					// Check if this is a club internal activity
					bool isClubInternalActivity = activity.Type == ActivityType.ClubMeeting || 
					                              activity.Type == ActivityType.ClubTraining || 
					                              activity.Type == ActivityType.ClubWorkshop;
					
					// Only update movement score for Event/Competition/Volunteer, NOT for club internal activities
					if (activity.Status == "Approved" && !isClubInternalActivity)
					{
						_logger.LogInformation("[UPDATE SCORE] Updating movement score for ActivityId={ActivityId}, StudentId={StudentId}, OldScore={OldScore}, NewScore={NewScore}", 
							activityId, student.Id, oldScore, participationScore);
						
						await _movementRecordService.UpdateScoreFromAttendanceAsync(
							studentId: student.Id,
							activityId: activityId,
							newPoints: participationScore
						);
						
						_logger.LogInformation("[UPDATE SCORE] Successfully updated movement score for StudentId={StudentId}", student.Id);
					}
					else
					{
						_logger.LogInformation("[UPDATE SCORE] Skipped movement scoring: status={Status}, isClubInternal={IsClubInternal}, type={Type}",
							activity.Status, isClubInternalActivity, activity.Type);
					}
				}
				else
				{
					_logger.LogWarning("[UPDATE SCORE] User {UserId} is not a student, skipping movement score update", targetUserId);
				}
			}
			catch (Exception ex)
			{
				// Log error but still return success for attendance update
				_logger.LogError(ex, "[UPDATE SCORE] Failed to update movement score for ActivityId={ActivityId}, UserId={UserId}", 
					activityId, targetUserId);
			}
			
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
    }
}

