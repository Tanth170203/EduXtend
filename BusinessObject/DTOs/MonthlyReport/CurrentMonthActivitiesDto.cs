namespace BusinessObject.DTOs.MonthlyReport;

public class CurrentMonthActivitiesDto
{
    public List<SchoolEventDto> SchoolEvents { get; set; } = new();
    public List<SupportActivityDto> SupportActivities { get; set; } = new();
    public List<CompetitionDto> Competitions { get; set; } = new();
    public List<InternalMeetingDto> InternalMeetings { get; set; } = new();
}
