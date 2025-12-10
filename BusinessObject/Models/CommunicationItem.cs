using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class CommunicationItem
{
    public int Id { get; set; }
    
    public int CommunicationPlanId { get; set; }
    public CommunicationPlan CommunicationPlan { get; set; } = null!;
    
    public int Order { get; set; } // STT
    
    [Required, MaxLength(500)]
    public string Content { get; set; } = null!; // Nội dung truyền thông
    
    public DateTime ScheduledDate { get; set; } // Thời gian
    
    [MaxLength(200)]
    public string? ResponsiblePerson { get; set; } // Phụ trách
    
    [MaxLength(1000)]
    public string? Notes { get; set; } // Ghi chú
}
