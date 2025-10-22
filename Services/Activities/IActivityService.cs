using BusinessObject.DTOs.Activity;

namespace Services.Activities
{
    public interface IActivityService
    {
        Task<List<ActivityListItemDto>> GetAllActivitiesAsync();
        Task<List<ActivityListItemDto>> SearchActivitiesAsync(string? searchTerm, string? type, string? status, bool? isPublic, int? clubId);
        Task<ActivityDetailDto?> GetActivityByIdAsync(int id);
        Task<ActivityDetailDto?> GetActivityByIdAsync(int id, int? currentUserId);
        Task<List<ActivityListItemDto>> GetActivitiesByClubIdAsync(int clubId);
        Task<ActivityDetailDto> AdminCreateAsync(int adminUserId, AdminCreateActivityDto dto);
        Task<ActivityDetailDto?> AdminUpdateAsync(int adminUserId, int id, AdminUpdateActivityDto dto);
        Task<bool> AdminDeleteAsync(int id);

		// Registration
		Task<(bool success, string message)> RegisterAsync(int userId, int activityId);
        Task<(bool success, string message)> UnregisterAsync(int userId, int activityId);

        // My Activities & Feedback
        Task<List<ActivityListItemDto>> GetMyRegistrationsAsync(int userId);
        Task<(bool success, string message)> SubmitFeedbackAsync(int userId, int activityId, int rating, string? comment);
        Task<ActivityFeedbackDto?> GetMyFeedbackAsync(int userId, int activityId);
        Task<List<AdminActivityRegistrantDto>> GetRegistrantsAsync(int adminUserId, int activityId);
        Task<(bool success, string message)> SetAttendanceAsync(int adminUserId, int activityId, int targetUserId, bool isPresent);
        Task<List<AdminActivityFeedbackDto>> GetActivityFeedbacksAsync(int adminUserId, int activityId);
    }
}

