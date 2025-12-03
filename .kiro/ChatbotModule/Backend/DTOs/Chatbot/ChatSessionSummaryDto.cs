namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Summary information about a chat session
    /// </summary>
    public class ChatSessionSummaryDto
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
        /// Total number of messages in this session
        /// </summary>
        /// <example>8</example>
        public int MessageCount { get; set; }
        
        /// <summary>
        /// Preview of the last message content
        /// </summary>
        /// <example>Cảm ơn bạn đã giúp tôi tìm câu lạc bộ phù hợp!</example>
        public string? LastMessage { get; set; }
    }
}
