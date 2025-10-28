using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementCriteria;

/// <summary>
/// DTO for returning MovementCriterion information
/// </summary>
public class MovementCriterionDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; } // Criteria group name
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public int? MinScore { get; set; }
    public string TargetType { get; set; } = "Student"; // "Student" or "Club"
    public string? DataSource { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for creating new MovementCriterion
/// </summary>
public class CreateMovementCriterionDto
{
    [Required(ErrorMessage = "Criteria group ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Criteria group ID must be greater than 0")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Maximum score must be between 0 and 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Target type is required")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Target type must be 'Student' or 'Club'")]
    public string TargetType { get; set; } = "Student";

    [MaxLength(200, ErrorMessage = "Data source cannot exceed 200 characters")]
    public string? DataSource { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating MovementCriterion
/// </summary>
public class UpdateMovementCriterionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Criteria group ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Criteria group ID must be greater than 0")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Maximum score must be between 0 and 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Target type is required")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Target type must be 'Student' or 'Club'")]
    public string TargetType { get; set; } = "Student";

    [MaxLength(200, ErrorMessage = "Data source cannot exceed 200 characters")]
    public string? DataSource { get; set; }

    public bool IsActive { get; set; } = true;
}


