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
        Task<ActivityDetailDto?> ApproveActivityAsync(int adminUserId, int activityId);
        Task<ActivityDetailDto?> RejectActivityAsync(int adminUserId, int activityId);
        // Club Manager
        Task<List<ActivityListItemDto>> GetActivitiesByManagerIdAsync(int managerUserId);
        Task<ActivityDetailDto> ClubCreateAsync(int managerUserId, int clubId, ClubCreateActivityDto dto);
        Task<ActivityDetailDto?> ClubUpdateAsync(int managerUserId, int id, ClubCreateActivityDto dto);
        Task<bool> ClubDeleteAsync(int managerUserId, int id);

		// Registration
		Task<(bool success, string message)> RegisterAsync(int userId, int activityId);
        Task<(bool success, string message)> UnregisterAsync(int userId, int activityId);

        // My Activities & Feedback
        Task<List<ActivityListItemDto>> GetMyRegistrationsAsync(int userId);
        Task<(bool success, string message)> SubmitFeedbackAsync(int userId, int activityId, int rating, string? comment);
        Task<ActivityFeedbackDto?> GetMyFeedbackAsync(int userId, int activityId);
        
        // Admin Attendance
        Task<List<AdminActivityRegistrantDto>> GetRegistrantsAsync(int adminUserId, int activityId);
        Task<(bool success, string message)> SetAttendanceAsync(int adminUserId, int activityId, int targetUserId, bool isPresent);
        
        // Club Manager Attendance
        Task<List<AdminActivityRegistrantDto>> GetClubRegistrantsAsync(int managerUserId, int activityId);
        Task<(bool success, string message)> SetClubAttendanceAsync(int managerUserId, int activityId, int targetUserId, bool isPresent);
        Task<List<AdminActivityFeedbackDto>> GetActivityFeedbacksAsync(int adminUserId, int activityId);
    }
}

