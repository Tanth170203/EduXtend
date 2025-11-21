using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class RejectCollaborationDto
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        [MinLength(10, ErrorMessage = "Please provide a detailed reason (at least 10 characters)")]
        public string Reason { get; set; } = string.Empty;
    }
}
