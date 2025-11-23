namespace Services.MonthlyReports;

public interface IMonthlyReportPdfService
{
    Task<byte[]> ExportToPdfAsync(int reportId);
}
