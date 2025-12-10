namespace BusinessObject.DTOs.MonthlyReport;

public class HeaderDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public string MainTitle { get; set; } = string.Empty;
    public string SubTitle { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string CreatorPosition { get; set; } = string.Empty;
}
