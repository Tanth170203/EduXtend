using BusinessObject.Models;

namespace Repositories.CommunicationPlans
{
    public interface ICommunicationPlanRepository
    {
        Task<CommunicationPlan?> GetByIdAsync(int id);
        Task<CommunicationPlan?> GetByActivityIdAsync(int activityId);
        Task<List<CommunicationPlan>> GetByClubIdAsync(int clubId);
        Task<List<CommunicationPlan>> GetByClubAndMonthAsync(int clubId, int month, int year);
        Task<CommunicationPlan> CreateAsync(CommunicationPlan plan);
        Task<CommunicationPlan> UpdateAsync(CommunicationPlan plan);
        Task<bool> DeleteAsync(int id);
        Task<List<int>> GetActivityIdsWithPlansAsync(int clubId);
    }
}
