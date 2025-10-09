using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Represents a blacklisted JWT token (logged out)
/// </summary>
public class LoggedOutToken
{
    public int Id { get; set; }
    
    [Required, MaxLength(2000)]
    public string Token { get; set; } = null!;
    
    public int? UserId { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime LoggedOutAt { get; set; }
    
    [MaxLength(200)]
    public string? Reason { get; set; }

    // Navigation property
    public User? User { get; set; }
}
