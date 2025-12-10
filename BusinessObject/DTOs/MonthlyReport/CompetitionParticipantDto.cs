namespace BusinessObject.DTOs.MonthlyReport;

public class CompetitionParticipantDto
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Achievement { get; set; }
    public string? Note { get; set; }
}
