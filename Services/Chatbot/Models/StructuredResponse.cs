namespace Services.Chatbot.Models;

/// <summary>
/// Represents a structured response from the AI containing a message and recommendations
/// </summary>
public class StructuredResponse
{
    /// <summary>
    /// The introductory message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of recommendation items (clubs or activities)
    /// </summary>
    public List<RecommendationItem> Recommendations { get; set; } = new();

    /// <summary>
    /// List of news recommendation items
    /// </summary>
    public List<NewsRecommendationItem> NewsRecommendations { get; set; } = new();
}
