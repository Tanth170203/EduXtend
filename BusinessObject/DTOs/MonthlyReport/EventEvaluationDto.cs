namespace BusinessObject.DTOs.MonthlyReport;

public class EventEvaluationDto
{
    // Quantity metrics
    public int ExpectedCount { get; set; }
    public int ActualCount { get; set; }
    public string? ReasonIfLess { get; set; }
    
    // Quality metrics (scale 1-10)
    public decimal? CommunicationScore { get; set; }
    public decimal? OrganizationScore { get; set; }
    public string? McHostEvaluation { get; set; }
    public string? SpeakerPerformerEvaluation { get; set; }
    
    // Summary
    public string? Achievements { get; set; }
    public string? Limitations { get; set; }
    public string? ProposedSolutions { get; set; }
}
