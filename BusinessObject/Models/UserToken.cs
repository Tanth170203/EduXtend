using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class UserToken
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required, MaxLength(500)]
    public string RefreshToken { get; set; } = null!;
    
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    [MaxLength(200)]
    public string? DeviceInfo { get; set; }
}
