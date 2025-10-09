using BusinessObject.Enum;

namespace BusinessObject.DTOs.Student
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public StudentStatus Status { get; set; }
        public int UserId { get; set; }
        public int MajorId { get; set; }

        // Related entities
        public UserInfoDto? User { get; set; }
        public MajorDto? Major { get; set; }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class MajorDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}

