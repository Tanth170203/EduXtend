using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class User
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string Email { get; set; } = null!;
    
    [MaxLength(100)]
    public string? GoogleSubject { get; set; } // for SSO
    
    [MaxLength(255)]
    public string? AvatarUrl { get; set; }
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<ActivityRegistration> ActivityRegistrations { get; set; } = new List<ActivityRegistration>();
    public ICollection<ActivityAttendance> Attendances { get; set; } = new List<ActivityAttendance>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
}
