namespace BusinessObject.DTOs.MonthlyReport;

public class MonthlyReportListDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
