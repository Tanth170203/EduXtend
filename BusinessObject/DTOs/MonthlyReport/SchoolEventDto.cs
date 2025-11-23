namespace BusinessObject.DTOs.MonthlyReport;

public class SchoolEventDto
{
    public int ActivityId { get; set; }
    public DateTime EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ActualParticipants { get; set; }
    
    // Participants table
    public List<ParticipantDto> Participants { get; set; } = new();
    
    // Event Evaluation
    public EventEvaluationDto Evaluation { get; set; } = new();
    
    // Club Support Members
    public List<SupportMemberDto> SupportMembers { get; set; } = new();
    
    // Timeline
    public string Timeline { get; set; } = string.Empty;
    
    // Links
    public string? FeedbackUrl { get; set; }
    public string? MediaUrls { get; set; }
}
