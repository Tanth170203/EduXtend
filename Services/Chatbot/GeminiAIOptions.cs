namespace Services.Chatbot;

public class GeminiAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-pro";
    public string ApiBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
}
