using BusinessObject.DTOs.News;
using BusinessObject.Models;
using Repositories.News;

namespace Services.News;

public class NewsService : INewsService
{
	private readonly INewsRepository _repo;

	public NewsService(INewsRepository repo)
	{
		_repo = repo;
	}

	public async Task<List<NewsListItemDto>> GetAllAsync(bool publishedOnly)
	{
		var list = await _repo.GetAllAsync(publishedOnly);
		return list.Select(n => new NewsListItemDto
		{
			Id = n.Id,
			Title = n.Title,
			ImageUrl = n.ImageUrl,
			IsPublished = n.IsActive,
			PublishedAt = n.IsActive ? n.PublishedAt : null
		}).ToList();
	}

	public async Task<NewsDetailDto?> GetByIdAsync(int id, bool includeUnpublishedForAdmin = false)
	{
		var n = await _repo.GetByIdAsync(id);
		if (n == null) return null;
		if (!n.IsActive && !includeUnpublishedForAdmin) return null;
		return Map(n);
	}

	public async Task<NewsDetailDto> CreateAsync(int creatorUserId, CreateNewsRequest request)
	{
		var now = DateTime.UtcNow;
		var entity = new SystemNews
		{
			Title = request.Title.Trim(),
			Content = request.Content?.Trim(),
			ImageUrl = request.ImageUrl?.Trim(),
			FacebookUrl = request.FacebookUrl?.Trim(),
			IsActive = request.Publish,
			PublishedAt = request.Publish ? now : now, // keep timestamp; UI will use only if IsActive
			CreatedById = creatorUserId
		};
		await _repo.CreateAsync(entity);
		return Map(entity);
	}

	public async Task<NewsDetailDto> UpdateAsync(int id, UpdateNewsRequest request)
	{
		var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("News not found");
		entity.Title = request.Title.Trim();
		entity.Content = request.Content?.Trim();
		entity.ImageUrl = request.ImageUrl?.Trim();
		entity.FacebookUrl = request.FacebookUrl?.Trim();
		if (request.Publish.HasValue)
		{
			entity.IsActive = request.Publish.Value;
			if (request.Publish.Value)
			{
				entity.PublishedAt = DateTime.UtcNow;
			}
		}
		await _repo.UpdateAsync(entity);
		return Map(entity);
	}

	public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

	public async Task<NewsDetailDto> PublishAsync(int id, bool publish)
	{
		var entity = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("News not found");
		entity.IsActive = publish;
		if (publish) entity.PublishedAt = DateTime.UtcNow;
		await _repo.UpdateAsync(entity);
		return Map(entity);
	}

	private static NewsDetailDto Map(SystemNews n) => new NewsDetailDto
	{
		Id = n.Id,
		Title = n.Title,
		Content = n.Content,
		ImageUrl = n.ImageUrl,
		FacebookUrl = n.FacebookUrl,
		IsPublished = n.IsActive,
		PublishedAt = n.IsActive ? n.PublishedAt : null,
		CreatedById = n.CreatedById,
		CreatedByName = n.CreatedBy?.FullName
	};
}


