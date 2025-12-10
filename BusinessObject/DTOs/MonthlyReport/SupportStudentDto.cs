namespace BusinessObject.DTOs.MonthlyReport;

public class SupportStudentDto
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public decimal? Rating { get; set; }
}
