using BusinessObject.Models;

namespace Repositories.News;

public interface INewsRepository
{
	Task<SystemNews?> GetByIdAsync(int id);
	Task<List<SystemNews>> GetAllAsync(bool? publishedOnly = null);
	Task<SystemNews> CreateAsync(SystemNews news);
	Task<SystemNews> UpdateAsync(SystemNews news);
	Task<bool> DeleteAsync(int id);
}


