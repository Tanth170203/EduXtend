using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Bảng điểm phong trào của CLB theo tháng
/// Tương tự MovementRecord nhưng cho CLB
/// </summary>
public class ClubMovementRecord
{
    public int Id { get; set; }
    
    [Required]
    public int ClubId { get; set; }
    
    [Required]
    public int SemesterId { get; set; }
    
    /// <summary>
    /// Tháng chấm điểm (9, 10, 11, 12)
    /// </summary>
    [Required]
    public int Month { get; set; }
    
    /// <summary>
    /// Điểm sinh hoạt nội bộ (Auto - 5đ/tuần, max 20đ)
    /// </summary>
    public double ClubMeetingScore { get; set; }
    
    /// <summary>
    /// Điểm tổ chức sự kiện (Auto - 20đ/15đ/5đ)
    /// </summary>
    public double EventScore { get; set; }
    
    /// <summary>
    /// Điểm thi đấu (Auto/Manual - 20đ/30đ/5-10đ)
    /// </summary>
    public double CompetitionScore { get; set; }
    
    /// <summary>
    /// Điểm kế hoạch (Manual - 10đ)
    /// </summary>
    public double PlanScore { get; set; }
    
    /// <summary>
    /// Điểm phối hợp (Manual - 1-10đ/1-3đ)
    /// </summary>
    public double CollaborationScore { get; set; }
    
    /// <summary>
    /// Tổng điểm tháng (max 100đ)
    /// </summary>
    public double TotalScore { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    
    // Navigation properties
    public Club Club { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public ICollection<ClubMovementRecordDetail> Details { get; set; } = new List<ClubMovementRecordDetail>();
}


