namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Response model containing the chatbot's reply and optional recommendations
    /// </summary>
    public class ChatResponseDto
    {
        /// <summary>
        /// The ID of the chat session (null for non-students without session persistence)
        /// </summary>
        /// <example>123</example>
        public int? SessionId { get; set; }
        
        /// <summary>
        /// The chatbot's response message in Vietnamese
        /// </summary>
        /// <example>Dựa trên thông tin của bạn, tôi đề xuất các câu lạc bộ sau...</example>
        public string Response { get; set; } = null!;
        
        /// <summary>
        /// List of recommendations (clubs or activities) if applicable
        /// </summary>
        public List<RecommendationDto>? Recommendations { get; set; }
        
        /// <summary>
        /// Timestamp when the response was generated (UTC)
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime Timestamp { get; set; }
    }
}
