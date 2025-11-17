using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class CheckInDto
    {
        [Required(ErrorMessage = "Attendance code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Attendance code must be exactly 6 characters")]
        public string AttendanceCode { get; set; } = null!;
    }
}
