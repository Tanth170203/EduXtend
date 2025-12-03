namespace Services.Chatbot.Models
{
    public class GeminiResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = new();
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    public class GeminiPromptFeedback
    {
        public string? BlockReason { get; set; }
        public List<GeminiSafetyRating>? SafetyRatings { get; set; }
    }

    public class GeminiSafetyRating
    {
        public string? Category { get; set; }
        public string? Probability { get; set; }
    }
}
