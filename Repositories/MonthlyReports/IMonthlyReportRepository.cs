using BusinessObject.Models;

namespace Repositories.MonthlyReports
{
    public interface IMonthlyReportRepository
    {
        Task<List<Plan>> GetAllByClubIdAsync(int clubId);
        Task<Plan?> GetByIdAsync(int id);
        Task<Plan> CreateAsync(Plan plan);
        Task<Plan> UpdateAsync(Plan plan);
        Task<Plan?> GetByClubAndMonthAsync(int clubId, int month, int year);
    }
}
