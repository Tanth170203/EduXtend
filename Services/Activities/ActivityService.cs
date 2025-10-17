using BusinessObject.DTOs.Activity;
using BusinessObject.Models;
using Repositories.Activities;

namespace Services.Activities;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _repo;

    public ActivityService(IActivityRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ActivityDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<List<ActivityDto>> GetPublicAsync()
    {
        var list = await _repo.GetPublicAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ActivityDto?> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<ActivityDto> CreateByAdminAsync(int adminUserId, CreateActivityDto dto)
    {
        var entity = new Activity
        {
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Type = dto.Type,
            IsPublic = dto.IsPublic,
            ImageUrl = dto.ImageUrl,
            // Admin created activity
            ClubId = null,
            CreatedById = adminUserId,
            ApprovedById = null,
            RequiresApproval = false, // null semantics not supported for bool; use false for admin-created
            Status = "Approved",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        return ToDto(entity);
    }

    public async Task<ActivityDto> UpdateByAdminAsync(int id, CreateActivityDto dto)
    {
        var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Activity {id} not found");
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.Location = dto.Location;
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.Type = dto.Type;
        entity.IsPublic = dto.IsPublic;
        entity.ImageUrl = dto.ImageUrl;
        await _repo.UpdateAsync(entity);
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
    }

    private static ActivityDto ToDto(Activity a) => new ActivityDto
    {
        Id = a.Id,
        Title = a.Title,
        Description = a.Description,
        Location = a.Location,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Type = a.Type,
        RequiresApproval = a.RequiresApproval,
        IsPublic = a.IsPublic,
        Status = a.Status,
        ImageUrl = a.ImageUrl
    };
}


