using BusinessObject.DTOs.Chatbot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Chatbot;
using System.Security.Claims;
using WebAPI.Constants;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatbotController(
            IChatbotService chatbotService,
            ILogger<ChatbotController> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _chatbotService = chatbotService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Send a message to the AI chatbot and receive a response
        /// </summary>
        /// <param name="request">Chat message request containing the user message and conversation history</param>
        /// <returns>AI response with timestamp and success status</returns>
        [HttpPost("message")]
        public async Task<ActionResult<ChatMessageResponseDto>> SendMessage([FromBody] ChatMessageRequestDto request)
        {
            // Generate correlation ID for tracking
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[{CorrelationId}] Invalid chat message request: {Errors}", 
                        correlationId, 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    
                    return BadRequest(new ChatMessageResponseDto
                    {
                        Message = string.Empty,
                        Timestamp = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ChatbotErrorMessages.InvalidMessage
                    });
                }

                // Extract user ID from claims
                var userId = GetUserIdFromClaims();
                
                // DEBUG: Log all claims
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogInformation("[{CorrelationId}] User claims: {Claims}", correlationId, string.Join(", ", allClaims));
                
                if (userId == null)
                {
                    _logger.LogWarning("[{CorrelationId}] Unauthorized access attempt - no user ID in claims", correlationId);
                    
                    return Unauthorized(new ChatMessageResponseDto
                    {
                        Message = string.Empty,
                        Timestamp = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ChatbotErrorMessages.Unauthorized
                    });
                }

                _logger.LogInformation("[{CorrelationId}] Processing chat message for user {UserId}", 
                    correlationId, userId.Value);

                // Call chatbot service to process the message
                var aiResponse = await _chatbotService.ProcessChatMessageAsync(
                    userId.Value,
                    request.Message,
                    request.ConversationHistory);

                _logger.LogInformation("[{CorrelationId}] Successfully processed chat message for user {UserId}", 
                    correlationId, userId.Value);

                return Ok(new ChatMessageResponseDto
                {
                    Message = aiResponse,
                    Timestamp = DateTime.UtcNow,
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Student not found", correlationId);
                
                return NotFound(new ChatMessageResponseDto
                {
                    Message = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ChatbotErrorMessages.StudentNotFound
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Network error calling Gemini API", correlationId);
                
                return StatusCode(502, new ChatMessageResponseDto
                {
                    Message = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ChatbotErrorMessages.NetworkError
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Timeout calling Gemini API", correlationId);
                
                return StatusCode(503, new ChatMessageResponseDto
                {
                    Message = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ChatbotErrorMessages.Timeout
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("API key") || ex.Message.Contains("quota") || ex.Message.Contains("rate limit"))
            {
                _logger.LogError(ex, "[{CorrelationId}] Gemini API error: {Message}", correlationId, ex.Message);
                
                // Determine specific error type
                var errorMessage = ex.Message.ToLower().Contains("quota") || ex.Message.ToLower().Contains("rate limit")
                    ? ChatbotErrorMessages.QuotaExceeded
                    : ChatbotErrorMessages.InvalidApiKey;
                
                return StatusCode(503, new ChatMessageResponseDto
                {
                    Message = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = errorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Unexpected error processing chat message", correlationId);
                
                return StatusCode(500, new ChatMessageResponseDto
                {
                    Message = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ChatbotErrorMessages.GenericError
                });
            }
        }

        /// <summary>
        /// Extract user ID from JWT claims
        /// </summary>
        /// <returns>User ID if found, null otherwise</returns>
        private int? GetUserIdFromClaims()
        {
            // Try to get user ID from NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}
