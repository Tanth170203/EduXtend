using BusinessObject.DTOs.Financial;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Services.FinancialDashboard;

public class FinancialDashboardService : IFinancialDashboardService
{
    private readonly EduXtendContext _context;

    public FinancialDashboardService(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<FinancialOverviewDto> GetOverviewAsync(int clubId, int? semesterId = null)
    {
        // Get current or specified semester
        Semester? semester = null;
        if (semesterId.HasValue)
        {
            semester = await _context.Semesters.FindAsync(semesterId.Value);
        }
        else
        {
            semester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
        }

        // Base query for completed/confirmed transactions (including NULL as valid)
        var completedStatuses = new[] { "completed", "confirmed" };

        // 1. TOTAL INCOME for current semester
        var incomeQuery = _context.PaymentTransactions
            .Where(t => t.ClubId == clubId 
                   && t.Type == "Income"
                   && (completedStatuses.Contains(t.Status) || t.Status == null));

        var totalIncome = semester != null
            ? await incomeQuery.Where(t => t.SemesterId == semester.Id).SumAsync(t => (decimal?)t.Amount) ?? 0
            : 0;

        // 2. TOTAL EXPENSES for current semester
        var expenseQuery = _context.PaymentTransactions
            .Where(t => t.ClubId == clubId 
                   && t.Type == "Expense"
                   && (completedStatuses.Contains(t.Status) || t.Status == null));

        var totalExpenses = semester != null
            ? await expenseQuery.Where(t => t.SemesterId == semester.Id).SumAsync(t => (decimal?)t.Amount) ?? 0
            : 0;

        // 3. BALANCE for ALL semesters (cumulative)
        var allIncome = await _context.PaymentTransactions
            .Where(t => t.ClubId == clubId 
                   && t.Type == "Income"
                   && (completedStatuses.Contains(t.Status) || t.Status == null))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var allExpenses = await _context.PaymentTransactions
            .Where(t => t.ClubId == clubId 
                   && t.Type == "Expense"
                   && (completedStatuses.Contains(t.Status) || t.Status == null))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var balance = allIncome - allExpenses;

        // 4. MEMBER FUNDS for current semester
        // Option: Count from FundCollectionPayments (more detailed, includes tracking)
        var memberFunds = semester != null
            ? await _context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                .Where(p => p.FundCollectionRequest.ClubId == clubId
                       && p.FundCollectionRequest.SemesterId == semester.Id
                       && (p.Status == "paid" || p.Status == "confirmed"))
                .SumAsync(p => (decimal?)p.Amount) ?? 0
            : 0;

        // Alternative: Count from PaymentTransactions with Category = "member_fees"
        // var memberFunds = semester != null
        //     ? await _context.PaymentTransactions
        //         .Where(t => t.ClubId == clubId
        //                && t.SemesterId == semester.Id
        //                && t.Type == "Income"
        //                && t.Category == "member_fees"
        //                && completedStatuses.Contains(t.Status))
        //         .SumAsync(t => (decimal?)t.Amount) ?? 0
        //     : 0;

        // 5. Calculate growth rates (compare with previous semester)
        double incomeGrowthRate = 0;
        double expenseGrowthRate = 0;

        if (semester != null)
        {
            var previousSemester = await _context.Semesters
                .Where(s => s.StartDate < semester.StartDate)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (previousSemester != null)
            {
                var previousIncome = await incomeQuery
                    .Where(t => t.SemesterId == previousSemester.Id)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var previousExpense = await expenseQuery
                    .Where(t => t.SemesterId == previousSemester.Id)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                incomeGrowthRate = previousIncome > 0 
                    ? (double)((totalIncome - previousIncome) / previousIncome * 100) 
                    : 0;

                expenseGrowthRate = previousExpense > 0 
                    ? (double)((totalExpenses - previousExpense) / previousExpense * 100) 
                    : 0;
            }
        }

        // 6. Calculate member fund ratio
        var memberFundRatio = totalIncome > 0 
            ? (double)(memberFunds / totalIncome * 100) 
            : 0;

        return new FinancialOverviewDto
        {
            SemesterId = semester?.Id,
            SemesterName = semester?.Name,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = balance,
            MemberFunds = memberFunds,
            IncomeGrowthRate = Math.Round(incomeGrowthRate, 1),
            ExpenseGrowthRate = Math.Round(expenseGrowthRate, 1),
            MemberFundRatio = Math.Round(memberFundRatio, 1)
        };
    }

    public async Task<IncomeExpenseChartDto> GetIncomeExpenseChartAsync(int clubId, int? year = null)
    {
        var completedStatuses = new[] { "completed", "confirmed" };
        
        // Use specified year or current year
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, 1, 1);
        var endDate = new DateTime(targetYear, 12, 31);

        // Get transactions grouped by month for the entire year
        var transactions = await _context.PaymentTransactions
            .Where(t => t.ClubId == clubId
                   && t.TransactionDate >= startDate
                   && t.TransactionDate <= endDate
                   && (completedStatuses.Contains(t.Status) || t.Status == null))
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Type })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Type = g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        // Generate labels for all 12 months
        var labels = new List<string>();
        var incomeData = new List<decimal>();
        var expenseData = new List<decimal>();

        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        for (int month = 1; month <= 12; month++)
        {
            labels.Add($"{monthNames[month - 1]} {targetYear}");

            var monthIncome = transactions
                .Where(t => t.Month == month && t.Type == "Income")
                .Sum(t => t.Total);

            var monthExpense = transactions
                .Where(t => t.Month == month && t.Type == "Expense")
                .Sum(t => t.Total);

            incomeData.Add(monthIncome);
            expenseData.Add(monthExpense);
        }

        return new IncomeExpenseChartDto
        {
            Labels = labels,
            IncomeData = incomeData,
            ExpenseData = expenseData
        };
    }

    public async Task<List<IncomeSourceDto>> GetIncomeSourcesAsync(int clubId, int? semesterId = null)
    {
        var completedStatuses = new[] { "completed", "confirmed" };

        var query = _context.PaymentTransactions
            .Where(t => t.ClubId == clubId
                   && t.Type == "Income"
                   && (completedStatuses.Contains(t.Status) || t.Status == null));

        if (semesterId.HasValue)
        {
            query = query.Where(t => t.SemesterId == semesterId.Value);
        }

        var incomeByCategory = await query
            .GroupBy(t => t.Category ?? "Other")
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        var totalIncome = incomeByCategory.Sum(x => x.Amount);

        return incomeByCategory.Select(x => new IncomeSourceDto
        {
            Category = FormatCategory(x.Category),
            Amount = x.Amount,
            Percentage = totalIncome > 0 ? Math.Round((double)(x.Amount / totalIncome * 100), 1) : 0
        }).ToList();
    }

    private static string FormatCategory(string category)
    {
        return category switch
        {
            "sponsorship" => "Sponsorship",
            "event_revenue" => "Event Revenue",
            "member_fees" => "Member Fees",
            "donation" => "Donation",
            "other" => "Other",
            _ => category
        };
    }
}

