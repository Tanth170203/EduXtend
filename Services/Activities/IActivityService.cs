using BusinessObject.DTOs.Activity;

namespace Services.Activities;

public interface IActivityService
{
    Task<List<ActivityDto>> GetAllAsync();
    Task<List<ActivityDto>> GetPublicAsync();
    Task<ActivityDto> CreateByAdminAsync(int adminUserId, CreateActivityDto dto);
    Task<ActivityDto?> GetByIdAsync(int id);
    Task<ActivityDto> UpdateByAdminAsync(int id, CreateActivityDto dto);
    Task DeleteAsync(int id);
}


