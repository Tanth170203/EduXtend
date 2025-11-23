namespace BusinessObject.DTOs.MonthlyReport;

public class SupportActivityDto
{
    public int ActivityId { get; set; }
    public string EventContent { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    
    public List<SupportStudentDto> SupportStudents { get; set; } = new();
}
