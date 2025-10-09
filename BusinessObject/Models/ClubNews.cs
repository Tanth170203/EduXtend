using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ClubNews
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    [MaxLength(1000)]
    public string? Content { get; set; }
    
    public string? ImageUrl { get; set; }
    public string? FacebookUrl { get; set; }
    
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = false;
    
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}
