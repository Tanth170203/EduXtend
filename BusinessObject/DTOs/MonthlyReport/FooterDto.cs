namespace BusinessObject.DTOs.MonthlyReport;

public class FooterDto
{
    public string? ApproverName { get; set; }
    public string? ApproverPosition { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewerPosition { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string CreatorPosition { get; set; } = string.Empty;
}
