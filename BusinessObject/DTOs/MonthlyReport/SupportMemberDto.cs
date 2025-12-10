namespace BusinessObject.DTOs.MonthlyReport;

public class SupportMemberDto
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
}
