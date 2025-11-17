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
}
