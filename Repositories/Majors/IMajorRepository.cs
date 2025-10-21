using BusinessObject.Models;

namespace Repositories.Majors
{
    public interface IMajorRepository
    {
        Task<Major?> GetByIdAsync(int id);
        Task<Major?> GetByCodeAsync(string code);
        Task<List<Major>> GetAllAsync();
        Task<Dictionary<string, int>> GetMajorIdsByCodesAsync(List<string> codes);
    }
}
