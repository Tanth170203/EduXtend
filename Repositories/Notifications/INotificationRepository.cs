using BusinessObject.Models;

namespace Repositories.Notifications
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> GetByIdAsync(int id);
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<Notification>> GetRecentUnreadAsync(DateTime since);
        Task MarkAsReadAsync(int id);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int id);
    }
}
