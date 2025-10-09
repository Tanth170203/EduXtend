using BusinessObject.Enum;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Student
{
    public class CreateStudentDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string StudentCode { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string? Phone { get; set; }

        [Required]
        public DateTime EnrollmentDate { get; set; }

        [Required]
        public StudentStatus Status { get; set; }

        [Required]
        public int MajorId { get; set; }
    }
}

