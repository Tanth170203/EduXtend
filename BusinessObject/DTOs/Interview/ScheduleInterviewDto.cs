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

        /// <summary>
        /// GPS Latitude for offline interview location (optional)
        /// Valid range: -90 to 90 degrees
        /// </summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public decimal? Latitude { get; set; }

        /// <summary>
        /// GPS Longitude for offline interview location (optional)
        /// Valid range: -180 to 180 degrees
        /// </summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public decimal? Longitude { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}

