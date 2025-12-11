using BusinessObject.Enum;

namespace BusinessObject.DTOs.Activity
{
    public class ClubCreateActivityDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ActivityType Type { get; set; }
        public bool IsPublic { get; set; }
        public int MaxParticipants { get; set; }
        public double MovementPoint { get; set; }
        public int? ClubCollaborationId { get; set; }
        public int? CollaborationPoint { get; set; }
        
        /// <summary>
        /// For Club Activities (ClubMeeting, ClubTraining, ClubWorkshop) only.
        /// If true, automatically register all club members and set MaxParticipants to club member count.
        /// </summary>
        public bool IsMandatory { get; set; }
        
        // GPS Location fields for GPS-based attendance (default: Đà Nẵng)
        public double? GpsLatitude { get; set; } = 15.967483;
        public double? GpsLongitude { get; set; } = 108.260361;
        
        // GPS Check-in configuration (always enabled by default)
        public bool IsGpsCheckInEnabled { get; set; } = true;
        public int GpsCheckInRadius { get; set; } = 100; // Default 100m
        public int CheckInWindowMinutes { get; set; } = 10; // Default 10 minutes
        public int CheckOutWindowMinutes { get; set; } = 10; // Default 10 minutes
    }
}



