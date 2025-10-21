using BusinessObject.Models;

namespace Repositories.Students
{
    public interface IStudentRepository
    {
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task<Student?> GetByUserIdAsync(int userId);
        Task<Student?> GetByIdAsync(int id);
        Task<List<Student>> GetAllAsync();
        Task<List<Student>> GetByStudentCodesAsync(List<string> studentCodes);
        Task<bool> ExistsAsync(int id);
        Task<List<User>> GetUsersWithoutStudentInfoAsync(); // Users with Student role but no Student record
        Task AddAsync(Student student);
        Task AddRangeAsync(List<Student> students);
        Task UpdateAsync(Student student);
        Task DeleteAsync(int id);
        Task SaveChangesAsync();
        Task<List<Student>> GetAllActiveStudentsAsync();
        Task<List<Student>> SearchStudentsAsync(string query);
    }
}
