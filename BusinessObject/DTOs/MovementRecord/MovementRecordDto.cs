using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementRecord;

/// <summary>
/// DTO for returning MovementRecord information
/// </summary>
public class MovementRecordDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public double TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int DetailCount { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Detailed DTO including all score details
/// </summary>
public class MovementRecordDetailedDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public double TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public List<MovementRecordDetailItemDto> Details { get; set; } = new();
    public List<CategoryScoreDto> CategoryScores { get; set; } = new(); // Capped category scores for display
}

/// <summary>
/// Category score with capped value
/// </summary>
public class CategoryScoreDto
{
    public string GroupName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public double ActualScore { get; set; } // Sum of all details in category (uncapped)
    public double CappedScore { get; set; } // Capped score for display
    public int MaxScore { get; set; } // Category max score
    public bool IsCapped => ActualScore > MaxScore;
}

/// <summary>
/// DTO for MovementRecordDetail item
/// </summary>
public class MovementRecordDetailItemDto
{
    public int Id { get; set; }
    public int CriterionId { get; set; }
    public string? CriterionTitle { get; set; }
    public string? GroupName { get; set; }
    public int CriterionMaxScore { get; set; }
    public double Score { get; set; }
    public DateTime AwardedAt { get; set; }
    public string? ScoreType { get; set; }
    public string? Note { get; set; }
    public int? ActivityId { get; set; }
}

/// <summary>
/// DTO for creating MovementRecord
/// </summary>
public class CreateMovementRecordDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Semester ID is required")]
    public int SemesterId { get; set; }
}

/// <summary>
/// DTO for adding score to a criterion
/// </summary>
public class AddScoreDto
{
    [Required(ErrorMessage = "Movement Record ID is required")]
    public int MovementRecordId { get; set; }

    [Required(ErrorMessage = "Criterion ID is required")]
    public int CriterionId { get; set; }

    [Required(ErrorMessage = "Score is required")]
    [Range(0, 1000, ErrorMessage = "Score must be between 0 and 1000")]
    public double Score { get; set; }
}

/// <summary>
/// DTO for adjusting total score manually
/// </summary>
public class AdjustScoreDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Total score is required")]
    [Range(0, 140, ErrorMessage = "Total score must be between 0 and 140")]
    public double TotalScore { get; set; }
}

/// <summary>
/// Filter DTO for MovementRecord list
/// </summary>
public class MovementRecordFilterDto
{
    public int? StudentId { get; set; }
    public int? SemesterId { get; set; }
    public double? MinScore { get; set; }
    public double? MaxScore { get; set; }
}

/// <summary>
/// Summary statistics for a student
/// </summary>
public class StudentMovementSummaryDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int TotalSemesters { get; set; }
    public double AverageScore { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public List<MovementRecordDto> Records { get; set; } = new();
}


