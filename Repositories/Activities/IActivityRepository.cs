using BusinessObject.Models;

namespace Repositories.Activities
{
    public interface IActivityRepository
    {
        Task<List<Activity>> GetAllAsync();
        Task<List<Activity>> SearchActivitiesAsync(string? searchTerm, string? type, string? status, bool? isPublic, int? clubId);
        Task<Activity?> GetByIdAsync(int id);
        Task<Activity?> GetByIdWithDetailsAsync(int id);
        Task<List<Activity>> GetActivitiesByClubIdAsync(int clubId);
        Task<int> GetRegistrationCountAsync(int activityId);
        Task<int> GetAttendanceCountAsync(int activityId);
        Task<int> GetFeedbackCountAsync(int activityId);
        Task<Activity> CreateAsync(Activity activity);
        Task<Activity?> UpdateAsync(Activity activity);
        Task<bool> DeleteAsync(int id);
	// Registration helpers
	Task<bool> IsRegisteredAsync(int activityId, int userId);
	Task<ActivityRegistration> AddRegistrationAsync(int activityId, int userId);
	Task<bool> IsUserMemberOfClubAsync(int userId, int clubId);
	Task<bool> IsUserManagerOfClubAsync(int userId, int clubId);
		Task<bool> HasAttendanceAsync(int activityId, int userId);
		Task<bool> HasAnyAttendanceRecordAsync(int activityId, int userId);
		Task<ActivityRegistration?> GetRegistrationAsync(int activityId, int userId);
		Task<bool> CancelRegistrationAsync(int activityId, int userId);
		Task<List<ActivityRegistration>> GetUserRegistrationsAsync(int userId);
		Task<bool> HasFeedbackAsync(int activityId, int userId);
		Task<ActivityFeedback> AddFeedbackAsync(int activityId, int userId, int rating, string? comment);
		Task<ActivityFeedback?> GetFeedbackAsync(int activityId, int userId);
		Task UpdateFeedbackAsync(ActivityFeedback feedback);
		Task<List<(int UserId, string FullName, string Email, bool? IsPresent)>> GetRegistrantsWithAttendanceAsync(int activityId);
		Task SetAttendanceAsync(int activityId, int userId, bool isPresent, int checkedById);
		Task<List<(int UserId, string FullName, string Email, int Rating, string? Comment, DateTime CreatedAt)>> GetFeedbacksAsync(int activityId);
		Task<List<(int UserId, int StudentId)>> GetClubMemberUserIdsAsync(int clubId);
    }
}

