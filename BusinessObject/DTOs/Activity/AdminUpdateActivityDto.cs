using BusinessObject.Enum;

namespace BusinessObject.DTOs.Activity
{
    public class AdminUpdateActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ActivityType Type { get; set; }
        public bool IsPublic { get; set; }
        public int? MaxParticipants { get; set; }
        public double MovementPoint { get; set; }
        // Admin context: ClubId, ApprovedById, RequiresApproval stay null
    }
}



