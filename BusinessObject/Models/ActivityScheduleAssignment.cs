using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Phân công nhân sự cho từng phần trong timeline
/// </summary>
public class ActivityScheduleAssignment
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to Schedule Item
    /// </summary>
    [Required]
    public int ActivityScheduleId { get; set; }
    public ActivitySchedule ActivitySchedule { get; set; } = null!;
    
    /// <summary>
    /// Nhân sự phụ trách (nếu là user trong hệ thống)
    /// NULL nếu không phải user trong hệ thống
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    /// <summary>
    /// Tên người/nhóm phụ trách (text tự do)
    /// Dùng khi:
    /// - Khách mời bên ngoài (VD: "anh Bạch Doãn Vương")
    /// - Nhiều người (VD: "Thủy Tiên, Hiếu Dần")
    /// - Nhóm/Ban (VD: "Ban điều phối", "MC")
    /// </summary>
    [MaxLength(200)]
    public string? ResponsibleName { get; set; }
    
    /// <summary>
    /// Vai trò/Chức danh (VD: "MC", "Diễn giả", "Đón tiếp", "Điều phối")
    /// </summary>
    [MaxLength(100)]
    public string? Role { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



