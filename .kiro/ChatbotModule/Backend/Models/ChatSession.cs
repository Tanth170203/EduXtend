using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ChatSession
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
