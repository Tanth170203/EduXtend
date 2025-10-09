using BusinessObject.Models;

namespace Repositories.Majors
{
    public interface IMajorRepository
    {
        Task<List<Major>> GetAllAsync();
        Task<List<Major>> GetActiveAsync();
        Task<Major?> GetByIdAsync(int id);
        Task<Major?> GetByCodeAsync(string code);
        Task AddAsync(Major major);
        Task UpdateAsync(Major major);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}

