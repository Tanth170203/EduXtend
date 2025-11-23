using BusinessObject.Models;

namespace Services.Notifications;

public interface INotificationService
{
    Task<Notification> CreateAsync(Notification notification);
    Task<List<Notification>> GetByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteAsync(int id);
    
    // Specific notification creators
    Task NotifyAdminsAboutNewClubNewsAsync(int clubNewsId, string newsTitle, int clubId, string clubName, int createdById);
    Task NotifyClubManagerAboutNewsApprovalAsync(int clubNewsId, string newsTitle, int clubManagerId, bool isApproved);
    
    // Monthly Report Approval notifications
    /// <summary>
    /// Send notification to user
    /// Requirements: 7.1, 7.2, 7.3, 7.4
    /// </summary>
    Task<Notification> SendNotificationAsync(int userId, string type, string message, int? reportId = null);
    
    /// <summary>
    /// Get unread notifications for user
    /// Requirements: 7.1, 7.2
    /// </summary>
    Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
    
    /// <summary>
    /// Get paginated notifications for user
    /// Requirements: 7.3
    /// </summary>
    Task<List<Notification>> GetNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10);
    
    /// <summary>
    /// Mark notification as read
    /// Requirements: 7.4
    /// </summary>
    Task<Notification> MarkNotificationAsReadAsync(int notificationId);
}
