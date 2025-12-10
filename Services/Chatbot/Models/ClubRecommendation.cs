namespace Services.Chatbot.Models
{
    public class ClubRecommendation
    {
        public int ClubId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsRecruitmentOpen { get; set; }
    }
}
