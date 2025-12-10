namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class MemberEvaluationHistoryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int TotalEvaluations { get; set; }
    public double? OverallAverageScore { get; set; }
    public List<AssignmentEvaluationDto> Evaluations { get; set; } = new();
}
