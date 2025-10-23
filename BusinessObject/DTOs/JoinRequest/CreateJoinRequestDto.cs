using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.JoinRequest
{
    public class CreateJoinRequestDto
    {
        [Required]
        public int ClubId { get; set; }

        public int? DepartmentId { get; set; }

        [MaxLength(500)]
        public string? Motivation { get; set; }

        [MaxLength(255)]
        public string? CvUrl { get; set; }
    }
}

