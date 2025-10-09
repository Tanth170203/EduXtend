using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ClubDepartment
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
}
