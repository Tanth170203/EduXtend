using BusinessObject.DTOs.Chatbot;

namespace Services.Recommendations;

/// <summary>
/// Interface for the recommendation engine that provides personalized club and activity recommendations
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Gets personalized club recommendations for a student based on their profile and preferences
    /// </summary>
    /// <param name="studentId">The ID of the student</param>
    /// <param name="userMessage">The user's message/query for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommended clubs with reasons and confidence scores</returns>
    Task<List<RecommendationDto>> GetClubRecommendationsAsync(
        int studentId,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets personalized activity recommendations for a student based on their profile and history
    /// </summary>
    /// <param name="studentId">The ID of the student</param>
    /// <param name="userMessage">The user's message/query for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recommended activities with reasons and confidence scores</returns>
    Task<List<RecommendationDto>> GetActivityRecommendationsAsync(
        int studentId,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a comprehensive context string about a student for use in AI prompts
    /// </summary>
    /// <param name="studentId">The ID of the student</param>
    /// <returns>Formatted context string in Vietnamese describing the student's profile and history</returns>
    Task<string> BuildStudentContextAsync(int studentId);
}
