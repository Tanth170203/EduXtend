using BusinessObject.DTOs.Activity;
using BusinessObject.Models;
using BusinessObject.Enum;
using Repositories.Activities;

namespace Services.Activities
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repo;
        public ActivityService(IActivityRepository repo) => _repo = repo;

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
                    IsFull = isFull
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
				IsPresent = x.IsPresent
			}).ToList();
		}

		public async Task<(bool success, string message)> SetAttendanceAsync(int adminUserId, int activityId, int targetUserId, bool isPresent)
		{
			var activity = await _repo.GetByIdAsync(activityId);
			if (activity == null) return (false, "Activity not found");
			if (activity.ClubId != null) return (false, "Attendance for club activities is managed by Club Manager");
			await _repo.SetAttendanceAsync(activityId, targetUserId, isPresent, adminUserId);
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

