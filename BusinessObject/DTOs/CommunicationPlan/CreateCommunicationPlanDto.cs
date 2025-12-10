using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.CommunicationPlan;

public class CreateCommunicationPlanDto
{
    [Required]
    public int ActivityId { get; set; }
    
    public List<CreateCommunicationItemDto> Items { get; set; } = new();
}
