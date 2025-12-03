using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Chatbot
{
    /// <summary>
    /// Request model for sending a message to the chatbot
    /// </summary>
    /// <example>
    /// {
    ///   "message": "Tôi muốn tìm câu lạc bộ về công nghệ",
    ///   "sessionId": 123
    /// }
    /// </example>
    public class ChatRequestDto
    {
        /// <summary>
        /// The message content from the user (1-1000 characters)
        /// </summary>
        /// <example>Tôi muốn tìm câu lạc bộ về công nghệ</example>
        [Required(ErrorMessage = "Message is required")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string Message { get; set; } = null!;

        /// <summary>
        /// Optional session ID to continue an existing conversation. If null, a new session will be created.
        /// </summary>
        /// <example>123</example>
        public int? SessionId { get; set; }
    }
}
