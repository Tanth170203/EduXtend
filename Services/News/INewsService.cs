using BusinessObject.DTOs.News;

namespace Services.News;

public interface INewsService
{
	Task<List<NewsListItemDto>> GetAllAsync(bool publishedOnly);
	Task<NewsDetailDto?> GetByIdAsync(int id, bool includeUnpublishedForAdmin = false);
	Task<NewsDetailDto> CreateAsync(int creatorUserId, CreateNewsRequest request);
	Task<NewsDetailDto> UpdateAsync(int id, UpdateNewsRequest request);
	Task<bool> DeleteAsync(int id);
	Task<NewsDetailDto> PublishAsync(int id, bool publish);
}


