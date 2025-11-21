namespace BusinessObject.DTOs.Activity
{
    public class CollaborationInvitationDto
    {
        public int ActivityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OrganizingClubId { get; set; }
        public string OrganizingClubName { get; set; } = string.Empty;
        public string? OrganizingClubLogoUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? CollaborationPoint { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
