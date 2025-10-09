using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ActivityRegistration
{
    public int Id { get; set; }
    
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Registered"; // Registered, Cancelled, Attended
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
