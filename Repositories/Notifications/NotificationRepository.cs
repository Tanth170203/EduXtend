using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly EduXtendContext _context;

        public NotificationRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            return await CreateAsync(notification);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.TargetUserId == userId || (n.Scope == "System" && n.TargetUserId == null))
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to last 50 notifications
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => (n.TargetUserId == userId || (n.Scope == "System" && n.TargetUserId == null)) 
                         && !n.IsRead)
                .CountAsync();
        }

        public async Task<List<Notification>> GetRecentUnreadAsync(DateTime since)
        {
            return await _context.Notifications
                .Where(n => n.CreatedAt >= since && !n.IsRead && n.TargetUserId != null)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.TargetUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            var skip = (pageNumber - 1) * pageSize;
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.TargetUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Notification> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                throw new InvalidOperationException($"Notification with ID {notificationId} not found");

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.TargetUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
