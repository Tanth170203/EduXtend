namespace BusinessObject.DTOs.Activity
{
    /// <summary>
    /// DTO for activity data extracted from uploaded files using AI
    /// </summary>
    public class ExtractedActivityDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? MaxParticipants { get; set; }
        public string? SuggestedType { get; set; }
        public string? RawExtractedText { get; set; }
        public List<ExtractedScheduleDto>? Schedules { get; set; }
    }

    /// <summary>
    /// DTO for schedule item extracted from uploaded files
    /// </summary>
    public class ExtractedScheduleDto
    {
        public string? StartTime { get; set; } // HH:mm format
        public string? EndTime { get; set; } // HH:mm format
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
