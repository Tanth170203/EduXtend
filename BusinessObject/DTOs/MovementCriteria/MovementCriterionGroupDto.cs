using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementCriteria;

/// <summary>
/// DTO for returning MovementCriterionGroup information
/// </summary>
public class MovementCriterionGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public string TargetType { get; set; } = "Student"; // "Student" or "Club"
    public int CriteriaCount { get; set; } // Number of criteria in the group
}

/// <summary>
/// DTO for creating new MovementCriterionGroup
/// </summary>
public class CreateMovementCriterionGroupDto
{
    [Required(ErrorMessage = "Criteria group name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Maximum score must be between 0 and 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Target type is required")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Target type must be 'Student' or 'Club'")]
    public string TargetType { get; set; } = "Student";
}

/// <summary>
/// DTO for updating MovementCriterionGroup
/// </summary>
public class UpdateMovementCriterionGroupDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Criteria group name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Maximum score must be between 0 and 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Target type is required")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Target type must be 'Student' or 'Club'")]
    public string TargetType { get; set; } = "Student";
}

/// <summary>
/// Detailed DTO of MovementCriterionGroup including sub-criteria
/// </summary>
public class MovementCriterionGroupDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public string TargetType { get; set; } = "Student";
    public List<MovementCriterionDto> Criteria { get; set; } = new();
}


