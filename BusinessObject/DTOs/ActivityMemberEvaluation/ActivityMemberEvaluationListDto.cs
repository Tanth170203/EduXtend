namespace BusinessObject.DTOs.ActivityMemberEvaluation;

public class AssignmentEvaluationListDto
{
    public int AssignmentId { get; set; }
    public int? UserId { get; set; }
    
    // Họ & tên, MSSV, SĐT
    public string? FullName { get; set; }
    public string? StudentCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ResponsibleName { get; set; }
    
    // Vị trí công việc
    public string? Role { get; set; }
    
    public string ScheduleTitle { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsEvaluated { get; set; }
    
    // Điểm đánh giá (5-10)
    public int? EvaluationScore { get; set; }
    
    public DateTime? EvaluatedAt { get; set; }
}

// Type alias for backward compatibility
public class ActivityMemberEvaluationListDto : AssignmentEvaluationListDto
{
    // Additional properties for backward compatibility
    public string? UserName { get; set; }
    public double? AverageScore { get; set; }
}
