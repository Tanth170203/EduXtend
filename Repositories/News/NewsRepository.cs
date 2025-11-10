using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.News;

public class NewsRepository : INewsRepository
{
	private readonly EduXtendContext _db;

	public NewsRepository(EduXtendContext db)
	{
		_db = db;
	}

	public async Task<SystemNews?> GetByIdAsync(int id)
		=> await _db.SystemNews.Include(n => n.CreatedBy).FirstOrDefaultAsync(n => n.Id == id);

	public async Task<List<SystemNews>> GetAllAsync(bool? publishedOnly = null)
	{
		var query = _db.SystemNews.Include(n => n.CreatedBy).AsQueryable();
		if (publishedOnly == true)
		{
			query = query.Where(n => n.IsActive);
		}
		return await query.OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id).ToListAsync();
	}

	public async Task<SystemNews> CreateAsync(SystemNews news)
	{
		_db.SystemNews.Add(news);
		await _db.SaveChangesAsync();
		return news;
	}

	public async Task<SystemNews> UpdateAsync(SystemNews news)
	{
		_db.SystemNews.Update(news);
		await _db.SaveChangesAsync();
		return news;
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var entity = await _db.SystemNews.FindAsync(id);
		if (entity == null) return false;
		_db.SystemNews.Remove(entity);
		await _db.SaveChangesAsync();
		return true;
	}
}


