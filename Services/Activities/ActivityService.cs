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

            // Determine if user can register
            var canRegister = activity.Status == "Approved" && 
                              activity.StartTime > DateTime.UtcNow &&
                              (!activity.MaxParticipants.HasValue || registrationCount < activity.MaxParticipants.Value);

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
                IsRegistered = false, // TODO: Check if current user is registered
                HasAttended = false   // TODO: Check if current user has attended
            };
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

        private async Task<List<ActivityListItemDto>> MapToListDto(List<Activity> activities)
        {
            var result = new List<ActivityListItemDto>();

            foreach (var activity in activities)
            {
                var registrationCount = await _repo.GetRegistrationCountAsync(activity.Id);
                var isFull = activity.MaxParticipants.HasValue && registrationCount >= activity.MaxParticipants.Value;
                var canRegister = activity.Status == "Approved" && 
                                  activity.StartTime > DateTime.UtcNow && 
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
    }
}

