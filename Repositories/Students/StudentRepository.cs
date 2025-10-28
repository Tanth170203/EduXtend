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

            var usersWithStudentRecord = await _db.Students
                .Select(s => s.UserId)
                .ToListAsync();

            return await _db.Users
                .Where(u => u.RoleId == studentRoleId && !usersWithStudentRecord.Contains(u.Id))
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
                int userId = student.UserId;

                // Remove related MovementRecords first
                var movementRecords = await _db.MovementRecords.Where(mr => mr.StudentId == id).ToListAsync();
                if (movementRecords.Any())
                {
                    _db.MovementRecords.RemoveRange(movementRecords);
                }

                // Remove related ActivityRegistrations (by UserId)
                var registrations = await _db.ActivityRegistrations.Where(ar => ar.UserId == userId).ToListAsync();
                if (registrations.Any())
                {
                    _db.ActivityRegistrations.RemoveRange(registrations);
                }

                // Remove related ActivityAttendances (by UserId)
                var attendances = await _db.ActivityAttendances.Where(aa => aa.UserId == userId).ToListAsync();
                if (attendances.Any())
                {
                    _db.ActivityAttendances.RemoveRange(attendances);
                }

                // Remove related ClubMembers
                var clubMembers = await _db.ClubMembers.Where(cm => cm.StudentId == id).ToListAsync();
                if (clubMembers.Any())
                {
                    _db.ClubMembers.RemoveRange(clubMembers);
                }

                // Remove related JoinRequests (by UserId)
                var joinRequests = await _db.JoinRequests.Where(jr => jr.UserId == userId).ToListAsync();
                if (joinRequests.Any())
                {
                    _db.JoinRequests.RemoveRange(joinRequests);
                }

                // Remove related Evidences
                var evidences = await _db.Evidences.Where(e => e.StudentId == id).ToListAsync();
                if (evidences.Any())
                {
                    _db.Evidences.RemoveRange(evidences);
                }

                // Finally remove the student
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
