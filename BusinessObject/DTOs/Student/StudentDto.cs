using BusinessObject.Enum;

namespace BusinessObject.DTOs.Student;

public class StudentDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = null!;
    public string Cohort { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public StudentStatus Status { get; set; }
    public int UserId { get; set; }
    public int MajorId { get; set; }
    public string? MajorName { get; set; }
    public string? MajorCode { get; set; }
}

