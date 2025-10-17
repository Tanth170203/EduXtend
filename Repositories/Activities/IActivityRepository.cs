using BusinessObject.Models;

namespace Repositories.Activities
{
    public interface IActivityRepository
    {
        
        Task<Activity?> GetByIdAsync(int id);
        Task<List<Activity>> GetAllAsync();
        Task<List<Activity>> GetPublicAsync();
        Task AddAsync(Activity activity);
        Task UpdateAsync(Activity activity);
        Task DeleteAsync(int id);

        
        Task<List<Activity>> SearchActivitiesAsync(string? searchTerm, string? type, string? status, bool? isPublic, int? clubId);
        Task<Activity?> GetByIdWithDetailsAsync(int id);
        Task<List<Activity>> GetActivitiesByClubIdAsync(int clubId);
        Task<int> GetRegistrationCountAsync(int activityId);
        Task<int> GetAttendanceCountAsync(int activityId);
        Task<int> GetFeedbackCountAsync(int activityId);
    }
}