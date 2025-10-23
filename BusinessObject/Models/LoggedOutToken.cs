using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Represents a blacklisted JWT token (logged out)
/// Uses SHA256 hash for efficient indexing and security
/// </summary>
public class LoggedOutToken
{
    public int Id { get; set; }
    
    /// <summary>
    /// SHA256 hash of the JWT token (indexed for fast lookups)
    /// Fixed size: 64 characters (SHA256 hex)
    /// </summary>
    [Required, MaxLength(64)]
    public string TokenHash { get; set; } = null!;
    
    /// <summary>
    /// Full JWT token (for debugging/audit only, not indexed)
    /// </summary>
    [MaxLength(2000)]
    public string? TokenFull { get; set; }
    
    public int? UserId { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime LoggedOutAt { get; set; }
    
    [MaxLength(200)]
    public string? Reason { get; set; }

    // Navigation property
    public User? User { get; set; }
}
