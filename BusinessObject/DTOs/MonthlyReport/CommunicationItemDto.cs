namespace BusinessObject.DTOs.MonthlyReport;

public class CommunicationItemDto
{
    public string Content { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string ResponsiblePerson { get; set; } = string.Empty;
    public bool NeedSupport { get; set; }
}
