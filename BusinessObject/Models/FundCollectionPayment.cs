using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Trạng thái thanh toán của từng member cho Fund Collection Request
/// </summary>
public class FundCollectionPayment
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to FundCollectionRequest
    /// </summary>
    [Required]
    public int FundCollectionRequestId { get; set; }
    public FundCollectionRequest FundCollectionRequest { get; set; } = null!;
    
    /// <summary>
    /// Link to ClubMember (người cần đóng phí)
    /// </summary>
    [Required]
    public int ClubMemberId { get; set; }
    public ClubMember ClubMember { get; set; } = null!;
    
    /// <summary>
    /// Số tiền cần đóng (có thể khác với AmountPerMember nếu có điều chỉnh)
    /// </summary>
    [Required]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Trạng thái: pending (chờ), paid (đã đóng), overdue (quá hạn), exempted (miễn)
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// Ngày thanh toán thực tế (khi status = paid)
    /// </summary>
    public DateTime? PaidAt { get; set; }
    
    /// <summary>
    /// Phương thức thanh toán đã sử dụng
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Link to PaymentTransaction (khi đã thanh toán và ghi vào sổ thu chi)
    /// </summary>
    public int? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
    
    /// <summary>
    /// Ghi chú (VD: "Đã xác nhận qua Momo #123456", "Miễn phí vì BCH")
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Người xác nhận thanh toán (ClubManager)
    /// </summary>
    public int? ConfirmedById { get; set; }
    public User? ConfirmedBy { get; set; }
    
    /// <summary>
    /// Số lần đã gửi reminder cho member này
    /// </summary>
    public int ReminderCount { get; set; } = 0;
    
    /// <summary>
    /// Lần gửi reminder gần nhất
    /// </summary>
    public DateTime? LastReminderAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}







