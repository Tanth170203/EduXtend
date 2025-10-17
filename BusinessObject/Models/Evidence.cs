using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class Evidence
{
    public int Id { get; set; }

    // Người nộp minh chứng (Student)
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    // Nếu minh chứng liên quan đến hoạt động trong hệ thống
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }

    // Nếu minh chứng gắn với tiêu chí phong trào (VD: Tham gia tình nguyện)
    public int? CriterionId { get; set; }
    public MovementCriterion? Criterion { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!; // Evidence name, e.g.: "Blood donation certificate"

    [MaxLength(1000)]
    public string? Description { get; set; } // Mô tả chi tiết nội dung minh chứng

    [MaxLength(255)]
    public string? FilePath { get; set; } // Link lưu file chứng minh (Drive/Local Storage)

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

    [MaxLength(255)]
    public string? ReviewerComment { get; set; } // Ghi chú từ CTSV

    // Ai duyệt minh chứng (CTSV - User với role Admin)
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Điểm phong trào được cộng (nếu có)
    public double Points { get; set; } = 0;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
