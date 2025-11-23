namespace BusinessObject.DTOs.MonthlyReport;

public class PlannedCompetitionDto
{
    public string CompetitionName { get; set; } = string.Empty;
    public string AuthorizedUnit { get; set; } = string.Empty;
    public DateTime CompetitionTime { get; set; }
    public string Location { get; set; } = string.Empty;
    
    public List<CompetitionParticipantDto> Participants { get; set; } = new();
}
