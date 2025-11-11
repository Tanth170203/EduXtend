using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Giao dịch tài chính của CLB (thu chi)
/// </summary>
public class PaymentTransaction
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to Club
    /// </summary>
    [Required]
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    /// <summary>
    /// Tiêu đề giao dịch
    /// VD: "University Sponsorship", "Workshop Materials", "Equipment Purchase"
    /// </summary>
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Loại giao dịch: Income (thu) / Expense (chi)
    /// </summary>
    [Required, MaxLength(20)]
    public string Type { get; set; } = "Income";
    
    /// <summary>
    /// Phân loại chi tiết
    /// Income: sponsorship, event_revenue, member_fees, donation, other
    /// Expense: event, equipment, marketing, venue, transportation, other
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }
    
    /// <summary>
    /// Số tiền (VND)
    /// </summary>
    [Required]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Trạng thái: completed, pending, cancelled
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "completed";
    
    /// <summary>
    /// Mô tả ngắn gọn
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Ghi chú chi tiết, checklist, yêu cầu kỹ thuật
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Phương thức thanh toán: Cash, Online, BankTransfer, Momo, ZaloPay
    /// </summary>
    [MaxLength(50)]
    public string Method { get; set; } = "Cash";
    
    /// <summary>
    /// Link file chứng từ, hóa đơn, biên lai (URL hoặc path)
    /// </summary>
    [MaxLength(500)]
    public string? ReceiptUrl { get; set; }
    
    /// <summary>
    /// Link to Student (nếu là member contribution)
    /// </summary>
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    
    /// <summary>
    /// Link to Activity (nếu giao dịch liên quan đến activity cụ thể)
    /// </summary>
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    
    /// <summary>
    /// Link to Semester (kỳ học liên quan đến giao dịch)
    /// Nullable để hỗ trợ các giao dịch không thuộc kỳ học cụ thể
    /// </summary>
    public int? SemesterId { get; set; }
    public Semester? Semester { get; set; }
    
    /// <summary>
    /// Ngày giao dịch thực tế
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Người tạo giao dịch (ClubManager hoặc Admin)
    /// </summary>
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    
    /// <summary>
    /// Ngày tạo record trong hệ thống
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Ngày cập nhật lần cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
