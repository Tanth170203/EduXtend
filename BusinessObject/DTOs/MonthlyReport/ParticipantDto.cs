namespace BusinessObject.DTOs.MonthlyReport;

public class ParticipantDto
{
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
}
