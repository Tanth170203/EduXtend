namespace BusinessObject.DTOs.JoinRequest
{
    public class JoinRequestDto
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string? ClubLogoUrl { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Motivation { get; set; }
        public string? CvUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedById { get; set; }
        public string? ProcessedByName { get; set; }
        
        // Interview info
        public bool HasInterview { get; set; }
        public int? InterviewId { get; set; }
    }
}

