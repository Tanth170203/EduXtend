using BusinessObject.Models;
using Repositories.Students;
using Repositories.Users;
using Repositories.Majors;

namespace Services.Students
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMajorRepository _majorRepo;

        public StudentService(
            IStudentRepository studentRepo,
            IUserRepository userRepo,
            IMajorRepository majorRepo)
        {
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _majorRepo = majorRepo;
        }

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            return await _studentRepo.GetAllAsync();
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _studentRepo.GetByIdAsync(id);
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
        {
            return await _studentRepo.GetByUserIdAsync(userId);
        }

        public async Task<List<User>> GetUsersWithoutStudentProfileAsync()
        {
            return await _userRepo.GetUsersWithoutStudentProfileAsync();
        }

        public async Task<List<Major>> GetActiveMajorsAsync()
        {
            return await _majorRepo.GetActiveAsync();
        }

        public async Task<bool> CreateStudentAsync(Student student)
        {
            try
            {
                // Check if student code already exists
                if (await _studentRepo.StudentCodeExistsAsync(student.StudentCode))
                {
                    return false;
                }

                // Check if user already has a student profile
                if (await _studentRepo.GetByUserIdAsync(student.UserId) != null)
                {
                    return false;
                }

                await _studentRepo.AddAsync(student);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateStudentAsync(Student student)
        {
            try
            {
                var existing = await _studentRepo.GetByIdAsync(student.Id);
                if (existing == null)
                {
                    return false;
                }

                // Check if student code is being changed to an existing one
                if (existing.StudentCode != student.StudentCode)
                {
                    if (await _studentRepo.StudentCodeExistsAsync(student.StudentCode))
                    {
                        return false;
                    }
                }

                await _studentRepo.UpdateAsync(student);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            try
            {
                if (!await _studentRepo.ExistsAsync(id))
                {
                    return false;
                }

                await _studentRepo.DeleteAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StudentCodeExistsAsync(string studentCode)
        {
            return await _studentRepo.StudentCodeExistsAsync(studentCode);
        }
    }
}

