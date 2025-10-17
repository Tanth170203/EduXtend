using BusinessObject.DTOs.Activity;

namespace Services.Activities
{
    public interface IActivityService
    {
        Task<List<ActivityListItemDto>> GetAllActivitiesAsync();
        Task<List<ActivityListItemDto>> SearchActivitiesAsync(string? searchTerm, string? type, string? status, bool? isPublic, int? clubId);
        Task<ActivityDetailDto?> GetActivityByIdAsync(int id);
        Task<List<ActivityListItemDto>> GetActivitiesByClubIdAsync(int clubId);
    }
}

