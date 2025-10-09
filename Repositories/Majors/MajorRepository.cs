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

        public async Task<List<Major>> GetAllAsync()
            => await _db.Majors.OrderBy(m => m.Name).ToListAsync();

        public async Task<List<Major>> GetActiveAsync()
            => await _db.Majors
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

        public async Task<Major?> GetByIdAsync(int id)
            => await _db.Majors.FirstOrDefaultAsync(m => m.Id == id);

        public async Task<Major?> GetByCodeAsync(string code)
            => await _db.Majors.FirstOrDefaultAsync(m => m.Code == code);

        public async Task AddAsync(Major major)
        {
            _db.Majors.Add(major);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Major major)
        {
            _db.Majors.Update(major);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var major = await _db.Majors.FindAsync(id);
            if (major != null)
            {
                _db.Majors.Remove(major);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
            => await _db.Majors.AnyAsync(m => m.Id == id);

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}

