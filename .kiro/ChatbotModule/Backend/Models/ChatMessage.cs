using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;

    [Required, MaxLength(10)]
    public string Role { get; set; } = null!; // "user" or "assistant"

    [Required]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: Store structured recommendation data as JSON string
    public string? RecommendationData { get; set; }
}
