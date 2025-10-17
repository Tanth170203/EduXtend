using System.ComponentModel.DataAnnotations;
using BusinessObject.Enum;

namespace BusinessObject.DTOs.Student;

public class CreateStudentDto
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Student code is required")]
    [MaxLength(20)]
    public string StudentCode { get; set; } = null!;

    [Required(ErrorMessage = "Cohort is required")]
    [MaxLength(10)]
    public string Cohort { get; set; } = null!;

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "Enrollment date is required")]
    public DateTime EnrollmentDate { get; set; }

    [Required(ErrorMessage = "Major ID is required")]
    public int MajorId { get; set; }

    public StudentStatus Status { get; set; } = StudentStatus.Active;
}

