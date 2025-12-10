namespace BusinessObject.DTOs.Activity
{
    public class AdminActivityRegistrantDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool? IsPresent { get; set; }
        public int? ParticipationScore { get; set; }
    }
}




