using System.Text.Json.Serialization;

namespace BusinessObject.DTOs.MonthlyReport;

public class ClubResponsibilitiesDto
{
    [JsonPropertyName("Planning")]
    public bool Planning { get; set; }
    
    [JsonPropertyName("Implementation")]
    public bool Implementation { get; set; }
    
    [JsonPropertyName("StaffAssignment")]
    public bool StaffAssignment { get; set; }
    
    [JsonPropertyName("SecurityOrder")]
    public bool SecurityOrder { get; set; }
    
    [JsonPropertyName("HygieneAssetMaintenance")]
    public bool HygieneAssetMaintenance { get; set; }
    
    // Custom text entered by ClubManager (overrides checkboxes if present)
    [JsonPropertyName("CustomText")]
    public string? CustomText { get; set; }
}
