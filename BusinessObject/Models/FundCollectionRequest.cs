using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

/// <summary>
/// Yêu cầu thu phí từ members (Member Fund Collection)
/// </summary>
public class FundCollectionRequest
{
    public int Id { get; set; }
    
    /// <summary>
    /// Link to Club
    /// </summary>
    [Required]
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    /// <summary>
    /// Tiêu đề yêu cầu
    /// VD: "Thu phí Q1 2024", "Đóng góp mua thiết bị"
    /// </summary>
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Mô tả chi tiết mục đích thu phí
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Số tiền mỗi member cần đóng (VND)
    /// </summary>
    [Required]
    public decimal AmountPerMember { get; set; }
    
    /// <summary>
    /// Ngày deadline để đóng phí
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Link to Semester (yêu cầu thu phí cho kỳ học nào)
    /// </summary>
    [Required]
    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    
    /// <summary>
    /// Trạng thái: active (đang thu), completed (đã xong), cancelled (đã hủy)
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// Các phương thức thanh toán chấp nhận (comma separated)
    /// VD: "Cash,BankTransfer,Momo,ZaloPay"
    /// </summary>
    [MaxLength(200)]
    public string? PaymentMethods { get; set; }
    
    /// <summary>
    /// Ghi chú bổ sung, hướng dẫn thanh toán
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Người tạo yêu cầu (ClubManager)
    /// </summary>
    [Required]
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<FundCollectionPayment> Payments { get; set; } = new List<FundCollectionPayment>();
}



