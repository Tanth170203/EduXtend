using BusinessObject.Models;

namespace Repositories.Interviews
{
    public interface IInterviewRepository
    {
        Task<Interview?> GetByIdAsync(int id);
        Task<Interview?> GetByJoinRequestIdAsync(int joinRequestId);
        Task<List<Interview>> GetByUserIdAsync(int userId);
        Task<Interview> CreateAsync(Interview interview);
        Task<Interview> UpdateAsync(Interview interview);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsForJoinRequestAsync(int joinRequestId);
    }
}

