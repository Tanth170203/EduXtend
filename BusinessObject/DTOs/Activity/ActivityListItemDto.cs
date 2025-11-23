using BusinessObject.Enum;

namespace BusinessObject.DTOs.Activity
{
    public class ActivityListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = null!; // ActivityType as string
        public string Status { get; set; } = null!;
        public double MovementPoint { get; set; }
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsPublic { get; set; }
        public bool RequiresApproval { get; set; }
        
        // Club info (if club activity)
        public int? ClubId { get; set; }
        public string? ClubName { get; set; }
        public string? ClubLogo { get; set; }
        
        // Attendance code for self check-in
        public string? AttendanceCode { get; set; }
        
        // Registration info
        public bool CanRegister { get; set; }
        public bool IsRegistered { get; set; }
        public bool IsFull { get; set; }
        public bool HasAttended { get; set; }
        public bool HasFeedback { get; set; }
        
        // Attendance statistics
        public int AttendedCount { get; set; }
        
        // Evaluation status
        public bool HasEvaluation { get; set; }
    }
}

