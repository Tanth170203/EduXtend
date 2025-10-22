namespace BusinessObject.DTOs.Interview
{
    public class InterviewDto
    {
        public int Id { get; set; }
        public int JoinRequestId { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? Evaluation { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }
}

