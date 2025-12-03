using BusinessObject.DTOs.Chatbot;
using BusinessObject.Models;

namespace Services.Chatbot;

/// <summary>
/// Interface for the chatbot service that handles chat interactions and message processing
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Sends a message to the chatbot and receives a response
    /// </summary>
    /// <param name="userId">The ID of the user sending the message</param>
    /// <param name="request">The chat request containing the message and optional session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response with the assistant's reply and any recommendations</returns>
    Task<ChatResponseDto> SendMessageAsync(
        int userId,
        ChatRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the chat history for a specific session
    /// </summary>
    /// <param name="userId">The ID of the user requesting the history</param>
    /// <param name="sessionId">The ID of the chat session</param>
    /// <returns>Chat history with all messages in the session</returns>
    Task<ChatHistoryDto> GetChatHistoryAsync(int userId, int sessionId);

    /// <summary>
    /// Gets all chat sessions for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>List of chat session summaries</returns>
    Task<List<ChatSessionSummaryDto>> GetUserSessionsAsync(int userId);

    /// <summary>
    /// Creates a new chat session for a student
    /// </summary>
    /// <param name="studentId">The ID of the student</param>
    /// <returns>The newly created chat session</returns>
    Task<ChatSession> CreateNewSessionAsync(int studentId);
}
