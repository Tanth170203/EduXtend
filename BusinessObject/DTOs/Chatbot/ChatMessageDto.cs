using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    public class ChatMessageDto
    {
        [Required]
        public string Role { get; set; } = string.Empty; // "user" or "assistant"

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}
