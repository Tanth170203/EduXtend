namespace Services.Chatbot;

/// <summary>
/// Metrics tracking for chatbot operations
/// </summary>
public class ChatbotMetrics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitHits { get; set; }
    public int ClubRecommendations { get; set; }
    public int ActivityRecommendations { get; set; }
    public int GeneralConversations { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int GeminiApiErrors { get; set; }
    public int DatabaseErrors { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Service for tracking and reporting chatbot metrics
/// </summary>
public interface IChatbotMetricsService
{
    void RecordRequest(bool success, double responseTimeMs);
    void RecordRateLimitHit();
    void RecordIntent(string intent);
    void RecordGeminiApiError();
    void RecordDatabaseError();
    ChatbotMetrics GetMetrics();
    void ResetMetrics();
}

/// <summary>
/// In-memory implementation of chatbot metrics tracking
/// </summary>
public class ChatbotMetricsService : IChatbotMetricsService
{
    private readonly object _lock = new object();
    private ChatbotMetrics _metrics = new ChatbotMetrics
    {
        LastUpdated = DateTime.UtcNow
    };
    private readonly List<double> _responseTimes = new List<double>();

    public void RecordRequest(bool success, double responseTimeMs)
    {
        lock (_lock)
        {
            _metrics.TotalRequests++;
            if (success)
            {
                _metrics.SuccessfulRequests++;
            }
            else
            {
                _metrics.FailedRequests++;
            }

            _responseTimes.Add(responseTimeMs);
            if (_responseTimes.Count > 1000) // Keep last 1000 response times
            {
                _responseTimes.RemoveAt(0);
            }

            _metrics.AverageResponseTimeMs = _responseTimes.Average();
            _metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public void RecordRateLimitHit()
    {
        lock (_lock)
        {
            _metrics.RateLimitHits++;
            _metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public void RecordIntent(string intent)
    {
        lock (_lock)
        {
            switch (intent.ToLowerInvariant())
            {
                case "clubrecommendation":
                    _metrics.ClubRecommendations++;
                    break;
                case "activityrecommendation":
                    _metrics.ActivityRecommendations++;
                    break;
                case "generalconversation":
                    _metrics.GeneralConversations++;
                    break;
            }
            _metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public void RecordGeminiApiError()
    {
        lock (_lock)
        {
            _metrics.GeminiApiErrors++;
            _metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public void RecordDatabaseError()
    {
        lock (_lock)
        {
            _metrics.DatabaseErrors++;
            _metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public ChatbotMetrics GetMetrics()
    {
        lock (_lock)
        {
            return new ChatbotMetrics
            {
                TotalRequests = _metrics.TotalRequests,
                SuccessfulRequests = _metrics.SuccessfulRequests,
                FailedRequests = _metrics.FailedRequests,
                RateLimitHits = _metrics.RateLimitHits,
                ClubRecommendations = _metrics.ClubRecommendations,
                ActivityRecommendations = _metrics.ActivityRecommendations,
                GeneralConversations = _metrics.GeneralConversations,
                AverageResponseTimeMs = _metrics.AverageResponseTimeMs,
                GeminiApiErrors = _metrics.GeminiApiErrors,
                DatabaseErrors = _metrics.DatabaseErrors,
                LastUpdated = _metrics.LastUpdated
            };
        }
    }

    public void ResetMetrics()
    {
        lock (_lock)
        {
            _metrics = new ChatbotMetrics
            {
                LastUpdated = DateTime.UtcNow
            };
            _responseTimes.Clear();
        }
    }
}
