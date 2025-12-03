namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Complete chat history for a specific session
    /// </summary>
    public class ChatHistoryDto
    {
        /// <summary>
        /// The ID of the chat session
        /// </summary>
        /// <example>123</example>
        public int SessionId { get; set; }
        
        /// <summary>
        /// When the chat session was created (UTC)
        /// </summary>
        /// <example>2024-01-15T09:00:00Z</example>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the last message was sent in this session (UTC)
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime? LastMessageAt { get; set; }
        
        /// <summary>
        /// List of all messages in the session, ordered by creation time
        /// </summary>
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }
}
