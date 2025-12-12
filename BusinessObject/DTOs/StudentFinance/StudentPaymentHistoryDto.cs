namespace BusinessObject.DTOs.StudentFinance;

public class StudentPaymentHistoryDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string? ClubLogoUrl { get; set; }
    public string PaymentTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "paid"
    public string? ConfirmedByName { get; set; }
    public int? PaymentTransactionId { get; set; }
    public string? Notes { get; set; }
}
