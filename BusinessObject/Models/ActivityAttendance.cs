using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ActivityAttendance
{
    public int Id { get; set; }
    
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public bool IsPresent { get; set; }
    
    /// <summary>
    /// Đánh giá mức độ tham gia: 3 (☹️), 4 (😐), 5 (😊)
    /// Chỉ áp dụng khi IsPresent = true
    /// </summary>
    public int? ParticipationScore { get; set; }
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    public int? CheckedById { get; set; }
    public User? CheckedBy { get; set; }
    
    // GPS Check-in fields
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public double? CheckInAccuracy { get; set; } // GPS accuracy in meters
    public double? DistanceFromActivity { get; set; } // calculated distance when check-in
    
    // GPS Check-out fields
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public double? CheckOutAccuracy { get; set; }
    public DateTime? CheckOutTime { get; set; }
    
    // Check-in method: Currently only "GPS" is supported
    // Note: "Code" and "Manual" methods are temporarily disabled (hidden, not deleted)
    [MaxLength(20)]
    public string? CheckInMethod { get; set; }
}
