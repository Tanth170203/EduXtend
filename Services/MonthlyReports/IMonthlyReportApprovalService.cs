namespace Services.MonthlyReports
{
    public interface IMonthlyReportApprovalService
    {
        Task ApproveReportAsync(int reportId, int adminId);
        Task RejectReportAsync(int reportId, int adminId, string reason);
    }
}
