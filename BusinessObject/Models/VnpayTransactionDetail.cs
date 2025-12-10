using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Chi tiết giao dịch VNPAY cho thanh toán fund collection
/// </summary>
public class VnpayTransactionDetail
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to FundCollectionPayment
    /// </summary>
    [Required]
    public int FundCollectionPaymentId { get; set; }
    public FundCollectionPayment FundCollectionPayment { get; set; } = null!;
    
    /// <summary>
    /// Mã giao dịch từ VNPAY (vnp_TxnRef)
    /// </summary>
    [Required]
    public long VnpayTransactionId { get; set; }
    
    /// <summary>
    /// Mã ngân hàng thực hiện giao dịch (vnp_BankCode)
    /// VD: VCB, BIDV, TCB, etc.
    /// </summary>
    [MaxLength(20)]
    public string? BankCode { get; set; }
    
    /// <summary>
    /// Mã giao dịch tại ngân hàng (vnp_BankTranNo)
    /// </summary>
    [MaxLength(255)]
    public string? BankTransactionId { get; set; }
    
    /// <summary>
    /// Mã phản hồi từ VNPAY (vnp_ResponseCode)
    /// 00: Thành công
    /// Các mã khác: Lỗi
    /// </summary>
    [Required, MaxLength(10)]
    public string ResponseCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Nội dung giao dịch (vnp_OrderInfo)
    /// </summary>
    [MaxLength(500)]
    public string? OrderInfo { get; set; }
    
    /// <summary>
    /// Số tiền thanh toán (vnp_Amount / 100)
    /// </summary>
    [Required]
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Ngày thanh toán từ VNPAY (vnp_PayDate)
    /// </summary>
    public DateTime? TransactionDate { get; set; }
    
    /// <summary>
    /// Trạng thái giao dịch: pending, success, failed
    /// </summary>
    [Required, MaxLength(20)]
    public string TransactionStatus { get; set; } = "pending";
    
    /// <summary>
    /// Secure hash từ VNPAY để verify
    /// </summary>
    [MaxLength(500)]
    public string? SecureHash { get; set; }
    
    /// <summary>
    /// IP address của người thanh toán
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
