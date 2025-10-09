namespace BusinessObject.Models;

public class ActivityAttendance
{
    public int Id { get; set; }
    
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public bool IsPresent { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    public int? CheckedById { get; set; }
    public User? CheckedBy { get; set; }
}
