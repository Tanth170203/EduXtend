namespace BusinessObject.DTOs.StudentFinance;

public class StudentPendingPaymentDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string? ClubLogoUrl { get; set; }
    public string PaymentTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public string Status { get; set; } = string.Empty; // "pending", "unconfirmed"
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; } // Within 3 days
}
