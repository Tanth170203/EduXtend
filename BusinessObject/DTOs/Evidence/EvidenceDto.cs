using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusinessObject.DTOs.Evidence;

/// <summary>
/// DTO for returning Evidence information
/// </summary>
public class EvidenceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int? ActivityId { get; set; }
    public string? ActivityTitle { get; set; }
    public int? CriterionId { get; set; }
    public string? CriterionTitle { get; set; }
    public int CriterionMaxScore { get; set; } // ✅ NEW: Max points for this criterion
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? FilePath { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public string? ReviewerComment { get; set; }
    public int? ReviewedById { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public double Points { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// DTO for creating new Evidence (from API with file upload)
/// </summary>
public class CreateEvidenceDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public int StudentId { get; set; }

    public int? ActivityId { get; set; }

    public int? CriterionId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    // File will be uploaded separately and FilePath will be set after upload
    public IFormFile? File { get; set; }
    
    // This will be set after file is uploaded to Cloudinary
    public string? FilePath { get; set; }
}

/// <summary>
/// DTO for creating evidence from WebFE (without file, only metadata)
/// </summary>
public class CreateEvidenceRequestDto
{
    [Required(ErrorMessage = "Student ID is required")]
    public int StudentId { get; set; }

    public int? ActivityId { get; set; }

    public int? CriterionId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating Evidence
/// </summary>
public class UpdateEvidenceDto
{
    public int Id { get; set; }

    public int? ActivityId { get; set; }

    public int? CriterionId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [MaxLength(255, ErrorMessage = "File path cannot exceed 255 characters")]
    public string? FilePath { get; set; }
}

/// <summary>
/// DTO for reviewing Evidence (Admin/CTSV)
/// </summary>
public class ReviewEvidenceDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Status must be 'Approved' or 'Rejected'")]
    public string Status { get; set; } = null!;

    [MaxLength(255, ErrorMessage = "Comment cannot exceed 255 characters")]
    public string? ReviewerComment { get; set; }

    [Range(0, 1000, ErrorMessage = "Points must be between 0 and 1000")]
    public double Points { get; set; }

    [Required(ErrorMessage = "Reviewer ID is required")]
    public int ReviewedById { get; set; }

    /// <summary>
    /// Optional: Admin can change the criterion when reviewing
    /// </summary>
    public int? CriterionId { get; set; }
}

/// <summary>
/// Filter DTO for Evidence list
/// </summary>
public class EvidenceFilterDto
{
    public int? StudentId { get; set; }
    public int? CriterionId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}


