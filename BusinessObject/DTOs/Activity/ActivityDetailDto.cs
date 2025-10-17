using BusinessObject.Enum;

namespace BusinessObject.DTOs.Activity
{
    public class ActivityDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public double MovementPoint { get; set; }
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsPublic { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Club info
        public int? ClubId { get; set; }
        public string? ClubName { get; set; }
        public string? ClubLogo { get; set; }
        public string? ClubBanner { get; set; }
        
        // Creator info
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = null!;
        
        // Approver info
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        
        // Statistics
        public int RegisteredCount { get; set; }
        public int AttendedCount { get; set; }
        public int FeedbackCount { get; set; }
        
        // User-specific
        public bool CanRegister { get; set; }
        public bool IsRegistered { get; set; }
        public bool HasAttended { get; set; }
    }
}

