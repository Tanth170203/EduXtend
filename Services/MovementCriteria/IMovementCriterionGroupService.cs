using BusinessObject.DTOs.MovementCriteria;

namespace Services.MovementCriteria;

public interface IMovementCriterionGroupService
{
    Task<IEnumerable<MovementCriterionGroupDto>> GetAllAsync();
    Task<IEnumerable<MovementCriterionGroupDto>> GetByTargetTypeAsync(string targetType);
    Task<MovementCriterionGroupDto?> GetByIdAsync(int id);
    Task<MovementCriterionGroupDetailDto?> GetByIdWithCriteriaAsync(int id);
    Task<MovementCriterionGroupDto> CreateAsync(CreateMovementCriterionGroupDto dto);
    Task<MovementCriterionGroupDto> UpdateAsync(int id, UpdateMovementCriterionGroupDto dto);
    Task<bool> DeleteAsync(int id);
}



