using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Activities;

public class ActivityRepository : IActivityRepository
{
    private readonly EduXtendContext _db;

    public ActivityRepository(EduXtendContext db)
    {
        _db = db;
    }

    public async Task<Activity?> GetByIdAsync(int id)
        => await _db.Activities.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Activity>> GetAllAsync()
        => await _db.Activities
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();

    public async Task<List<Activity>> GetPublicAsync()
        => await _db.Activities
            .Where(a => a.IsPublic)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();

    public async Task AddAsync(Activity activity)
    {
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Activity activity)
    {
        _db.Activities.Update(activity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Activities.FindAsync(id);
        if (entity != null)
        {
            _db.Activities.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}


