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

        public async Task<Student?> GetByIdAsync(int id)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Student?> GetByStudentCodeAsync(string studentCode)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.StudentCode == studentCode);

        public async Task<Student?> GetByUserIdAsync(int userId)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.UserId == userId);

        public async Task<List<Student>> GetAllAsync()
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .OrderByDescending(s => s.EnrollmentDate)
                .ToListAsync();

        public async Task<List<Student>> GetByStudentCodesAsync(List<string> studentCodes)
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .Where(s => studentCodes.Contains(s.StudentCode))
                .ToListAsync();

        public async Task<bool> ExistsAsync(int id)
            => await _db.Students.AnyAsync(s => s.Id == id);

        public async Task<List<User>> GetUsersWithoutStudentInfoAsync()
        {
            // Get users with Student role but no Student record
            var studentRoleId = await _db.Roles
                .Where(r => r.RoleName == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (studentRoleId == 0)
                return new List<User>();

            var usersWithStudentRole = await _db.UserRoles
                .Where(ur => ur.RoleId == studentRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var usersWithStudentRecord = await _db.Students
                .Select(s => s.UserId)
                .ToListAsync();

            var usersWithoutStudentInfo = usersWithStudentRole
                .Except(usersWithStudentRecord)
                .ToList();

            return await _db.Users
                .Where(u => usersWithoutStudentInfo.Contains(u.Id))
                .ToListAsync();
        }

        public async Task AddAsync(Student student)
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<Student> students)
        {
            await _db.Students.AddRangeAsync(students);
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

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

        public async Task<List<Student>> GetAllActiveStudentsAsync()
            => await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .Where(s => s.Status.ToString() == "Active")
                .OrderBy(s => s.FullName)
                .ToListAsync();

        public async Task<List<Student>> SearchStudentsAsync(string query)
        {
            var lowerQuery = query.ToLower();
            return await _db.Students
                .Include(s => s.User)
                .Include(s => s.Major)
                .Where(s => 
                    s.FullName.ToLower().Contains(lowerQuery) ||
                    s.StudentCode.ToLower().Contains(lowerQuery) ||
                    s.User.Email.ToLower().Contains(lowerQuery))
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }
    }
}
