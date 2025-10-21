using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementRecord;

/// <summary>
/// DTO for adding manual score by admin with specific criterion
/// </summary>
public class AddManualScoreWithCriterionDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Category ID is required")]
    [Range(1, 4, ErrorMessage = "Category ID must be between 1 and 4")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Criterion ID is required")]
    public int CriterionId { get; set; }

    [Required(ErrorMessage = "Score is required")]
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    public double Score { get; set; }

    [Required(ErrorMessage = "Comments are required")]
    [MinLength(10, ErrorMessage = "Comments must be at least 10 characters")]
    public string Comments { get; set; } = null!;

    public DateTime? AwardedDate { get; set; }
}
