using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;

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
        Task<ActivityDetailDto?> RejectActivityAsync(int adminUserId, int activityId, string rejectionReason);
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
        Task<(bool success, string message)> SetAttendanceAsync(int adminUserId, int activityId, int targetUserId, bool isPresent, int? participationScore = null);
        
        // Club Manager Attendance
        Task<List<AdminActivityRegistrantDto>> GetClubRegistrantsAsync(int managerUserId, int activityId);
        Task<(bool success, string message)> SetClubAttendanceAsync(int managerUserId, int activityId, int targetUserId, bool isPresent, int? participationScore = null);
        Task<List<AdminActivityFeedbackDto>> GetActivityFeedbacksAsync(int adminUserId, int activityId);
        
        // Student self check-in
        Task<(bool success, string message)> CheckInWithCodeAsync(int userId, int activityId, string attendanceCode);
        
        // Update participation score (Admin/Manager)
        Task<(bool success, string message)> UpdateParticipationScoreAsync(int adminOrManagerUserId, int activityId, int targetUserId, int participationScore);
        
        // Auto mark absent for non-attended registrants after activity ends
        Task<(int markedCount, string message)> AutoMarkAbsentAsync(int activityId, int? userId = null);
        
        // Check if user is manager of club
        Task<bool> IsUserManagerOfClubAsync(int userId, int clubId);
        
        // Collaboration validation
        Task ValidateCollaborationSettingsAsync(ActivityType type, string userRole, int? organizingClubId, int? clubCollaborationId, int? collaborationPoint, double movementPoint);
        
        // Get available clubs for collaboration
        Task<List<BusinessObject.DTOs.Club.ClubListItemDto>> GetAvailableCollaboratingClubsAsync(int excludeClubId);
        
        // Collaboration Invitations
        Task<List<CollaborationInvitationDto>> GetCollaborationInvitationsAsync(int clubId);
        Task<int> GetPendingInvitationCountAsync(int clubId);
        Task<(bool success, string message)> AcceptCollaborationAsync(int activityId, int userId, int clubId);
        Task<(bool success, string message)> RejectCollaborationAsync(int activityId, int userId, int clubId, string reason);
        
        // Schedule Management
        Task AddSchedulesToActivityAsync(int activityId, List<CreateActivityScheduleDto> schedules);
        Task UpdateActivitySchedulesAsync(int activityId, List<UpdateActivityScheduleDto> schedules);
        Task<List<ActivityScheduleDto>> GetActivitySchedulesAsync(int activityId);
        
        // Activity Completion
        Task<(bool success, string message, double organizingClubPoints, double? collaboratingClubPoints)> CompleteActivityAsync(int activityId, int userId);
    }
}

