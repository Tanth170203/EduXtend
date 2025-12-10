using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.CommunicationPlan;

public class CreateCommunicationItemDto
{
    [Required, MaxLength(500)]
    public string Content { get; set; } = null!;
    
    [Required]
    public DateTime ScheduledDate { get; set; }
    
    [MaxLength(200)]
    public string? ResponsiblePerson { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
