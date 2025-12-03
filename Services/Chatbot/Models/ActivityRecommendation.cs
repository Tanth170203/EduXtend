namespace Services.Chatbot.Models
{
    public class ActivityRecommendation
    {
        public int ActivityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }
}
