using BusinessObject.DTOs.MovementCriteria;
using BusinessObject.Models;
using Repositories.MovementCriteria;

namespace Services.MovementCriteria;

public class MovementCriterionGroupService : IMovementCriterionGroupService
{
    private readonly IMovementCriterionGroupRepository _groupRepository;
    private readonly IMovementCriterionRepository _criterionRepository;

    public MovementCriterionGroupService(
        IMovementCriterionGroupRepository groupRepository,
        IMovementCriterionRepository criterionRepository)
    {
        _groupRepository = groupRepository;
        _criterionRepository = criterionRepository;
    }

    public async Task<IEnumerable<MovementCriterionGroupDto>> GetAllAsync()
    {
        var groups = await _groupRepository.GetAllAsync();
        return groups.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementCriterionGroupDto>> GetByTargetTypeAsync(string targetType)
    {
        if (targetType != "Student" && targetType != "Club")
            throw new ArgumentException("TargetType phải là 'Student' hoặc 'Club'");

        var groups = await _groupRepository.GetByTargetTypeAsync(targetType);
        return groups.Select(MapToDto);
    }

    public async Task<MovementCriterionGroupDto?> GetByIdAsync(int id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        return group != null ? MapToDto(group) : null;
    }

    public async Task<MovementCriterionGroupDetailDto?> GetByIdWithCriteriaAsync(int id)
    {
        var group = await _groupRepository.GetByIdWithCriteriaAsync(id);
        return group != null ? MapToDetailDto(group) : null;
    }

    public async Task<MovementCriterionGroupDto> CreateAsync(CreateMovementCriterionGroupDto dto)
    {
        // Validate business rules
        if (dto.TargetType != "Student" && dto.TargetType != "Club")
            throw new ArgumentException("TargetType phải là 'Student' hoặc 'Club'");

        if (dto.MaxScore < 0)
            throw new ArgumentException("Điểm tối đa không thể âm");

        var group = new MovementCriterionGroup
        {
            Name = dto.Name,
            Description = dto.Description,
            MaxScore = dto.MaxScore,
            TargetType = dto.TargetType
        };

        var created = await _groupRepository.CreateAsync(group);
        return MapToDto(created);
    }

    public async Task<MovementCriterionGroupDto> UpdateAsync(int id, UpdateMovementCriterionGroupDto dto)
    {
        // Kiểm tra tồn tại
        var existing = await _groupRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Không tìm thấy nhóm tiêu chí với ID {id}");

        // Validate business rules
        if (dto.TargetType != "Student" && dto.TargetType != "Club")
            throw new ArgumentException("TargetType phải là 'Student' hoặc 'Club'");

        if (dto.MaxScore < 0)
            throw new ArgumentException("Điểm tối đa không thể âm");

        // Cập nhật thông tin
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.MaxScore = dto.MaxScore;
        existing.TargetType = dto.TargetType;

        var updated = await _groupRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Kiểm tra tồn tại
        var exists = await _groupRepository.ExistsAsync(id);
        if (!exists)
            throw new KeyNotFoundException($"Không tìm thấy nhóm tiêu chí với ID {id}");

        // Kiểm tra có tiêu chí con hay không
        var hasCriteria = await _groupRepository.HasCriteriaAsync(id);
        if (hasCriteria)
            throw new InvalidOperationException("Không thể xóa nhóm tiêu chí đã có tiêu chí con. Vui lòng xóa các tiêu chí con trước.");

        return await _groupRepository.DeleteAsync(id);
    }

    // Helper methods for mapping
    private static MovementCriterionGroupDto MapToDto(MovementCriterionGroup group)
    {
        return new MovementCriterionGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            MaxScore = group.MaxScore,
            TargetType = group.TargetType,
            CriteriaCount = group.Criteria?.Count ?? 0
        };
    }

    private static MovementCriterionGroupDetailDto MapToDetailDto(MovementCriterionGroup group)
    {
        return new MovementCriterionGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            MaxScore = group.MaxScore,
            TargetType = group.TargetType,
            Criteria = group.Criteria?.Select(c => new MovementCriterionDto
            {
                Id = c.Id,
                GroupId = c.GroupId,
                GroupName = group.Name,
                Title = c.Title,
                Description = c.Description,
                MaxScore = c.MaxScore,
                TargetType = c.TargetType,
                DataSource = c.DataSource,
                IsActive = c.IsActive
            }).ToList() ?? new List<MovementCriterionDto>()
        };
    }
}



