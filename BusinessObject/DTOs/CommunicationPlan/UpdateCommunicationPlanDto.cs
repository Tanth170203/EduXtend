namespace BusinessObject.DTOs.CommunicationPlan;

public class UpdateCommunicationPlanDto
{
    public List<CreateCommunicationItemDto> Items { get; set; } = new();
}
