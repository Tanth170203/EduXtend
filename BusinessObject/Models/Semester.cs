using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Semester
{
    public int Id { get; set; }
    
    [Required, MaxLength(20)]
    public string Name { get; set; } = null!; // e.g., Fall2025, Spring2026
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = false;
    
    // Navigation properties
    public ICollection<MovementRecord> MovementRecords { get; set; } = new List<MovementRecord>();
    public ICollection<ClubAward> ClubAwards { get; set; } = new List<ClubAward>();
}
