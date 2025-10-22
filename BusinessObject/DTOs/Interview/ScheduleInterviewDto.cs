using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Interview
{
    public class ScheduleInterviewDto
    {
        [Required]
        public int JoinRequestId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}

