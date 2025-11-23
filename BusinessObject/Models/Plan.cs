using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Models;

public class Plan
{
    public int Id { get; set; }
    
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Approved, Rejected
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    
    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    // Monthly Report fields
    [MaxLength(50)]
    public string? ReportType { get; set; } // "Monthly" or null for regular plans
    
    public int? ReportMonth { get; set; } // 1-12
    
    public int? ReportYear { get; set; } // 2025, etc.
    
    // Lưu danh sách Activity IDs liên quan (JSON array)
    [Column(TypeName = "nvarchar(max)")]
    public string? ReportActivityIds { get; set; } // "[123,124,125]"
    
    // Optional: Snapshot metadata
    [Column(TypeName = "nvarchar(max)")]
    public string? ReportSnapshot { get; set; } // Summary data only
    
    // Rejection reason (when Status = "Rejected")
    [Column(TypeName = "nvarchar(max)")]
    public string? RejectionReason { get; set; }
    
    // NEW FIELDS for Editable Sections (ClubManager can edit)
    // Part A: Event media (images, videos)
    [Column(TypeName = "nvarchar(max)")]
    public string? EventMediaUrls { get; set; } // JSON array of media URLs
    
    // Part B: Next month plans - Mục đích và ý nghĩa
    [Column(TypeName = "nvarchar(max)")]
    public string? NextMonthPurposeAndSignificance { get; set; }
    
    // Part VII: Club responsibilities - Trách nhiệm của CLB
    [Column(TypeName = "nvarchar(max)")]
    public string? ClubResponsibilities { get; set; }
}
