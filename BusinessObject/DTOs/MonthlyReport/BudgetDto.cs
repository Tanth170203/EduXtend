namespace BusinessObject.DTOs.MonthlyReport;

public class BudgetDto
{
    public List<BudgetItemDto> SchoolFunding { get; set; } = new();
    public decimal SchoolTotal { get; set; }
    public string SchoolTotalInWords { get; set; } = string.Empty;
    
    public List<BudgetItemDto> ClubFunding { get; set; } = new();
    public decimal ClubTotal { get; set; }
    public string ClubTotalInWords { get; set; } = string.Empty;
}
