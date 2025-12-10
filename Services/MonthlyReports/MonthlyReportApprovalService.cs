using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Repositories.MonthlyReports;
using Services.Notifications;
using Utils;

namespace Services.MonthlyReports
{
    public class MonthlyReportApprovalService : IMonthlyReportApprovalService
    {
        private readonly IMonthlyReportRepository _reportRepo;
        private readonly INotificationService _notificationService;
        private readonly EduXtendContext _context;

        public MonthlyReportApprovalService(
            IMonthlyReportRepository reportRepo,
            INotificationService notificationService,
            EduXtendContext context)
        {
            _reportRepo = reportRepo;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task ApproveReportAsync(int reportId, int adminId)
        {
            // Get the report
            var report = await _reportRepo.GetByIdAsync(reportId);
            if (report == null)
            {
                throw new InvalidOperationException($"Report with ID {reportId} not found");
            }

            // Validate status
            if (report.Status != "PendingApproval")
            {
                throw new InvalidOperationException($"Report must be in PendingApproval status to be approved. Current status: {report.Status}");
            }

            // Update report status
            report.Status = "Approved";
            report.ApprovedById = adminId;
            report.ApprovedAt = DateTimeHelper.Now;

            await _reportRepo.UpdateAsync(report);

            // Get ClubManager to notify
            var clubManager = await GetClubManagerAsync(report.ClubId);
            if (clubManager != null)
            {
                // Create notification for ClubManager
                var notification = new Notification
                {
                    Title = "Báo cáo được phê duyệt",
                    Message = $"Báo cáo tháng {report.ReportMonth}/{report.ReportYear} của CLB đã được Admin phê duyệt.",
                    Scope = "User",
                    TargetUserId = clubManager.Id,
                    CreatedById = adminId,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.Now
                };

                await _notificationService.CreateAsync(notification);
            }
        }

        public async Task RejectReportAsync(int reportId, int adminId, string reason)
        {
            // Validate reason
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Rejection reason is required", nameof(reason));
            }

            // Get the report
            var report = await _reportRepo.GetByIdAsync(reportId);
            if (report == null)
            {
                throw new InvalidOperationException($"Report with ID {reportId} not found");
            }

            // Validate status
            if (report.Status != "PendingApproval")
            {
                throw new InvalidOperationException($"Report must be in PendingApproval status to be rejected. Current status: {report.Status}");
            }

            // Update report status
            report.Status = "Rejected";
            report.RejectionReason = reason;
            report.ApprovedById = null;
            report.ApprovedAt = null;

            await _reportRepo.UpdateAsync(report);

            // Get ClubManager to notify
            var clubManager = await GetClubManagerAsync(report.ClubId);
            if (clubManager != null)
            {
                // Create notification for ClubManager with rejection reason
                var notification = new Notification
                {
                    Title = "Báo cáo bị từ chối",
                    Message = $"Báo cáo tháng {report.ReportMonth}/{report.ReportYear} của CLB đã bị Admin từ chối. Lý do: {reason}",
                    Scope = "User",
                    TargetUserId = clubManager.Id,
                    CreatedById = adminId,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.Now
                };

                await _notificationService.CreateAsync(notification);
            }
        }

        private async Task<User?> GetClubManagerAsync(int clubId)
        {
            // Find the ClubManager for this club
            var clubMember = await _context.ClubMembers
                .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
                .Where(cm => cm.ClubId == clubId && 
                            cm.IsActive && 
                            cm.RoleInClub == "Manager")
                .FirstOrDefaultAsync();

            return clubMember?.Student?.User;
        }
    }
}
