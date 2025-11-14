namespace BusinessObject.DTOs.Financial;

/// <summary>
/// DTO for Financial Dashboard Overview
/// </summary>
public class FinancialOverviewDto
{
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    
    /// <summary>
    /// Total Income for current semester
    /// </summary>
    public decimal TotalIncome { get; set; }
    
    /// <summary>
    /// Total Expenses for current semester
    /// </summary>
    public decimal TotalExpenses { get; set; }
    
    /// <summary>
    /// Current Balance (All semesters: Income - Expenses)
    /// </summary>
    public decimal Balance { get; set; }
    
    /// <summary>
    /// Member Funds collected for current semester
    /// </summary>
    public decimal MemberFunds { get; set; }
    
    /// <summary>
    /// Growth rate compared to previous semester
    /// </summary>
    public double IncomeGrowthRate { get; set; }
    
    /// <summary>
    /// Expense growth rate compared to previous semester
    /// </summary>
    public double ExpenseGrowthRate { get; set; }
    
    /// <summary>
    /// Ratio of member funds to total income (%)
    /// </summary>
    public double MemberFundRatio { get; set; }
}

/// <summary>
/// DTO for Income vs Expenses Chart Data
/// </summary>
public class IncomeExpenseChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> IncomeData { get; set; } = new();
    public List<decimal> ExpenseData { get; set; } = new();
}

/// <summary>
/// DTO for Income Sources Breakdown
/// </summary>
public class IncomeSourceDto
{
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}





