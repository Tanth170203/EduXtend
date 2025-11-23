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
    }
}



