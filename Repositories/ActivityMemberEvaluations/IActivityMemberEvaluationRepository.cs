using BusinessObject.Models;

namespace Repositories.ActivityMemberEvaluations
{
    public interface IActivityMemberEvaluationRepository
    {
        Task<ActivityMemberEvaluation?> GetByIdAsync(int id);
        Task<ActivityMemberEvaluation?> GetByAssignmentIdAsync(int assignmentId);
        Task<List<ActivityMemberEvaluation>> GetByActivityIdAsync(int activityId);
        Task<List<ActivityMemberEvaluation>> GetByUserIdAsync(int userId);
        Task<ActivityMemberEvaluation> CreateAsync(ActivityMemberEvaluation evaluation);
        Task<ActivityMemberEvaluation> UpdateAsync(ActivityMemberEvaluation evaluation);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int assignmentId);
    }
}
