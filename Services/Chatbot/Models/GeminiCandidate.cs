namespace Services.Chatbot.Models
{
    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
        public int? Index { get; set; }
        public List<GeminiSafetyRating>? SafetyRatings { get; set; }
    }
}
