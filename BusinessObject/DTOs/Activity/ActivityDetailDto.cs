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
        
        // Collaboration info
        public int? ClubCollaborationId { get; set; }
        public string? CollaboratingClubName { get; set; }
        public int? CollaborationPoint { get; set; }
        public string? CollaborationStatus { get; set; }
        public string? CollaborationRejectionReason { get; set; }
        public bool IsCollaboratedActivity { get; set; } // True if current user's club is the collaborating club (not owner)
        
        // Creator info
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = null!;
        
        // Approver info
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        
        // Rejection info
        public string? RejectionReason { get; set; }
        
        // Statistics
        public int RegisteredCount { get; set; }
        public int AttendedCount { get; set; }
        public int FeedbackCount { get; set; }
        
        // User-specific
        public bool CanRegister { get; set; }
        public bool IsRegistered { get; set; }
        public bool HasAttended { get; set; }
        
        // Attendance code (only visible to Admin/Manager)
        public string? AttendanceCode { get; set; }
        
        // Schedules (only for complex activities)
        public List<ActivityScheduleDto>? Schedules { get; set; }
        
        // Evaluation status
        public bool HasEvaluation { get; set; }
    }
}

