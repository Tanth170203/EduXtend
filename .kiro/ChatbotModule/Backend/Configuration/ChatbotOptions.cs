namespace WebAPI.Configuration;

public class ChatbotOptions
{
    public int RateLimitRequests { get; set; } = 15;
    public int RateLimitWindowSeconds { get; set; } = 60;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int RetryMaxAttempts { get; set; } = 3;
}
