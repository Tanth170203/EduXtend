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
    Task NotifyMemberAboutSuccessfulVnpayPaymentAsync(int memberId, string fundCollectionTitle, decimal amount, long transactionId);
    Task NotifyClubManagerAboutVnpayPaymentAsync(int clubManagerId, string memberName, string fundCollectionTitle, decimal amount, long transactionId);
    
    // Fund collection notifications
    Task NotifyMembersAboutNewFundCollectionAsync(int clubId, string fundCollectionTitle, decimal amount, DateTime dueDate, int createdById);
    Task NotifyClubManagerAboutCashPaymentAsync(int clubManagerId, int clubId, string memberName, string fundCollectionTitle, decimal amount);
    Task NotifyClubManagerAboutBankTransferPaymentAsync(int clubManagerId, int clubId, string memberName, string fundCollectionTitle, decimal amount);
    Task NotifyMemberAboutPaymentConfirmationAsync(int memberId, int clubId, string fundCollectionTitle, decimal amount, string paymentMethod);
    Task NotifyMemberAboutPaymentReminderAsync(int memberId, int clubId, string fundCollectionTitle, decimal amount, DateTime dueDate, int daysUntilDue);
    
    // Join request notifications
    Task NotifyUserAboutJoinRequestApprovalAsync(int userId, int clubId, string clubName);
    Task NotifyUserAboutJoinRequestRejectionAsync(int userId, int clubId, string clubName);
}
