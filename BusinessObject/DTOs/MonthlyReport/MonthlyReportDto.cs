namespace BusinessObject.DTOs.MonthlyReport;

public class MonthlyReportDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReportMonth { get; set; }
    public int ReportYear { get; set; }
    public int NextMonth { get; set; }
    public int NextYear { get; set; }
    
    // Header
    public HeaderDto Header { get; set; } = new();
    
    // Part A: Current Month Activities
    public CurrentMonthActivitiesDto CurrentMonthActivities { get; set; } = new();
    
    // Part B: Next Month Plans
    public NextMonthPlansDto NextMonthPlans { get; set; } = new();
    
    // Footer
    public FooterDto Footer { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
