using BusinessObject.DTOs.MovementCriteria;
using BusinessObject.Models;
using Repositories.MovementCriteria;

namespace Services.MovementCriteria;

public class MovementCriterionService : IMovementCriterionService
{
    private readonly IMovementCriterionRepository _criterionRepository;
    private readonly IMovementCriterionGroupRepository _groupRepository;

    public MovementCriterionService(
        IMovementCriterionRepository criterionRepository,
        IMovementCriterionGroupRepository groupRepository)
    {
        _criterionRepository = criterionRepository;
        _groupRepository = groupRepository;
    }

    public async Task<IEnumerable<MovementCriterionDto>> GetAllAsync()
    {
        var criteria = await _criterionRepository.GetAllAsync();
        return criteria.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementCriterionDto>> GetByGroupIdAsync(int groupId)
    {
        var criteria = await _criterionRepository.GetByGroupIdAsync(groupId);
        return criteria.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementCriterionDto>> GetByTargetTypeAsync(string targetType)
    {
        if (targetType != "Student" && targetType != "Club")
            throw new ArgumentException("TargetType must be 'Student' or 'Club'");

        var criteria = await _criterionRepository.GetByTargetTypeAsync(targetType);
        return criteria.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementCriterionDto>> GetActiveAsync()
    {
        var criteria = await _criterionRepository.GetActiveAsync();
        return criteria.Select(MapToDto);
    }

    public async Task<MovementCriterionDto?> GetByIdAsync(int id)
    {
        var criterion = await _criterionRepository.GetByIdWithGroupAsync(id);
        return criterion != null ? MapToDto(criterion) : null;
    }

    public async Task<MovementCriterionDto> CreateAsync(CreateMovementCriterionDto dto)
    {
        // Validate business rules
        if (dto.TargetType != "Student" && dto.TargetType != "Club")
            throw new ArgumentException("TargetType must be 'Student' or 'Club'");

        if (dto.MaxScore < 0)
            throw new ArgumentException("Maximum score cannot be negative");

        // Kiểm tra GroupId có tồn tại không
        var groupExists = await _groupRepository.ExistsAsync(dto.GroupId);
        if (!groupExists)
            throw new KeyNotFoundException($"Criteria group with ID {dto.GroupId} not found");

        var criterion = new MovementCriterion
        {
            GroupId = dto.GroupId,
            Title = dto.Title,
            Description = dto.Description,
            MaxScore = dto.MaxScore,
            TargetType = dto.TargetType,
            DataSource = dto.DataSource,
            IsActive = dto.IsActive
        };

        var created = await _criterionRepository.CreateAsync(criterion);
        
        // Load lại với Group để có thông tin đầy đủ
        var result = await _criterionRepository.GetByIdWithGroupAsync(created.Id);
        return MapToDto(result!);
    }

    public async Task<MovementCriterionDto> UpdateAsync(int id, UpdateMovementCriterionDto dto)
    {
        // Kiểm tra tồn tại
        var existing = await _criterionRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Criteria with ID {id} not found");

        // Validate business rules
        if (dto.TargetType != "Student" && dto.TargetType != "Club")
            throw new ArgumentException("TargetType must be 'Student' or 'Club'");

        if (dto.MaxScore < 0)
            throw new ArgumentException("Maximum score cannot be negative");

        // Kiểm tra GroupId có tồn tại không (nếu thay đổi)
        if (existing.GroupId != dto.GroupId)
        {
            var groupExists = await _groupRepository.ExistsAsync(dto.GroupId);
            if (!groupExists)
                throw new KeyNotFoundException($"Criteria group with ID {dto.GroupId} not found");
        }

        // Cập nhật thông tin
        existing.GroupId = dto.GroupId;
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.MaxScore = dto.MaxScore;
        existing.TargetType = dto.TargetType;
        existing.DataSource = dto.DataSource;
        existing.IsActive = dto.IsActive;

        var updated = await _criterionRepository.UpdateAsync(existing);
        
        // Load lại với Group để có thông tin đầy đủ
        var result = await _criterionRepository.GetByIdWithGroupAsync(updated.Id);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Kiểm tra tồn tại
        var exists = await _criterionRepository.ExistsAsync(id);
        if (!exists)
            throw new KeyNotFoundException($"Criteria with ID {id} not found");

        // Kiểm tra có dữ liệu liên quan hay không
        var hasRelatedData = await _criterionRepository.HasRelatedDataAsync(id);
        if (hasRelatedData)
            throw new InvalidOperationException("Cannot delete criteria that has related data (MovementRecordDetail or Evidence). You can deactivate the criteria instead of deleting.");

        return await _criterionRepository.DeleteAsync(id);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var criterion = await _criterionRepository.GetByIdAsync(id);
        if (criterion == null)
            throw new KeyNotFoundException($"Criteria with ID {id} not found");

        criterion.IsActive = !criterion.IsActive;
        await _criterionRepository.UpdateAsync(criterion);
        
        return criterion.IsActive;
    }

    // Helper method for mapping
    private static MovementCriterionDto MapToDto(MovementCriterion criterion)
    {
        return new MovementCriterionDto
        {
            Id = criterion.Id,
            GroupId = criterion.GroupId,
            GroupName = criterion.Group?.Name,
            Title = criterion.Title,
            Description = criterion.Description,
            MaxScore = criterion.MaxScore,
            MinScore = criterion.MinScore,
            TargetType = criterion.TargetType,
            DataSource = criterion.DataSource,
            IsActive = criterion.IsActive
        };
    }
}



