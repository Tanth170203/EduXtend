using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class UpdateAssignmentEvaluationDto
{
    [Required, Range(5, 10)]
    public int EvaluationScore { get; set; }
}
