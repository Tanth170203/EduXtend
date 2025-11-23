using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.FundCollection
{
    public class FundCollectionPaymentDto
    {
        public int Id { get; set; }
        public int FundCollectionRequestId { get; set; }
        public string FundCollectionTitle { get; set; } = null!;
        
        // Nested FundCollectionRequest details for frontend
        public FundCollectionRequestSummaryDto FundCollectionRequest { get; set; } = null!;
        
        public int ClubMemberId { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string? StudentEmail { get; set; }
        public string? AvatarUrl { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public int? PaymentTransactionId { get; set; }
        public string? Notes { get; set; }
        public int? ConfirmedById { get; set; }
        public string? ConfirmedByName { get; set; }
        public int ReminderCount { get; set; }
        public DateTime? LastReminderAt { get; set; }
        
        // VNPAY transaction details
        public int? VnpayTransactionDetailId { get; set; }
        public long? VnpayTransactionId { get; set; }
        public string? VnpayBankCode { get; set; }
        public string? VnpayTransactionStatus { get; set; }
        public DateTime? VnpayTransactionDate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Lightweight DTO for nested FundCollectionRequest in Payment
    public class FundCollectionRequestSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal AmountPerMember { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class ConfirmPaymentDto
    {
        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Paid date is required")]
        public DateTime PaidAt { get; set; }

        // Validation: PaidAt cannot be in the future
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PaidAt > DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Payment date cannot be in the future",
                    new[] { nameof(PaidAt) }
                );
            }
        }
    }

    public class SendReminderDto
    {
        [Required(ErrorMessage = "Payment IDs are required")]
        [MinLength(1, ErrorMessage = "At least one payment ID is required")]
        public List<int> PaymentIds { get; set; } = new();

        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
        public string? CustomMessage { get; set; }
    }

    public class MemberPaymentSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string? StudentEmail { get; set; }
        public string? AvatarUrl { get; set; }
        public int TotalRequests { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    public class FundCollectionStatisticsDto
    {
        public int TotalMembers { get; set; }
        public int PaidMembers { get; set; }
        public int PendingMembers { get; set; }
        public int UnconfirmedMembers { get; set; }
        public int OverdueMembers { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
        public decimal ExpectedTotal { get; set; }
        public double CollectionRate { get; set; }
    }

    public class MemberPayDto
    {
        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}

