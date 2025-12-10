namespace BusinessObject.DTOs.MonthlyReport;

public class InternalMeetingDto
{
    public int ActivityId { get; set; }
    public DateTime MeetingTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
