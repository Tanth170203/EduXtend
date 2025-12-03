using System.ComponentModel.DataAnnotations;

namespace Services.Chatbot.Models;

/// <summary>
/// Represents a single news/post recommendation item with relevance scoring
/// </summary>
public class NewsRecommendationItem
{
    /// <summary>
    /// The unique identifier of the news post
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The title of the news post
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The type of news: "club_news" or "system_news"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A brief summary or excerpt of the news content
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The source of the news (club name or "Hệ thống")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The category of the news (e.g., "Tin CLB", "Thông báo")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The published date of the news
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// The reason why this news is relevant to the student's query
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The relevance score indicating how well this matches the query (0-100%)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Relevance score must be between 0 and 100")]
    public int RelevanceScore { get; set; }
}
