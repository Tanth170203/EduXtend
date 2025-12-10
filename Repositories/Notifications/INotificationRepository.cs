using BusinessObject.Models;

namespace Repositories.Notifications
{
    public interface INotificationRepository
    {
        /// <summary>
        /// Create a new notification
        /// </summary>
        Task<Notification> CreateAsync(Notification notification);

        /// <summary>
        /// Create a new notification (alias for CreateAsync)
        /// </summary>
        Task<Notification> CreateNotificationAsync(Notification notification);

        Task<Notification?> GetByIdAsync(int id);
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<Notification>> GetRecentUnreadAsync(DateTime since);
        Task DeleteAsync(int id);

        /// <summary>
        /// Get unread notifications for a specific user
        /// </summary>
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);

        /// <summary>
        /// Get paginated notifications for a user
        /// </summary>
        Task<List<Notification>> GetNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Mark a notification as read and return the updated notification
        /// </summary>
        Task<Notification> MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task MarkAllAsReadAsync(int userId);
    }
}
