using BusinessObject.DTOs.MonthlyReport;

namespace Services.MonthlyReports;

public interface IMonthlyReportDataAggregator
{
    Task<List<SchoolEventDto>> GetSchoolEventsAsync(int clubId, int month, int year);
    Task<List<SupportActivityDto>> GetSupportActivitiesAsync(int clubId, int month, int year);
    Task<List<CompetitionDto>> GetCompetitionsAsync(int clubId, int month, int year);
    Task<List<InternalMeetingDto>> GetInternalMeetingsAsync(int clubId, int month, int year);
    Task<NextMonthPlansDto> GetNextMonthPlansAsync(int clubId, int reportMonth, int reportYear, int nextMonth, int nextYear);
}
