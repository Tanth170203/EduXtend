namespace BusinessObject.DTOs.StudentFinance;

public class StudentFinanceStatisticsDto
{
    public decimal TotalPendingAmount { get; set; }
    public int TotalPendingCount { get; set; }
    public int OverdueCount { get; set; }
    public int ClubsWithPendingPayments { get; set; }
    public decimal TotalPaidThisSemester { get; set; }
    public int TotalPaidCount { get; set; }
}
