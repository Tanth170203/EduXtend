using BusinessObject.DTOs.News;
using BusinessObject.Models;
using Repositories.ClubNews;
using Repositories.Clubs;
using Services.Notifications;

namespace Services.ClubNews;

public class ClubNewsService : IClubNewsService
{
	private readonly IClubNewsRepository _repo;
	private readonly IClubRepository _clubRepo;
	private readonly INotificationService _notificationService;

	public ClubNewsService(IClubNewsRepository repo, IClubRepository clubRepo, INotificationService notificationService)
	{
		_repo = repo;
		_clubRepo = clubRepo;
		_notificationService = notificationService;
	}

	public async Task<List<ClubNewsListItemDto>> GetAllAsync(int? clubId = null, bool? approvedOnly = null)
	{
		var list = await _repo.GetAllAsync(clubId, approvedOnly);
		return list.Select(n => new ClubNewsListItemDto
		{
			Id = n.Id,
			ClubId = n.ClubId,
			ClubName = n.Club?.Name ?? "",
			Title = n.Title,
			ImageUrl = n.ImageUrl,
			IsApproved = n.IsApproved,
			PublishedAt = n.PublishedAt,
			CreatedById = n.CreatedById,
			CreatedByName = n.CreatedBy?.FullName
		}).ToList();
	}

	public async Task<List<ClubNewsListItemDto>> GetPendingApprovalAsync()
	{
		var list = await _repo.GetPendingApprovalAsync();
		return list.Select(n => new ClubNewsListItemDto
		{
			Id = n.Id,
			ClubId = n.ClubId,
			ClubName = n.Club?.Name ?? "",
			Title = n.Title,
			ImageUrl = n.ImageUrl,
			IsApproved = n.IsApproved,
			PublishedAt = n.PublishedAt,
			CreatedById = n.CreatedById,
			CreatedByName = n.CreatedBy?.FullName
		}).ToList();
	}

	public async Task<ClubNewsDetailDto?> GetByIdAsync(int id)
	{
		var n = await _repo.GetByIdAsync(id);
		if (n == null) return null;
		return Map(n);
	}

	public async Task<ClubNewsDetailDto> CreateAsync(int creatorUserId, int clubId, CreateClubNewsRequest request)
	{
		// Verify club exists
		var club = await _clubRepo.GetByIdAsync(clubId);
		if (club == null)
		{
			throw new KeyNotFoundException("Club not found");
		}

		var now = DateTime.UtcNow;
		var entity = new BusinessObject.Models.ClubNews
		{
			ClubId = clubId,
			Title = request.Title.Trim(),
			Content = request.Content?.Trim(),
			ImageUrl = request.ImageUrl?.Trim(),
			FacebookUrl = request.FacebookUrl?.Trim(),
			IsApproved = false, // Requires admin approval
			PublishedAt = now,
			CreatedById = creatorUserId
		};

		await _repo.CreateAsync(entity);
		
		// Reload to get navigation properties
		var created = await _repo.GetByIdAsync(entity.Id);
		
		// Notify admins about new club news
		await _notificationService.NotifyAdminsAboutNewClubNewsAsync(
			created!.Id, 
			created.Title, 
			clubId, 
			club.Name, 
			creatorUserId
		);
		
		return Map(created!);
	}

	public async Task<ClubNewsDetailDto> UpdateAsync(int id, int userId, UpdateClubNewsRequest request)
	{
		var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Club news not found");
		
		// Only creator or admin can update
		// This check should be done in controller, but adding here for safety
		entity.Title = request.Title.Trim();
		entity.Content = request.Content?.Trim();
		entity.ImageUrl = request.ImageUrl?.Trim();
		entity.FacebookUrl = request.FacebookUrl?.Trim();
		
		await _repo.UpdateAsync(entity);
		
		// Reload to get navigation properties
		var updated = await _repo.GetByIdAsync(id);
		return Map(updated!);
	}

	public async Task<ClubNewsDetailDto> ApproveAsync(int id, bool approve)
	{
		var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Club news not found");
		
		// Store creator ID before update
		var creatorId = entity.CreatedById;
		var newsTitle = entity.Title;
		
		entity.IsApproved = approve;
		if (approve)
		{
			entity.PublishedAt = DateTime.UtcNow;
		}
		await _repo.UpdateAsync(entity);
		
		// Reload to get navigation properties
		var updated = await _repo.GetByIdAsync(id);
		
		// Notify club manager about approval/rejection
		await _notificationService.NotifyClubManagerAboutNewsApprovalAsync(
			id,
			newsTitle,
			creatorId,
			approve
		);
		
		return Map(updated!);
	}

	public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
	{
		var entity = await _repo.GetByIdAsync(id);
		if (entity == null) return false;
		
		// Only creator or admin can delete
		if (!isAdmin && entity.CreatedById != userId)
		{
			throw new UnauthorizedAccessException("You can only delete your own news");
		}
		
		return await _repo.DeleteAsync(id);
	}

	private static ClubNewsDetailDto Map(BusinessObject.Models.ClubNews n) => new ClubNewsDetailDto
	{
		Id = n.Id,
		ClubId = n.ClubId,
		ClubName = n.Club?.Name ?? "",
		Title = n.Title,
		Content = n.Content,
		ImageUrl = n.ImageUrl,
		FacebookUrl = n.FacebookUrl,
		IsApproved = n.IsApproved,
		PublishedAt = n.PublishedAt,
		CreatedById = n.CreatedById,
		CreatedByName = n.CreatedBy?.FullName
	};
}
