namespace WebFE.Pages.ClubManager.CommunicationPlans;

public class CommunicationItemInput
{
    public int Order { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string? ResponsiblePerson { get; set; }
    public string? Notes { get; set; }
}

public class CommunicationPlanDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<CommunicationItemDto> Items { get; set; } = new();
}

public class CommunicationItemDto
{
    public int Order { get; set; }
    public string Content { get; set; } = null!;
    public DateTime ScheduledDate { get; set; }
    public string? ResponsiblePerson { get; set; }
    public string? Notes { get; set; }
}
