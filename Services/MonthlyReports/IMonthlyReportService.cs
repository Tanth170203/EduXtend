using BusinessObject.DTOs.MonthlyReport;

namespace Services.MonthlyReports;

public interface IMonthlyReportService
{
    /// <summary>
    /// Get all monthly reports for a club
    /// Requirements: 2.1, 2.2, 2.3, 2.4
    /// </summary>
    Task<List<MonthlyReportListDto>> GetAllReportsAsync(int clubId);
    
    /// <summary>
    /// Get all monthly reports from all clubs (Admin only)
    /// Requirements: 18.1-18.8
    /// </summary>
    Task<List<MonthlyReportListDto>> GetAllReportsForAdminAsync();
    
    /// <summary>
    /// Get basic report by ID (without fresh data aggregation)
    /// Requirements: 2.1, 2.2, 2.3, 2.4
    /// </summary>
    Task<MonthlyReportDto> GetReportByIdAsync(int reportId);
    
    /// <summary>
    /// Get report with fresh data from all sources
    /// Requirements: 3.1-3.10, 4.1-4.7, 5.1-5.15, 9.1-9.2, 15.1-15.6
    /// </summary>
    Task<MonthlyReportDto> GetReportWithFreshDataAsync(int reportId);
    
    /// <summary>
    /// Create a new monthly report
    /// Requirements: 1.1, 1.2, 1.3, 1.4
    /// </summary>
    Task<int> CreateMonthlyReportAsync(int clubId, int month, int year);
    
    /// <summary>
    /// Update editable sections of a report
    /// Requirements: 9.1-9.2, 15.1-15.6
    /// </summary>
    Task UpdateReportAsync(int reportId, UpdateMonthlyReportDto dto);
    
    /// <summary>
    /// Submit report for approval
    /// Requirements: 17.1, 17.2, 17.3, 17.4
    /// </summary>
    Task SubmitReportAsync(int reportId, int userId);
}
