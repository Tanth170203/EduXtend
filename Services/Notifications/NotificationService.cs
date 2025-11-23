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

    public async Task NotifyMemberAboutSuccessfulVnpayPaymentAsync(int memberId, string fundCollectionTitle, decimal amount, long transactionId)
    {
        var notification = new Notification
        {
            Title = "Payment successful",
            Message = $"Your VNPAY payment of {amount:N0} VND for \"{fundCollectionTitle}\" has been received successfully. Transaction ID: {transactionId}. Waiting for club manager confirmation.",
            Scope = "User",
            TargetUserId = memberId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyClubManagerAboutVnpayPaymentAsync(int clubManagerId, string memberName, string fundCollectionTitle, decimal amount, long transactionId)
    {
        var notification = new Notification
        {
            Title = "New VNPAY payment received",
            Message = $"{memberName} has paid {amount:N0} VND via VNPAY for \"{fundCollectionTitle}\". Transaction ID: {transactionId}. Please confirm the payment.",
            Scope = "User",
            TargetUserId = clubManagerId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyMembersAboutNewFundCollectionAsync(int clubId, string fundCollectionTitle, decimal amount, DateTime dueDate, int createdById)
    {
        // Get club name
        var club = await _context.Clubs.FindAsync(clubId);
        var clubName = club?.Name ?? "Club";

        // Get all active members of the club
        var clubMembers = await _context.ClubMembers
            .Include(cm => cm.Student)
            .Where(cm => cm.ClubId == clubId && cm.IsActive)
            .ToListAsync();

        // Create notification for each member
        foreach (var member in clubMembers)
        {
            var notification = new Notification
            {
                Title = "New payment request",
                Message = $"[{clubName}] New fund collection: \"{fundCollectionTitle}\" - Amount: {amount:N0} VND. Due date: {dueDate:dd/MM/yyyy}. Please pay before the deadline.",
                Scope = "User",
                TargetUserId = member.Student.UserId,
                TargetClubId = clubId,
                CreatedById = createdById,
                IsRead = false,
                CreatedAt = DateTimeHelper.Now
            };

            await _repo.CreateAsync(notification);
        }
    }

    public async Task NotifyClubManagerAboutCashPaymentAsync(int clubManagerId, int clubId, string memberName, string fundCollectionTitle, decimal amount)
    {
        var notification = new Notification
        {
            Title = "New cash payment submitted",
            Message = $"{memberName} has submitted a cash payment of {amount:N0} VND for \"{fundCollectionTitle}\". Please confirm after receiving the payment.",
            Scope = "User",
            TargetUserId = clubManagerId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyClubManagerAboutBankTransferPaymentAsync(int clubManagerId, int clubId, string memberName, string fundCollectionTitle, decimal amount)
    {
        var notification = new Notification
        {
            Title = "New bank transfer payment submitted",
            Message = $"{memberName} has submitted a bank transfer payment of {amount:N0} VND for \"{fundCollectionTitle}\". Please verify and confirm the payment.",
            Scope = "User",
            TargetUserId = clubManagerId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyMemberAboutPaymentConfirmationAsync(int memberId, int clubId, string fundCollectionTitle, decimal amount, string paymentMethod)
    {
        // Get club name
        var club = await _context.Clubs.FindAsync(clubId);
        var clubName = club?.Name ?? "Club";

        var notification = new Notification
        {
            Title = "Payment confirmed",
            Message = $"[{clubName}] Your {paymentMethod} payment of {amount:N0} VND for \"{fundCollectionTitle}\" has been confirmed by the club manager. Thank you!",
            Scope = "User",
            TargetUserId = memberId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyMemberAboutPaymentReminderAsync(int memberId, int clubId, string fundCollectionTitle, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        // Get club name
        var club = await _context.Clubs.FindAsync(clubId);
        var clubName = club?.Name ?? "Club";

        var notification = new Notification
        {
            Title = daysUntilDue > 0 ? "Payment reminder" : "Payment overdue",
            Message = daysUntilDue > 0 
                ? $"[{clubName}] Reminder: Payment for \"{fundCollectionTitle}\" ({amount:N0} VND) is due in {daysUntilDue} day(s) on {dueDate:dd/MM/yyyy}. Please pay soon."
                : $"[{clubName}] Your payment for \"{fundCollectionTitle}\" ({amount:N0} VND) is overdue since {dueDate:dd/MM/yyyy}. Please pay as soon as possible.",
            Scope = "User",
            TargetUserId = memberId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyUserAboutJoinRequestApprovalAsync(int userId, int clubId, string clubName)
    {
        var notification = new Notification
        {
            Title = "Đơn gia nhập được duyệt",
            Message = $"Chúc mừng! Đơn gia nhập câu lạc bộ [{clubName}] của bạn đã được chấp nhận. Chào mừng bạn đến với câu lạc bộ!",
            Scope = "User",
            TargetUserId = userId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }

    public async Task NotifyUserAboutJoinRequestRejectionAsync(int userId, int clubId, string clubName)
    {
        var notification = new Notification
        {
            Title = "Đơn gia nhập bị từ chối",
            Message = $"Rất tiếc, đơn gia nhập câu lạc bộ [{clubName}] của bạn đã bị từ chối. Bạn có thể nộp đơn lại khi câu lạc bộ mở tuyển thành viên.",
            Scope = "User",
            TargetUserId = userId,
            TargetClubId = clubId,
            CreatedById = 1, // System user ID
            IsRead = false,
            CreatedAt = DateTimeHelper.Now
        };

        await _repo.CreateAsync(notification);
    }
}
