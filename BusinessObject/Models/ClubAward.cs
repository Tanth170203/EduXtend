using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ClubAward
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    [Required, MaxLength(150)]
    public string Title { get; set; } = null!;
    
    [MaxLength(300)]
    public string? Description { get; set; }
    
    public int? SemesterId { get; set; }
    public Semester? Semester { get; set; }
    
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
