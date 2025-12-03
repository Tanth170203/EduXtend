namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Represents a single message in a chat conversation
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// The unique ID of the message
        /// </summary>
        /// <example>456</example>
        public int Id { get; set; }
        
        /// <summary>
        /// The role of the message sender: "user" or "assistant"
        /// </summary>
        /// <example>user</example>
        public string Role { get; set; } = null!;
        
        /// <summary>
        /// The content of the message
        /// </summary>
        /// <example>Tôi muốn tìm câu lạc bộ về công nghệ</example>
        public string Content { get; set; } = null!;
        
        /// <summary>
        /// When the message was created (UTC)
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime CreatedAt { get; set; }
    }
}
