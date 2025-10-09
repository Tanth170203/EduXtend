using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Students
{
    public class StudentRepository : IStudentRepository
    {
        private readonly EduXtendContext _db;

        public StudentRepository(EduXtendContext db)
        {
            _db = db;
        }

        public async Task<List<Student>> GetAllAsync()
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

        public async Task<Student?> GetByIdAsync(int id)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Student?> GetByUserIdAsync(int userId)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.UserId == userId);

        public async Task<Student?> GetByStudentCodeAsync(string studentCode)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.StudentCode == studentCode);

        public async Task AddAsync(Student student)
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _db.Students.Update(student);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var student = await _db.Students.FindAsync(id);
            if (student != null)
            {
                _db.Students.Remove(student);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
            => await _db.Students.AnyAsync(s => s.Id == id);

        public async Task<bool> StudentCodeExistsAsync(string studentCode)
            => await _db.Students.AnyAsync(s => s.StudentCode == studentCode);

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}

