using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class MovementCriterion
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public MovementCriterionGroup Group { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int MaxScore { get; set; }

    // Dùng lại cho rõ ràng
    [Required, MaxLength(20)]
    public string TargetType { get; set; } = "Student"; // or "Club"

    [MaxLength(200)]
    public string? DataSource { get; set; }

    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<MovementRecordDetail> RecordDetails { get; set; } = new List<MovementRecordDetail>();
    public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
}
