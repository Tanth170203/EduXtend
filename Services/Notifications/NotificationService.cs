using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Repositories.Notifications;
using Utils;

namespace Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly EduXtendContext _context;

    public NotificationService(INotificationRepository repo, EduXtendContext context)
    {
        _repo = repo;
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        return await _repo.CreateAsync(notification);
    }

    public async Task<List<Notification>> GetByUserIdAsync(int userId)
    {
        return await _repo.GetByUserIdAsync(userId);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _repo.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(int id)
    {
        await _repo.MarkAsReadAsync(id);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
    }

    public async Task NotifyAdminsAboutNewClubNewsAsync(int clubNewsId, string newsTitle, int clubId, string clubName, int createdById)
    {
        // Get all admin users
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
        if (adminRole == null) return;

        var adminUsers = await _context.Users
            .Where(u => u.RoleId == adminRole.Id && u.IsActive)
            .ToListAsync();

        // Create notification for each admin
        foreach (var admin in adminUsers)
        {
            var notification = new Notification
            {
                Title = "New article pending approval",
                Message = $"Club {clubName} has posted a new article: \"{newsTitle}\" and is awaiting approval.",
                Scope = "System",
                TargetUserId = admin.Id,
                CreatedById = createdById,
                IsRead = false,
                CreatedAt = DateTimeHelper.Now
            };

            await _repo.CreateAsync(notification);
        }
    }

    public async Task NotifyClubManagerAboutNewsApprovalAsync(int clubNewsId, string newsTitle, int clubManagerId, bool isApproved)
    {
        var notification = new Notification
        {
            Title = isApproved ? "Article approved" : "Article rejected",
            Message = isApproved 
                ? $"Your article \"{newsTitle}\" has been approved by Admin and is now published."
                : $"Your article \"{newsTitle}\" has been rejected by Admin.",
            Scope = "User",
            TargetUserId = clubManagerId,
            CreatedById = 1, // System or Admin user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }
}
