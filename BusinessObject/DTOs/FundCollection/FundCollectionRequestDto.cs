using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.FundCollection
{
    public class FundCollectionRequestDto
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal AmountPerMember { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = null!;
        public string? PaymentMethods { get; set; }
        public string? Notes { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Statistics
        public int TotalMembers { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int UnconfirmedCount { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal ExpectedTotal { get; set; }
    }

    public class CreateFundCollectionRequestDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Amount per member is required")]
        [Range(1000, 100000000, ErrorMessage = "Amount must be between 1,000 and 100,000,000 VND")]
        public decimal AmountPerMember { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Semester ID (optional - defaults to active semester if not provided)
    /// </summary>
    public int? SemesterId { get; set; }

    [StringLength(200, ErrorMessage = "Payment methods cannot exceed 200 characters")]
    public string? PaymentMethods { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        // Validation: DueDate must be in the future
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate <= DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Due date must be in the future",
                    new[] { nameof(DueDate) }
                );
            }
        }
    }

    public class UpdateFundCollectionRequestDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Amount per member is required")]
        [Range(1000, 100000000, ErrorMessage = "Amount must be between 1,000 and 100,000,000 VND")]
        public decimal AmountPerMember { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(active|completed|cancelled)$", ErrorMessage = "Invalid status")]
        public string Status { get; set; } = "active";

        [StringLength(200, ErrorMessage = "Payment methods cannot exceed 200 characters")]
        public string? PaymentMethods { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }

    public class FundCollectionRequestListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal AmountPerMember { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = null!;
        public int TotalMembers { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int UnconfirmedCount { get; set; }
        public decimal TotalCollected { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue => DueDate < DateTime.UtcNow && Status == "active";
    }
}

