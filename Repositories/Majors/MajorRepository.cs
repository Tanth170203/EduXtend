using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Majors
{
    public class MajorRepository : IMajorRepository
    {
        private readonly EduXtendContext _db;

        public MajorRepository(EduXtendContext db)
        {
            _db = db;
        }

        public async Task<Major?> GetByIdAsync(int id)
            => await _db.Majors
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<Major?> GetByCodeAsync(string code)
            => await _db.Majors
                .FirstOrDefaultAsync(m => m.Code == code);

        public async Task<List<Major>> GetAllAsync()
            => await _db.Majors
                .Where(m => m.IsActive)
                .OrderBy(m => m.Code)
                .ToListAsync();

        public async Task<Dictionary<string, int>> GetMajorIdsByCodesAsync(List<string> codes)
        {
            var majors = await _db.Majors
                .Where(m => codes.Contains(m.Code))
                .ToDictionaryAsync(m => m.Code, m => m.Id);
            return majors;
        }
    }
}
