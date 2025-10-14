using BusinessObject.DTOs.MovementCriteria;

namespace Services.MovementCriteria;

public interface IMovementCriterionService
{
    Task<IEnumerable<MovementCriterionDto>> GetAllAsync();
    Task<IEnumerable<MovementCriterionDto>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<MovementCriterionDto>> GetByTargetTypeAsync(string targetType);
    Task<IEnumerable<MovementCriterionDto>> GetActiveAsync();
    Task<MovementCriterionDto?> GetByIdAsync(int id);
    Task<MovementCriterionDto> CreateAsync(CreateMovementCriterionDto dto);
    Task<MovementCriterionDto> UpdateAsync(int id, UpdateMovementCriterionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
}



