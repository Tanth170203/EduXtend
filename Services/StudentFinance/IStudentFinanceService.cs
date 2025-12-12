using BusinessObject.DTOs.Common;
using BusinessObject.DTOs.StudentFinance;

namespace Services.StudentFinance;

public interface IStudentFinanceService
{
    Task<PaginatedResult<StudentPendingPaymentDto>> GetPendingPaymentsAsync(int userId, int page, int pageSize, int? clubId);
    Task<PaginatedResult<StudentPaymentHistoryDto>> GetPaymentHistoryAsync(int userId, int page, int pageSize, int? clubId, DateTime? startDate, DateTime? endDate);
    Task<StudentFinanceStatisticsDto> GetFinanceStatisticsAsync(int userId);
    Task<byte[]> ExportPaymentHistoryToCsvAsync(int userId, int? clubId, DateTime? startDate, DateTime? endDate);
}
