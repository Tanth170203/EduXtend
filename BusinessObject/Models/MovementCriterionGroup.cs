using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class MovementCriterionGroup
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? Description { get; set; }

    public int MaxScore { get; set; }

    // Loại đối tượng áp dụng: Student / Club
    [Required, MaxLength(20)]
    public string TargetType { get; set; } = "Student"; // hoặc "Club"

    public ICollection<MovementCriterion> Criteria { get; set; } = new List<MovementCriterion>();
}
