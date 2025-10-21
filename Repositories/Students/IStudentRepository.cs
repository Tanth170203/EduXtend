using BusinessObject.Models;

namespace Repositories.Students
{
    public interface IStudentRepository
    {
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task<Student?> GetByUserIdAsync(int userId);
        Task<Student?> GetByIdAsync(int id);
<<<<<<< HEAD
        Task<List<Student>> GetByStudentCodesAsync(List<string> studentCodes);
        Task<bool> ExistsAsync(int id);
=======
        Task<List<Student>> GetAllAsync();
        Task<List<Student>> GetByStudentCodesAsync(List<string> studentCodes);
        Task<List<User>> GetUsersWithoutStudentInfoAsync(); // Users with Student role but no Student record
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414
        Task AddAsync(Student student);
        Task AddRangeAsync(List<Student> students);
        Task UpdateAsync(Student student);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
        Task<List<Student>> GetAllActiveStudentsAsync();
        Task<List<Student>> SearchStudentsAsync(string query);
    }
}
