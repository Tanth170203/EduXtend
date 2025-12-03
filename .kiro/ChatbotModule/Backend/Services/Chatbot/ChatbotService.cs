using BusinessObject.DTOs.Chatbot;
using BusinessObject.Models;
using Microsoft.Extensions.Logging;
using Repositories.ChatSessions;
using Repositories.Students;
using Services.Recommendations;
using System.Text.Json;

namespace Services.Chatbot;

public class ChatbotService : IChatbotService
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGeminiApiClient _geminiApiClient;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IChatbotMetricsService _metricsService;
    private readonly ILogger<ChatbotService> _logger;

    // Intent detection keywords
    private static readonly string[] ClubKeywords = { "câu lạc bộ", "clb", "tham gia", "đăng ký", "club" };
    private static readonly string[] ActivityKeywords = { "hoạt động", "sự kiện", "event", "hđnk", "activity" };

    public ChatbotService(
        IChatSessionRepository chatSessionRepository,
        IStudentRepository studentRepository,
        IGeminiApiClient geminiApiClient,
        IRecommendationEngine recommendationEngine,
        IChatbotMetricsService metricsService,
        ILogger<ChatbotService> logger)
    {
        _chatSessionRepository = chatSessionRepository;
        _studentRepository = studentRepository;
        _geminiApiClient = geminiApiClient;
        _recommendationEngine = recommendationEngine;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Detects the intent of the user's message
    /// </summary>
    private ChatIntent DetectIntent(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        // Check for club recommendation intent
        if (ClubKeywords.Any(keyword => lowerMessage.Contains(keyword)))
        {
            return ChatIntent.ClubRecommendation;
        }

        // Check for activity recommendation intent
        if (ActivityKeywords.Any(keyword => lowerMessage.Contains(keyword)))
        {
            return ChatIntent.ActivityRecommendation;
        }

        // Default to general conversation
        return ChatIntent.GeneralConversation;
    }

    public async Task<ChatResponseDto> SendMessageAsync(
        int userId,
        ChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        
        try
        {
            // Validate message length (already validated by DataAnnotations, but double-check)
            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 1000)
            {
                throw new ArgumentException("Message must be between 1 and 1000 characters");
            }

            // Get student info first to determine if we can create sessions
            var student = await _studentRepository.GetByUserIdAsync(userId);
            
            // Get or create chat session (only for students with valid student records)
            ChatSession? session = null;
            if (student != null)
            {
                if (request.SessionId.HasValue)
                {
                    // Try to get existing session
                    session = await _chatSessionRepository.GetByIdAsync(request.SessionId.Value, includeMessages: true);
                    
                    if (session != null && session.StudentId != student.Id)
                    {
                        _logger.LogWarning(
                            "Session {SessionId} belongs to student {SessionStudentId} but current student is {CurrentStudentId}",
                            session.Id, session.StudentId, student.Id);
                        throw new UnauthorizedAccessException("You do not have access to this chat session");
                    }
                }
                
                if (session == null)
                {
                    // Create new session for students only
                    session = await CreateNewSessionAsync(student.Id);
                }
            }
            else
            {
                _logger.LogInformation("User {UserId} is not a student, chatting without session persistence", userId);
            }

            // Save user message to database (only if session exists)
            if (session != null)
            {
                var userMessage = new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "user",
                    Content = request.Message,
                    CreatedAt = DateTime.UtcNow
                };
                await _chatSessionRepository.AddMessageAsync(userMessage);
            }

            // Detect intent from user message
            var intent = DetectIntent(request.Message);
            _logger.LogInformation("Detected intent: {Intent} for user {UserId}", intent, userId);
            
            // Record intent in metrics
            _metricsService.RecordIntent(intent.ToString());

            // Process message based on intent (student already loaded above)
            string assistantResponse;
            List<RecommendationDto>? recommendations = null;

            switch (intent)
            {
                case ChatIntent.ClubRecommendation:
                    if (student == null)
                    {
                        assistantResponse = "Xin lỗi, tính năng đề xuất câu lạc bộ chỉ dành cho sinh viên. Bạn có thể xem danh sách câu lạc bộ tại trang Clubs.";
                    }
                    else
                    {
                        recommendations = await _recommendationEngine.GetClubRecommendationsAsync(
                            student.Id, request.Message, cancellationToken);
                        assistantResponse = BuildRecommendationResponse(recommendations, "câu lạc bộ");
                    }
                    break;

                case ChatIntent.ActivityRecommendation:
                    if (student == null)
                    {
                        assistantResponse = "Xin lỗi, tính năng đề xuất hoạt động chỉ dành cho sinh viên. Bạn có thể xem danh sách hoạt động tại trang Activities.";
                    }
                    else
                    {
                        recommendations = await _recommendationEngine.GetActivityRecommendationsAsync(
                            student.Id, request.Message, cancellationToken);
                        assistantResponse = BuildRecommendationResponse(recommendations, "hoạt động");
                    }
                    break;

                case ChatIntent.GeneralConversation:
                default:
                    if (student != null)
                    {
                        _logger.LogInformation(
                            "Sending to Gemini - UserId: {UserId}, StudentId: {StudentId}, Student: {StudentCode}, Name: {Name}, Major: {Major}",
                            userId,
                            student.Id,
                            student.StudentCode, 
                            student.User?.FullName ?? "N/A",
                            student.Major?.Name ?? "N/A");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Sending to Gemini - UserId: {UserId}, No student profile (likely Club Manager or Admin)",
                            userId);
                    }
                    
                    // Get recent message history for context (only if session exists)
                    List<ChatMessage> messageHistory;
                    if (session != null)
                    {
                        // Filter out system prompts (messages starting with "Bạn là AI Assistant" or "=== THÔNG TIN")
                        messageHistory = session.Messages
                            .Where(m => !m.Content.StartsWith("Bạn là AI Assistant") && !m.Content.StartsWith("=== THÔNG TIN"))
                            .OrderByDescending(m => m.CreatedAt)
                            .Take(10)
                            .OrderBy(m => m.CreatedAt)
                            .ToList();
                        
                        _logger.LogInformation("Message history count (filtered): {Count}", messageHistory.Count);
                    }
                    else
                    {
                        // No session for non-students, start fresh conversation
                        messageHistory = new List<ChatMessage>();
                        _logger.LogInformation("No session - starting fresh conversation");
                    }
                    
                    // Pass student context if available (null for non-students)
                    assistantResponse = await _geminiApiClient.GenerateContentWithHistoryAsync(
                        messageHistory, request.Message, student, cancellationToken);
                    break;
            }

            // Save assistant response to database (only if session exists)
            if (session != null)
            {
                var assistantMessage = new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = "assistant",
                    Content = assistantResponse,
                    CreatedAt = DateTime.UtcNow,
                    RecommendationData = recommendations != null 
                        ? JsonSerializer.Serialize(recommendations) 
                        : null
                };
                await _chatSessionRepository.AddMessageAsync(assistantMessage);

                // Update session timestamp
                await _chatSessionRepository.UpdateSessionTimestampAsync(session.Id);
            }

            // Mark as successful
            success = true;

            // Return response
            return new ChatResponseDto
            {
                SessionId = session?.Id, // Null for non-students
                Response = assistantResponse,
                Recommendations = recommendations,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API error for user {UserId}", userId);
            _metricsService.RecordGeminiApiError();
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("database") || ex.Message.Contains("Database"))
        {
            _logger.LogError(ex, "Database error for user {UserId}", userId);
            _metricsService.RecordDatabaseError();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for user {UserId}", userId);
            throw;
        }
        finally
        {
            // Record request metrics
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _metricsService.RecordRequest(success, responseTime);
            
            _logger.LogInformation(
                "Chat request completed for user {UserId}: Success={Success}, ResponseTime={ResponseTimeMs}ms",
                userId, success, Math.Round(responseTime, 2));
        }
    }

    /// <summary>
    /// Builds a friendly response message with recommendations
    /// </summary>
    private string BuildRecommendationResponse(List<RecommendationDto> recommendations, string type)
    {
        if (recommendations == null || recommendations.Count == 0)
        {
            return $"Xin lỗi, hiện tại tôi không tìm thấy {type} phù hợp với bạn. Bạn có thể thử hỏi theo cách khác hoặc cung cấp thêm thông tin về sở thích của bạn.";
        }

        var response = $"Dựa trên thông tin của bạn, tôi đề xuất các {type} sau:\n\n";
        
        for (int i = 0; i < recommendations.Count; i++)
        {
            var rec = recommendations[i];
            response += $"{i + 1}. **{rec.Name}**\n";
            if (!string.IsNullOrEmpty(rec.Description))
            {
                response += $"   {rec.Description}\n";
            }
            response += $"   💡 Lý do: {rec.Reason}\n\n";
        }

        response += "Bạn có muốn biết thêm thông tin chi tiết về bất kỳ mục nào không?";
        
        return response;
    }

    public async Task<ChatHistoryDto> GetChatHistoryAsync(int userId, int sessionId)
    {
        try
        {
            // Get session with messages
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, includeMessages: true);

            if (session == null)
            {
                throw new InvalidOperationException($"Chat session {sessionId} not found");
            }

            // Verify session belongs to the user
            if (session.StudentId != userId)
            {
                throw new UnauthorizedAccessException("You do not have access to this chat session");
            }

            // Map to ChatHistoryDto
            return new ChatHistoryDto
            {
                SessionId = session.Id,
                CreatedAt = session.CreatedAt,
                LastMessageAt = session.LastMessageAt,
                Messages = session.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<List<ChatSessionSummaryDto>> GetUserSessionsAsync(int userId)
    {
        try
        {
            // Get all sessions for the user
            var sessions = await _chatSessionRepository.GetSessionsByStudentIdAsync(userId);

            // Map to ChatSessionSummaryDto
            return sessions.Select(s => new ChatSessionSummaryDto
            {
                SessionId = s.Id,
                CreatedAt = s.CreatedAt,
                LastMessageAt = s.LastMessageAt,
                MessageCount = s.Messages?.Count ?? 0,
                LastMessage = s.Messages?
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault()?.Content
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ChatSession> CreateNewSessionAsync(int studentId)
    {
        try
        {
            // Note: Parameter is named studentId but actually receives userId
            // ChatSession.StudentId field stores userId (naming is misleading for historical reasons)
            var session = new ChatSession
            {
                StudentId = studentId, // This is actually userId
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdSession = await _chatSessionRepository.CreateAsync(session);
            _logger.LogInformation("Created new chat session {SessionId} for user {UserId}", 
                createdSession.Id, studentId);

            return createdSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new session for user {UserId}", studentId);
            throw;
        }
    }
}

/// <summary>
/// Enum representing different chat intents
/// </summary>
internal enum ChatIntent
{
    GeneralConversation,
    ClubRecommendation,
    ActivityRecommendation
}
