using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class CommunicationPlan
{
    public int Id { get; set; }
    
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<CommunicationItem> Items { get; set; } = new List<CommunicationItem>();
}
