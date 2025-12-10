namespace BusinessObject.DTOs.Activity
{
    public class ActivityEvaluationDto
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = null!;
        public DateTime ActivityStartTime { get; set; }
        public DateTime ActivityEndTime { get; set; }
        
        public int ExpectedParticipants { get; set; }
        public int ActualParticipants { get; set; }
        public string? Reason { get; set; }
        
        public int CommunicationScore { get; set; }
        public int OrganizationScore { get; set; }
        public int HostScore { get; set; }
        public int SpeakerScore { get; set; }
        public int Success { get; set; }
        public double AverageScore { get; set; }
        
        public string? Limitations { get; set; }
        public string? ImprovementMeasures { get; set; }
        
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
