using BusinessObject.Models;

namespace Services.Students
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student?> GetStudentByUserIdAsync(int userId);
        Task<List<User>> GetUsersWithoutStudentProfileAsync();
        Task<List<Major>> GetActiveMajorsAsync();
        Task<bool> CreateStudentAsync(Student student);
        Task<bool> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> StudentCodeExistsAsync(string studentCode);
    }
}

