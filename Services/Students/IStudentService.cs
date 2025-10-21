using BusinessObject.DTOs.Student;
using BusinessObject.Models;

namespace Services.Students;

public interface IStudentService
{
    Task<List<StudentDto>> GetAllAsync();
    Task<StudentDto?> GetByIdAsync(int id);
    Task<List<User>> GetUsersWithoutStudentInfoAsync();
    Task<StudentDto> CreateAsync(CreateStudentDto dto);
    Task<StudentDto> UpdateAsync(int id, UpdateStudentDto dto);
    Task<bool> DeleteAsync(int id);
}

