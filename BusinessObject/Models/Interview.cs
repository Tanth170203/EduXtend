using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Models
{
    public class Interview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JoinRequestId { get; set; }

        [ForeignKey("JoinRequestId")]
        public virtual JoinRequest JoinRequest { get; set; } = null!;

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Notes { get; set; } // Ghi chú ban đầu khi tạo lịch

        [MaxLength(2000)]
        public string? Evaluation { get; set; } // Đánh giá sau interview

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int CreatedById { get; set; } // Manager who created

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; } = null!;
    }
}

