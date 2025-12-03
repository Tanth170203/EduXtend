using System.ComponentModel.DataAnnotations;

namespace Services.Chatbot.Models;

/// <summary>
/// Represents a single recommendation item (club or activity) with relevance scoring
/// </summary>
public class RecommendationItem
{
    /// <summary>
    /// The unique identifier of the club or activity
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the club or activity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of recommendation: "club" or "activity"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A brief description of the club or activity
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The reason why this recommendation is relevant to the student
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The relevance score indicating how well this matches the student's profile (0-100%)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Relevance score must be between 0 and 100")]
    public int RelevanceScore { get; set; }
}
