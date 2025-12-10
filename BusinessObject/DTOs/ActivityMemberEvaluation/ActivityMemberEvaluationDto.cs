namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class AssignmentEvaluationDto
{
    public int AssignmentId { get; set; }
    public int? EvaluatorId { get; set; }
    public string? EvaluatorName { get; set; }
    
    // Member info (Họ & tên, MSSV, SĐT)
    public int? UserId { get; set; }
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ResponsibleName { get; set; }
    
    // Vị trí công việc
    public string? Role { get; set; }
    
    // Schedule info
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public string ScheduleTitle { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    // Evaluation status
    public bool IsEvaluated { get; set; }
    
    // Điểm đánh giá (5-10)
    public int? EvaluationScore { get; set; }
    
    public DateTime? EvaluatedAt { get; set; }
    public DateTime? EvaluationUpdatedAt { get; set; }
}

// Type alias for backward compatibility
public class ActivityMemberEvaluationDto : AssignmentEvaluationDto
{
    // Additional properties for detailed evaluation
    public int Id { get; set; }
    public int ActivityScheduleAssignmentId { get; set; }
    public string? UserName { get; set; }
    public int ResponsibilityScore { get; set; }
    public int SkillScore { get; set; }
    public int AttitudeScore { get; set; }
    public int EffectivenessScore { get; set; }
    public double AverageScore { get; set; }
    public string? Comments { get; set; }
    public string? Strengths { get; set; }
    public string? Improvements { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
