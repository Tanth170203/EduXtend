using BusinessObject.Models;

namespace Repositories.Students
{
    public interface IStudentRepository
    {
        Task<Student?> GetByStudentCodeAsync(string studentCode);
        Task<Student?> GetByUserIdAsync(int userId);
        Task<List<Student>> GetByStudentCodesAsync(List<string> studentCodes);
        Task AddAsync(Student student);
        Task AddRangeAsync(List<Student> students);
        Task SaveChangesAsync();
    }
}
