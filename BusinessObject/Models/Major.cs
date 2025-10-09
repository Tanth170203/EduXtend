using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Major
{
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string Code { get; set; } = null!; // SE, IA, AI, etc.

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!; // Software Engineering, AI, etc.

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
