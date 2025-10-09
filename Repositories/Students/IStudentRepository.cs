using BusinessObject.Models;

namespace Repositories.Students
{
    public interface IStudentRepository
    {
        Task<List<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(int id);
        Task<Student?> GetByUserIdAsync(int userId);
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> StudentCodeExistsAsync(string studentCode);
        Task SaveChangesAsync();
    }
}

