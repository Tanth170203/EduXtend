namespace BusinessObject.DTOs.MonthlyReport;

public class FacilityDto
{
    public DateTime? ElectionTime { get; set; }
    public List<FacilityItemDto> Items { get; set; } = new();
}
