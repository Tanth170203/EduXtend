using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ClubNews;

public class ClubNewsRepository : IClubNewsRepository
{
	private readonly EduXtendContext _db;

	public ClubNewsRepository(EduXtendContext db)
	{
		_db = db;
	}

	public async Task<BusinessObject.Models.ClubNews?> GetByIdAsync(int id)
		=> await _db.ClubNews
			.Include(n => n.CreatedBy)
			.Include(n => n.Club)
			.FirstOrDefaultAsync(n => n.Id == id);

	public async Task<List<BusinessObject.Models.ClubNews>> GetAllAsync(int? clubId = null, bool? approvedOnly = null)
	{
		var query = _db.ClubNews
			.Include(n => n.CreatedBy)
			.Include(n => n.Club)
			.AsQueryable();

		if (clubId.HasValue)
		{
			query = query.Where(n => n.ClubId == clubId.Value);
		}

		if (approvedOnly == true)
		{
			query = query.Where(n => n.IsApproved);
		}

		return await query
			.OrderByDescending(n => n.PublishedAt)
			.ThenByDescending(n => n.Id)
			.ToListAsync();
	}

	public async Task<List<BusinessObject.Models.ClubNews>> GetPendingApprovalAsync()
	{
		return await _db.ClubNews
			.Include(n => n.CreatedBy)
			.Include(n => n.Club)
			.Where(n => !n.IsApproved)
			.OrderBy(n => n.PublishedAt)
			.ToListAsync();
	}

	public async Task<BusinessObject.Models.ClubNews> CreateAsync(BusinessObject.Models.ClubNews news)
	{
		_db.ClubNews.Add(news);
		await _db.SaveChangesAsync();
		return news;
	}

	public async Task<BusinessObject.Models.ClubNews> UpdateAsync(BusinessObject.Models.ClubNews news)
	{
		_db.ClubNews.Update(news);
		await _db.SaveChangesAsync();
		return news;
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var entity = await _db.ClubNews.FindAsync(id);
		if (entity == null) return false;
		_db.ClubNews.Remove(entity);
		await _db.SaveChangesAsync();
		return true;
	}
}
