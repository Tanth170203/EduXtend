using BusinessObject.DTOs.Financial;

namespace Services.FinancialDashboard;

public interface IFinancialDashboardService
{
    /// <summary>
    /// Get financial overview for a club
    /// </summary>
    /// <param name="clubId">Club ID</param>
    /// <param name="semesterId">Semester ID (null = current active semester)</param>
    Task<FinancialOverviewDto> GetOverviewAsync(int clubId, int? semesterId = null);
    
    /// <summary>
    /// Get income vs expenses chart data
    /// </summary>
    /// <param name="clubId">Club ID</param>
    /// <param name="year">Year for chart (null = current year)</param>
    Task<IncomeExpenseChartDto> GetIncomeExpenseChartAsync(int clubId, int? year = null);
    
    /// <summary>
    /// Get income sources breakdown
    /// </summary>
    Task<List<IncomeSourceDto>> GetIncomeSourcesAsync(int clubId, int? semesterId = null);
}

