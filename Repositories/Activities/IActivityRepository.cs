using BusinessObject.Models;

namespace Repositories.Activities;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(int id);
    Task<List<Activity>> GetAllAsync();
    Task<List<Activity>> GetPublicAsync();
    Task AddAsync(Activity activity);
    Task UpdateAsync(Activity activity);
    Task DeleteAsync(int id);
}


