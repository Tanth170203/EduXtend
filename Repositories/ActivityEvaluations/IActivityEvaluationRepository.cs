using BusinessObject.Models;

namespace Repositories.ActivityEvaluations
{
    public interface IActivityEvaluationRepository
    {
        Task<ActivityEvaluation?> GetByIdAsync(int id);
        Task<ActivityEvaluation?> GetByActivityIdAsync(int activityId);
        Task<ActivityEvaluation> CreateAsync(ActivityEvaluation evaluation);
        Task<ActivityEvaluation> UpdateAsync(ActivityEvaluation evaluation);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int activityId);
    }
}
