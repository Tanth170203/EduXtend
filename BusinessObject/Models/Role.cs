using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Role
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string RoleName { get; set; } = null!; // Admin, ClubManager, ClubMember, Student
    
    [MaxLength(200)]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
