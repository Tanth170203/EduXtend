using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Activity
{
    public class CreateActivityEvaluationDto
    {
        [Range(0, 10000, ErrorMessage = "Expected participants must be between 0 and 10000")]
        public int ExpectedParticipants { get; set; }
        
        [Range(0, 10000, ErrorMessage = "Actual participants must be between 0 and 10000")]
        public int ActualParticipants { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string? Reason { get; set; }
        
        [Range(0, 10, ErrorMessage = "Communication score must be between 0 and 10")]
        public int CommunicationScore { get; set; } = 8;
        
        [Range(0, 10, ErrorMessage = "Organization score must be between 0 and 10")]
        public int OrganizationScore { get; set; } = 8;
        
        [Range(0, 10, ErrorMessage = "Host score must be between 0 and 10")]
        public int HostScore { get; set; } = 8;
        
        [Range(0, 10, ErrorMessage = "Speaker score must be between 0 and 10")]
        public int SpeakerScore { get; set; } = 8;
        
        [Range(0, 10, ErrorMessage = "Success score must be between 0 and 10")]
        public int Success { get; set; } = 8;
        
        [MaxLength(2000, ErrorMessage = "Limitations cannot exceed 2000 characters")]
        public string? Limitations { get; set; }
        
        [MaxLength(2000, ErrorMessage = "Improvement measures cannot exceed 2000 characters")]
        public string? ImprovementMeasures { get; set; }
    }
}
