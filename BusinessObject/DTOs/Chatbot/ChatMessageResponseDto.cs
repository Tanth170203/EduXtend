using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    public class ChatMessageResponseDto
    {
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
