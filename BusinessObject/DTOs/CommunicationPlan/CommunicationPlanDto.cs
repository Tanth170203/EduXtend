namespace BusinessObject.DTOs.CommunicationPlan;

public class CommunicationPlanDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<CommunicationItemDto> Items { get; set; } = new();
}
