using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Plan
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Approved, Rejected
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
