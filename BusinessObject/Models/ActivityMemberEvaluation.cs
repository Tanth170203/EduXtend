using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Đánh giá thành viên hỗ trợ sự kiện cấp trường
/// </summary>
public class ActivityMemberEvaluation
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to assignment được đánh giá
    /// </summary>
    [Required]
    public int ActivityScheduleAssignmentId { get; set; }
    public ActivityScheduleAssignment Assignment { get; set; } = null!;
    
    /// <summary>
    /// Người đánh giá (Manager hoặc Admin)
    /// </summary>
    [Required]
    public int EvaluatorId { get; set; }
    public User Evaluator { get; set; } = null!;
    
    /// <summary>
    /// Điểm đánh giá trách nhiệm (1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int ResponsibilityScore { get; set; }
    
    /// <summary>
    /// Điểm đánh giá kỹ năng chuyên môn (1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int SkillScore { get; set; }
    
    /// <summary>
    /// Điểm đánh giá thái độ làm việc (1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int AttitudeScore { get; set; }
    
    /// <summary>
    /// Điểm đánh giá hiệu quả công việc (1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int EffectivenessScore { get; set; }
    
    /// <summary>
    /// Điểm trung bình (tự động tính từ 4 tiêu chí)
    /// </summary>
    [Required]
    public double AverageScore { get; set; }
    
    /// <summary>
    /// Nhận xét chi tiết về hiệu suất làm việc
    /// </summary>
    [MaxLength(2000)]
    public string? Comments { get; set; }
    
    /// <summary>
    /// Điểm mạnh của thành viên
    /// </summary>
    [MaxLength(1000)]
    public string? Strengths { get; set; }
    
    /// <summary>
    /// Điểm cần cải thiện
    /// </summary>
    [MaxLength(1000)]
    public string? Improvements { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
