namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class ActivityEvaluationReportDto
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public int TotalAssignments { get; set; }
    public int EvaluatedCount { get; set; }
    public int UnevaluatedCount { get; set; }
    public double? AverageScore { get; set; }
    public List<AssignmentEvaluationListDto> Members { get; set; } = new();
    
    // Additional properties for detailed statistics
    public double? OverallAverageScore { get; set; }
    public double? AvgResponsibilityScore { get; set; }
    public double? AvgSkillScore { get; set; }
    public double? AvgAttitudeScore { get; set; }
    public double? AvgEffectivenessScore { get; set; }
}
