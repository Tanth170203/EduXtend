using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Chi tiết điểm phong trào của CLB
/// Lưu từng lần chấm điểm cụ thể
/// </summary>
public class ClubMovementRecordDetail
{
    public int Id { get; set; }
    
    [Required]
    public int ClubMovementRecordId { get; set; }
    
    [Required]
    public int CriterionId { get; set; }
    
    /// <summary>
    /// Link đến Activity nếu điểm liên quan đến hoạt động cụ thể
    /// </summary>
    public int? ActivityId { get; set; }
    
    /// <summary>
    /// Điểm đạt được
    /// </summary>
    [Required]
    public double Score { get; set; }
    
    /// <summary>
    /// Loại điểm: "Auto" hoặc "Manual"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ScoreType { get; set; } = "Auto";
    
    /// <summary>
    /// Ghi chú (bắt buộc với điểm Manual)
    /// VD: "Phối hợp với CLB FCS, 20 thành viên tham gia"
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }
    
    /// <summary>
    /// Admin đã chấm điểm (chỉ với điểm Manual)
    /// </summary>
    public int? CreatedBy { get; set; }
    
    public DateTime AwardedAt { get; set; }
    
    // Navigation properties
    public ClubMovementRecord ClubMovementRecord { get; set; } = null!;
    public MovementCriterion Criterion { get; set; } = null!;
    public Activity? Activity { get; set; }
    public User? CreatedByUser { get; set; }
}


