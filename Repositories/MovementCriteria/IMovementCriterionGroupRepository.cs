using BusinessObject.Models;

namespace Repositories.MovementCriteria;

public interface IMovementCriterionGroupRepository
{
    Task<IEnumerable<MovementCriterionGroup>> GetAllAsync();
    Task<IEnumerable<MovementCriterionGroup>> GetByTargetTypeAsync(string targetType);
    Task<MovementCriterionGroup?> GetByIdAsync(int id);
    Task<MovementCriterionGroup?> GetByIdWithCriteriaAsync(int id);
    Task<MovementCriterionGroup> CreateAsync(MovementCriterionGroup group);
    Task<MovementCriterionGroup> UpdateAsync(MovementCriterionGroup group);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> HasCriteriaAsync(int groupId);
}


