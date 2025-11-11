namespace BusinessObject.DTOs.PaymentTransaction;

public class PaymentTransactionDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;  // Income/Expense
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;  // completed, pending, cancelled
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string Method { get; set; } = null!;  // Cash, BankTransfer, etc.
    public string? ReceiptUrl { get; set; }
    
    // Student info (if related to member contribution)
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    
    // Activity info (if related to activity)
    public int? ActivityId { get; set; }
    public string? ActivityTitle { get; set; }
    
    // Semester info
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    
    // Creator info
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePaymentTransactionDto
{
    public string Title { get; set; } = null!;
    public string Type { get; set; } = "Income";  // Income/Expense
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string Method { get; set; } = "Cash";
    public string? ReceiptUrl { get; set; }
    public int? StudentId { get; set; }
    public int? ActivityId { get; set; }
    public int? SemesterId { get; set; }
    public DateTime? TransactionDate { get; set; }
}

public class UpdatePaymentTransactionDto
{
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string Method { get; set; } = null!;
    public string? ReceiptUrl { get; set; }
    public DateTime TransactionDate { get; set; }
}

