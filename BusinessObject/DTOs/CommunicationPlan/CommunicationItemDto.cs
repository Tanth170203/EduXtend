namespace BusinessObject.DTOs.CommunicationPlan;

public class CommunicationItemDto
{
    public int Order { get; set; }
    public string Content { get; set; } = null!;
    public DateTime ScheduledDate { get; set; }
    public string? ResponsiblePerson { get; set; }
    public string? Notes { get; set; }
}
