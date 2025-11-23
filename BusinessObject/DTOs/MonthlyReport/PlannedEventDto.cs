namespace BusinessObject.DTOs.MonthlyReport;

public class PlannedEventDto
{
    public int? PlanId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventContent { get; set; } = string.Empty;
    public DateTime OrganizationTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int ExpectedStudents { get; set; }
    public string? RegistrationUrl { get; set; }
    public string Timeline { get; set; } = string.Empty;
    
    public List<GuestDto> Guests { get; set; } = new();
}
