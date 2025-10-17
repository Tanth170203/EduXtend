using BusinessObject.DTOs.Activity;
using BusinessObject.Models;
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

        
        public async Task<List<ActivityListItemDto>> GetPublicAsync()
        {
            var list = await _repo.GetPublicAsync();
            return await MapToListDto(list); 
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
                BannerUrl = activity.BannerUrl,
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

        public async Task<List<ActivityListItemDto>> GetActivitiesByClubIdAsync(int clubId)
        {
            var activities = await _repo.GetActivitiesByClubIdAsync(clubId);
            return await MapToListDto(activities);
        }



        public async Task<ActivityDto> CreateByAdminAsync(int adminUserId, CreateActivityDto dto)
        {
            var entity = new Activity
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Type = dto.Type,
                IsPublic = dto.IsPublic,
                ImageUrl = dto.ImageUrl,
                ClubId = null,
                CreatedById = adminUserId,
                ApprovedById = null,
                RequiresApproval = false,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);
            return ToSimpleDto(entity); 
        }

        public async Task<ActivityDto> UpdateByAdminAsync(int id, CreateActivityDto dto)
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Activity {id} not found");
            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.Location = dto.Location;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.Type = dto.Type;
            entity.IsPublic = dto.IsPublic;
            entity.ImageUrl = dto.ImageUrl;
            await _repo.UpdateAsync(entity);
            return ToSimpleDto(entity); 
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
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
                    IsRegistered = false, 
                    IsFull = isFull
                });
            }

            return result;
        }
        
        
        private static ActivityDto ToSimpleDto(Activity a) => new ActivityDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            Location = a.Location,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Type = a.Type,
            RequiresApproval = a.RequiresApproval,
            IsPublic = a.IsPublic,
            Status = a.Status,
            ImageUrl = a.ImageUrl
        };
    }
}