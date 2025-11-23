using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class CreateActivityMemberEvaluationDto
{
    [Required]
    public int ActivityScheduleAssignmentId { get; set; }

    [Required, Range(1, 10)]
    public int ResponsibilityScore { get; set; }

    [Required, Range(1, 10)]
    public int SkillScore { get; set; }

    [Required, Range(1, 10)]
    public int AttitudeScore { get; set; }

    [Required, Range(1, 10)]
    public int EffectivenessScore { get; set; }

    [MaxLength(2000)]
    public string? Comments { get; set; }

    [MaxLength(1000)]
    public string? Strengths { get; set; }

    [MaxLength(1000)]
    public string? Improvements { get; set; }
}
