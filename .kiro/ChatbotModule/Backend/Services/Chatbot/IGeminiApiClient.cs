using BusinessObject.Models;

namespace Services.Chatbot;

public interface IGeminiApiClient
{
    /// <summary>
    /// Generates content from Gemini AI based on a single prompt
    /// </summary>
    /// <param name="prompt">The prompt to send to Gemini AI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text response from Gemini AI</returns>
    Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content from Gemini AI with conversation history context
    /// </summary>
    /// <param name="history">List of previous chat messages for context</param>
    /// <param name="newMessage">The new message from the user</param>
    /// <param name="studentContext">Student information for personalized responses</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text response from Gemini AI</returns>
    Task<string> GenerateContentWithHistoryAsync(
        List<ChatMessage> history,
        string newMessage,
        Student? studentContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the Gemini API key is valid
    /// </summary>
    /// <returns>True if API key is valid, false otherwise</returns>
    Task<bool> ValidateApiKeyAsync();
}
