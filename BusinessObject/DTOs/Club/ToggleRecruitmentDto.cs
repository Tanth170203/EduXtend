namespace BusinessObject.DTOs.Club
{
    public class ToggleRecruitmentDto
    {
        public bool IsOpen { get; set; }
    }

    public class RecruitmentStatusDto
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public bool IsRecruitmentOpen { get; set; }
        public int PendingRequestCount { get; set; }
    }
}

