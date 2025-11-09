using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Timeline item cho hoạt động - lịch trình chi tiết theo giờ
/// </summary>
public class ActivitySchedule
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to Activity
    /// </summary>
    [Required]
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    /// <summary>
    /// Thời gian bắt đầu (chỉ giờ trong ngày, VD: 15:00)
    /// </summary>
    [Required]
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// Thời gian kết thúc (chỉ giờ trong ngày, VD: 15:20)
    /// </summary>
    [Required]
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// Nội dung chương trình (VD: "Check in", "MC giới thiệu khách mời")
    /// </summary>
    [Required, MaxLength(500)]
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Mô tả chi tiết (optional)
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Ghi chú bổ sung - yêu cầu kỹ thuật, checklist
    /// VD: "Tạo Form và gửi File trả lời về BTC"
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Thứ tự sắp xếp trong timeline
    /// </summary>
    public int Order { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<ActivityScheduleAssignment> Assignments { get; set; } = new List<ActivityScheduleAssignment>();
}



