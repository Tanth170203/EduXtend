using BusinessObject.DTOs.Chatbot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Chatbot;
using System.Security.Claims;
using WebAPI.Middleware;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Controller for AI Chatbot interactions and recommendations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the chatbot and receive a response
        /// </summary>
        /// <remarks>
        /// This endpoint allows authenticated students to send messages to the AI chatbot.
        /// The chatbot can provide recommendations for clubs and activities based on the student's profile.
        /// 
        /// **Rate Limit:** 15 requests per minute per user
        /// 
        /// **Intent Detection:**
        /// - Club recommendations: Use keywords like "câu lạc bộ", "CLB", "tham gia"
        /// - Activity recommendations: Use keywords like "hoạt động", "sự kiện", "event"
        /// - General conversation: Any other message
        /// 
        /// **Example Request:**
        /// ```json
        /// {
        ///   "message": "Tôi muốn tìm câu lạc bộ về công nghệ",
        ///   "sessionId": null
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">Chat request containing the message and optional session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Chat response with assistant's reply and recommendations</returns>
        /// <response code="200">Successfully processed the message and returned a response</response>
        /// <response code="400">Invalid request (e.g., message too long or empty)</response>
        /// <response code="401">User is not authenticated or has invalid credentials</response>
        /// <response code="429">Rate limit exceeded - too many requests in a short time</response>
        /// <response code="500">Internal server error or Gemini API unavailable</response>
        [HttpPost("message")]
        [ChatbotRateLimit]
        [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendMessage(
            [FromBody] ChatRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get userId from claims
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in claims");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // Validate request
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid chat request from user {UserId}", userId);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation(
                    "Processing chat message from user {UserId}, SessionId: {SessionId}",
                    userId, request.SessionId);

                // Send message to chatbot service
                var response = await _chatbotService.SendMessageAsync(userId, request, cancellationToken);

                _logger.LogInformation(
                    "Chat message processed successfully for user {UserId}, SessionId: {SessionId}",
                    userId, response.SessionId);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in chat request");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during chat processing");
                return StatusCode(500, new { message = "Chatbot tạm thời không khả dụng. Vui lòng thử lại sau." });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat request cancelled by user");
                return StatusCode(499, new { message = "Request cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý tin nhắn. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Get chat history for a specific session
        /// </summary>
        /// <remarks>
        /// Retrieves the complete conversation history for a specific chat session.
        /// Users can only access their own chat sessions.
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "sessionId": 123,
        ///   "createdAt": "2024-01-15T09:00:00Z",
        ///   "lastMessageAt": "2024-01-15T10:30:00Z",
        ///   "messages": [
        ///     {
        ///       "id": 1,
        ///       "role": "user",
        ///       "content": "Tôi muốn tìm câu lạc bộ về công nghệ",
        ///       "createdAt": "2024-01-15T09:00:00Z"
        ///     },
        ///     {
        ///       "id": 2,
        ///       "role": "assistant",
        ///       "content": "Dựa trên thông tin của bạn...",
        ///       "createdAt": "2024-01-15T09:00:05Z"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="sessionId">The ID of the chat session</param>
        /// <returns>Chat history with all messages</returns>
        /// <response code="200">Successfully retrieved chat history</response>
        /// <response code="401">User is not authenticated or does not own this session</response>
        /// <response code="404">Chat session not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("history/{sessionId:int}")]
        [ProducesResponseType(typeof(ChatHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetChatHistory(int sessionId)
        {
            try
            {
                // Get userId from claims
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in claims");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                _logger.LogInformation(
                    "Retrieving chat history for user {UserId}, SessionId: {SessionId}",
                    userId, sessionId);

                // Get chat history
                var history = await _chatbotService.GetChatHistoryAsync(userId, sessionId);

                if (history == null)
                {
                    _logger.LogWarning(
                        "Chat session {SessionId} not found for user {UserId}",
                        sessionId, userId);
                    return NotFound(new { message = "Phiên chat không tồn tại" });
                }

                _logger.LogInformation(
                    "Retrieved {MessageCount} messages for session {SessionId}",
                    history.Messages.Count, sessionId);

                return Ok(history);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to chat history");
                return Unauthorized(new { message = "Bạn không có quyền truy cập phiên chat này" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history for session {SessionId}", sessionId);
                return StatusCode(500, new { message = "Không thể tải lịch sử chat. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Get all chat sessions for the current user
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all chat sessions belonging to the authenticated user,
        /// ordered by most recent activity first.
        /// 
        /// **Example Response:**
        /// ```json
        /// [
        ///   {
        ///     "sessionId": 123,
        ///     "createdAt": "2024-01-15T09:00:00Z",
        ///     "lastMessageAt": "2024-01-15T10:30:00Z",
        ///     "messageCount": 8,
        ///     "lastMessage": "Cảm ơn bạn đã giúp tôi!"
        ///   },
        ///   {
        ///     "sessionId": 122,
        ///     "createdAt": "2024-01-14T14:00:00Z",
        ///     "lastMessageAt": "2024-01-14T14:15:00Z",
        ///     "messageCount": 4,
        ///     "lastMessage": "Tôi muốn tìm hoạt động về thể thao"
        ///   }
        /// ]
        /// ```
        /// </remarks>
        /// <returns>List of chat session summaries</returns>
        /// <response code="200">Successfully retrieved list of sessions</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("sessions")]
        [ProducesResponseType(typeof(List<ChatSessionSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserSessions()
        {
            try
            {
                // Get userId from claims
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in claims");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                _logger.LogInformation("Retrieving chat sessions for user {UserId}", userId);

                // Get user sessions
                var sessions = await _chatbotService.GetUserSessionsAsync(userId);

                _logger.LogInformation(
                    "Retrieved {SessionCount} sessions for user {UserId}",
                    sessions.Count, userId);

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user sessions");
                return StatusCode(500, new { message = "Không thể tải danh sách phiên chat. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Create a new chat session
        /// </summary>
        /// <remarks>
        /// Creates a new chat session for the authenticated user.
        /// This is useful when starting a fresh conversation with the chatbot.
        /// 
        /// **Example Response:**
        /// ```json
        /// {
        ///   "sessionId": 124,
        ///   "createdAt": "2024-01-15T11:00:00Z",
        ///   "isActive": true,
        ///   "message": "Phiên chat mới đã được tạo"
        /// }
        /// ```
        /// </remarks>
        /// <returns>The newly created chat session</returns>
        /// <response code="200">Successfully created a new chat session</response>
        /// <response code="400">Invalid request or unable to create session</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("session/new")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateNewSession()
        {
            try
            {
                // Get userId from claims
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in claims");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                _logger.LogInformation("Creating new chat session for user {UserId}", userId);

                // Create new session
                var session = await _chatbotService.CreateNewSessionAsync(userId);

                _logger.LogInformation(
                    "Created new chat session {SessionId} for user {UserId}",
                    session.Id, userId);

                return Ok(new
                {
                    sessionId = session.Id,
                    createdAt = session.CreatedAt,
                    isActive = session.IsActive,
                    message = "Phiên chat mới đã được tạo"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot create new session for user");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new chat session");
                return StatusCode(500, new { message = "Không thể tạo phiên chat mới. Vui lòng thử lại." });
            }
        }
    }
}
