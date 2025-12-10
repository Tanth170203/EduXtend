namespace BusinessObject.DTOs.MonthlyReport;

public class CompetitionDto
{
    public int ActivityId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string OrganizingUnit { get; set; } = string.Empty;
    
    public List<CompetitionParticipantDto> Participants { get; set; } = new();
}
