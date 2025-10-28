namespace BusinessObject.DTOs.Club
{
    public class ClubAwardDto
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public DateTime AwardedAt { get; set; }
    }
}

