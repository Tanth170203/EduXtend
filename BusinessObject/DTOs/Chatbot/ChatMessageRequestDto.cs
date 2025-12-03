using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    public class ChatMessageRequestDto
    {
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public List<ChatMessageDto>? ConversationHistory { get; set; }
    }
}
