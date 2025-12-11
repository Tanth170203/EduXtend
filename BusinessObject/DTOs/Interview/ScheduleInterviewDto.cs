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
        [RegularExpression("^(Online|Offline)$", ErrorMessage = "Interview type must be 'Online' or 'Offline'")]
        public string InterviewType { get; set; } = "Offline";

        /// <summary>
        /// For Offline: Physical address
        /// For Online: Google Meet link (user enters manually)
        /// </summary>
        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}

