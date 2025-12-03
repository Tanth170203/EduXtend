using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Response model containing the chatbot's reply and optional structured recommendations
    /// </summary>
    public class ChatResponseDto
    {
        /// <summary>
        /// The chatbot's response message in Vietnamese
        /// </summary>
        /// <example>Dựa trên thông tin của bạn, tôi đề xuất các câu lạc bộ sau...</example>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the chat session (optional, for session tracking)
        /// </summary>
        /// <example>123</example>
        public int? SessionId { get; set; }

        /// <summary>
        /// Timestamp when the response was generated (UTC)
        /// </summary>
        /// <example>2024-01-15T10:30:00Z</example>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        /// <example>null</example>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Indicates whether the response contains structured recommendations
        /// </summary>
        /// <example>true</example>
        public bool HasRecommendations { get; set; }

        /// <summary>
        /// List of structured recommendations (clubs or activities) if applicable
        /// </summary>
        public List<RecommendationDto>? Recommendations { get; set; }
    }
}
