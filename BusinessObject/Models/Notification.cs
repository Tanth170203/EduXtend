using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Notification
{
    public int Id { get; set; }
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Message { get; set; }
    
    public string Scope { get; set; } = "Club"; // Club / System / User
    
    public int? TargetClubId { get; set; }
    public Club? TargetClub { get; set; }
    
    [MaxLength(50)]
    public string? TargetRole { get; set; } // Member / Manager / All
    
    public int? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
    
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
