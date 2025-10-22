using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class JoinRequest
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int? DepartmentId { get; set; }
    public ClubDepartment? Department { get; set; }
    
    [MaxLength(500)]
    public string? Motivation { get; set; }
    
    [MaxLength(255)]
    public string? CvUrl { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    public int? ProcessedById { get; set; }
    public User? ProcessedBy { get; set; }
}
