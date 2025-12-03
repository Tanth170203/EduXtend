namespace Services.Chatbot
{
    public interface IGeminiAIService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}
