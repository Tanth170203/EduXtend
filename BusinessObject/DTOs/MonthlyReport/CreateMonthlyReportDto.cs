namespace BusinessObject.DTOs.MonthlyReport;

public class CreateMonthlyReportDto
{
    public int ClubId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
