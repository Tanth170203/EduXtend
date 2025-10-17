using BusinessObject.DTOs.Activity;

namespace Services.Activities
{
    public interface IActivityService
    {
        
        Task<List<ActivityListItemDto>> GetAllActivitiesAsync();
        
        Task<List<ActivityListItemDto>> GetPublicAsync();
        
        Task<List<ActivityListItemDto>> SearchActivitiesAsync(string? searchTerm, string? type, string? status, bool? isPublic, int? clubId);
        
        Task<ActivityDetailDto?> GetActivityByIdAsync(int id);
        
        Task<List<ActivityListItemDto>> GetActivitiesByClubIdAsync(int clubId);
        

        Task<ActivityDto> CreateByAdminAsync(int adminUserId, CreateActivityDto dto);
        Task<ActivityDto> UpdateByAdminAsync(int id, CreateActivityDto dto);
        Task DeleteAsync(int id);
    }
}