namespace BusinessObject.DTOs.Activity;

public class PendingApprovalDto
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public int TotalAttendees { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
}
