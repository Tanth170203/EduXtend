using BusinessObject.DTOs.Student;
using BusinessObject.Models;
using Repositories.Students;
using Repositories.Majors;
using Repositories.Users;

namespace Services.Students;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepo;
    private readonly IMajorRepository _majorRepo;
    private readonly IUserRepository _userRepo;

    public StudentService(IStudentRepository studentRepo, IMajorRepository majorRepo, IUserRepository userRepo)
    {
        _studentRepo = studentRepo;
        _majorRepo = majorRepo;
        _userRepo = userRepo;
    }

    public async Task<List<StudentDto>> GetAllAsync()
    {
        var students = await _studentRepo.GetAllAsync();
        return students.Select(s => new StudentDto
        {
            Id = s.Id,
            StudentCode = s.StudentCode,
            Cohort = s.Cohort,
            FullName = s.FullName,
            Email = s.Email,
            Phone = s.Phone,
            DateOfBirth = s.DateOfBirth,
            Gender = s.Gender,
            EnrollmentDate = s.EnrollmentDate,
            Status = s.Status,
            UserId = s.UserId,
            MajorId = s.MajorId,
            MajorName = s.Major?.Name,
            MajorCode = s.Major?.Code
        }).ToList();
    }

    public async Task<StudentDto?> GetByIdAsync(int id)
    {
        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
            return null;

        return new StudentDto
        {
            Id = student.Id,
            StudentCode = student.StudentCode,
            Cohort = student.Cohort,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            EnrollmentDate = student.EnrollmentDate,
            Status = student.Status,
            UserId = student.UserId,
            MajorId = student.MajorId,
            MajorName = student.Major?.Name,
            MajorCode = student.Major?.Code
        };
    }

    public async Task<List<User>> GetUsersWithoutStudentInfoAsync()
    {
        return await _studentRepo.GetUsersWithoutStudentInfoAsync();
    }

    public async Task<StudentDto> CreateAsync(CreateStudentDto dto)
    {
        // Validate user exists
        var existingStudent = await _studentRepo.GetByUserIdAsync(dto.UserId);
        if (existingStudent != null)
            throw new InvalidOperationException($"User ID {dto.UserId} already has student information");

        // Validate student code unique
        var studentByCode = await _studentRepo.GetByStudentCodeAsync(dto.StudentCode);
        if (studentByCode != null)
            throw new InvalidOperationException($"Student code {dto.StudentCode} already exists");

        // Validate major exists
        var major = await _majorRepo.GetByIdAsync(dto.MajorId);
        if (major == null)
            throw new KeyNotFoundException($"Major with ID {dto.MajorId} not found");

        // Load user to populate denormalized fields
        var user = await _userRepo.GetByIdAsync(dto.UserId)
            ?? throw new KeyNotFoundException($"User with ID {dto.UserId} not found");

        var student = new Student
        {
            UserId = dto.UserId,
            StudentCode = dto.StudentCode,
            Cohort = dto.Cohort,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            EnrollmentDate = dto.EnrollmentDate,
            MajorId = dto.MajorId,
            Status = dto.Status,
            // Fill from user profile to satisfy non-null constraints
            FullName = user.FullName ?? string.Empty,
            Email = user.Email,
            Phone = user.PhoneNumber
        };

        await _studentRepo.AddAsync(student);

        // Reload to get related data
        var created = await _studentRepo.GetByIdAsync(student.Id);
        return new StudentDto
        {
            Id = created!.Id,
            StudentCode = created.StudentCode,
            Cohort = created.Cohort,
            FullName = created.FullName,
            Email = created.Email,
            Phone = created.Phone,
            DateOfBirth = created.DateOfBirth,
            Gender = created.Gender,
            EnrollmentDate = created.EnrollmentDate,
            Status = created.Status,
            UserId = created.UserId,
            MajorId = created.MajorId,
            MajorName = created.Major?.Name,
            MajorCode = created.Major?.Code
        };
    }

    public async Task<StudentDto> UpdateAsync(int id, UpdateStudentDto dto)
    {
        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {id} not found");

        // Validate student code unique (except current)
        var studentByCode = await _studentRepo.GetByStudentCodeAsync(dto.StudentCode);
        if (studentByCode != null && studentByCode.Id != id)
            throw new InvalidOperationException($"Student code {dto.StudentCode} already exists");

        // Validate major exists
        var major = await _majorRepo.GetByIdAsync(dto.MajorId);
        if (major == null)
            throw new KeyNotFoundException($"Major with ID {dto.MajorId} not found");

        student.StudentCode = dto.StudentCode;
        student.Cohort = dto.Cohort;
        student.DateOfBirth = dto.DateOfBirth;
        student.Gender = dto.Gender;
        student.EnrollmentDate = dto.EnrollmentDate;
        student.MajorId = dto.MajorId;
        student.Status = dto.Status;

        await _studentRepo.UpdateAsync(student);

        // Reload to get updated data
        var updated = await _studentRepo.GetByIdAsync(id);
        return new StudentDto
        {
            Id = updated!.Id,
            StudentCode = updated.StudentCode,
            Cohort = updated.Cohort,
            FullName = updated.FullName,
            Email = updated.Email,
            Phone = updated.Phone,
            DateOfBirth = updated.DateOfBirth,
            Gender = updated.Gender,
            EnrollmentDate = updated.EnrollmentDate,
            Status = updated.Status,
            UserId = updated.UserId,
            MajorId = updated.MajorId,
            MajorName = updated.Major?.Name,
            MajorCode = updated.Major?.Code
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var student = await _studentRepo.GetByIdAsync(id);
        if (student == null)
            return false;

        await _studentRepo.DeleteAsync(id);
        return true;
    }
}

