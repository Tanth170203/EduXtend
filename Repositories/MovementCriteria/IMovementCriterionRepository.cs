using BusinessObject.Models;

namespace Repositories.MovementCriteria;

public interface IMovementCriterionRepository
{
    Task<IEnumerable<MovementCriterion>> GetAllAsync();
    Task<IEnumerable<MovementCriterion>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<MovementCriterion>> GetByTargetTypeAsync(string targetType);
    Task<IEnumerable<MovementCriterion>> GetActiveAsync();
    Task<MovementCriterion?> GetByIdAsync(int id);
    Task<MovementCriterion?> GetByIdWithGroupAsync(int id);
    Task<MovementCriterion> CreateAsync(MovementCriterion criterion);
    Task<MovementCriterion> UpdateAsync(MovementCriterion criterion);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> HasRelatedDataAsync(int criterionId);
}



