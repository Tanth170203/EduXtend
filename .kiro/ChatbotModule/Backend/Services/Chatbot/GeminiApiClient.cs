using BusinessObject.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Chatbot;

public class GeminiApiClient : IGeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiAIOptions _options;
    private readonly ILogger<GeminiApiClient> _logger;

    // Retry configuration for exponential backoff
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays = new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    };

    public GeminiApiClient(
        HttpClient httpClient,
        IOptions<GeminiAIOptions> options,
        ILogger<GeminiApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        var request = new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = prompt }
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = _options.Temperature,
                MaxOutputTokens = _options.MaxTokens
            }
        };

        return await SendRequestWithRetryAsync(request, cancellationToken);
    }

    public async Task<string> GenerateContentWithHistoryAsync(
        List<ChatMessage> history,
        string newMessage,
        Student? studentContext = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newMessage))
        {
            throw new ArgumentException("Message cannot be empty", nameof(newMessage));
        }

        var contents = new List<GeminiContent>();

        // Add system prompt with student context if available
        if (studentContext != null)
        {
            var systemPrompt = BuildSystemPrompt(studentContext);
            _logger.LogInformation("System prompt created for student: {StudentCode}", studentContext.StudentCode);
            _logger.LogDebug("System prompt content: {Prompt}", systemPrompt);
            
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = systemPrompt }
                }
            });
            
            // Add acknowledgment from model
            contents.Add(new GeminiContent
            {
                Role = "model",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = "Tôi đã hiểu thông tin của bạn. Tôi sẽ trả lời dựa trên profile và sở thích của bạn." }
                }
            });
        }

        // Add history messages
        if (history != null && history.Any())
        {
            foreach (var message in history.OrderBy(m => m.CreatedAt))
            {
                contents.Add(new GeminiContent
                {
                    Role = message.Role == "user" ? "user" : "model",
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = message.Content }
                    }
                });
            }
        }

        // Add new message
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart>
            {
                new GeminiPart { Text = newMessage }
            }
        });

        var request = new GeminiRequest
        {
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = _options.Temperature,
                MaxOutputTokens = _options.MaxTokens
            }
        };

        return await SendRequestWithRetryAsync(request, cancellationToken);
    }

    private string BuildSystemPrompt(Student student)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("=== THÔNG TIN QUAN TRỌNG - ĐỌC KỸ ===");
        prompt.AppendLine();
        prompt.AppendLine("Bạn đang nói chuyện với sinh viên sau đây:");
        prompt.AppendLine();
        prompt.AppendLine($"TÊN: {student.User?.FullName ?? "N/A"}");
        prompt.AppendLine($"MÃ SINH VIÊN: {student.StudentCode}");
        prompt.AppendLine($"KHÓA: {student.Cohort}");
        prompt.AppendLine($"CHUYÊN NGÀNH: {student.Major?.Name ?? "N/A"} ({student.Major?.Code ?? "N/A"})");
        prompt.AppendLine($"GIỚI TÍNH: {student.Gender}");
        
        if (student.DateOfBirth != DateTime.MinValue)
        {
            var age = DateTime.Now.Year - student.DateOfBirth.Year;
            prompt.AppendLine($"TUỔI: {age}");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("=== QUY TẮC QUAN TRỌNG ===");
        prompt.AppendLine("1. Khi sinh viên hỏi 'tôi là ai', 'thông tin của tôi', hãy trả lời CHÍNH XÁC thông tin ở trên");
        prompt.AppendLine("2. KHÔNG được nhầm lẫn với sinh viên khác");
        prompt.AppendLine("3. Luôn dựa vào thông tin profile ở trên khi trả lời");
        prompt.AppendLine("4. Khi đề xuất câu lạc bộ/hoạt động, xem xét chuyên ngành và sở thích");
        prompt.AppendLine();
        prompt.AppendLine("Bạn là AI Assistant của EduXtend - hệ thống quản lý câu lạc bộ và hoạt động ngoại khóa.");
        
        return prompt.ToString();
    }

    public async Task<bool> ValidateApiKeyAsync()
    {
        try
        {
            _logger.LogInformation("Validating Gemini API key...");

            // Send a simple test request
            var testPrompt = "Hello";
            await GenerateContentAsync(testPrompt, CancellationToken.None);

            _logger.LogInformation("Gemini API key validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini API key validation failed: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<string> SendRequestWithRetryAsync(
        GeminiRequest request,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                return await SendRequestAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex) when (IsTransientError(ex))
            {
                lastException = ex;
                _logger.LogWarning(
                    "Gemini API request failed (attempt {Attempt}/{MaxAttempts}): {Message}",
                    attempt + 1,
                    MaxRetryAttempts,
                    ex.Message);

                if (attempt < MaxRetryAttempts - 1)
                {
                    var delay = RetryDelays[attempt];
                    _logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
            {
                // Timeout occurred (not user cancellation)
                _logger.LogError("Gemini API request timed out after 30 seconds");
                throw new TimeoutException("Yêu cầu mất quá nhiều thời gian. Vui lòng thử lại với câu hỏi ngắn gọn hơn.", ex);
            }
        }

        _logger.LogError(lastException, "Gemini API request failed after {MaxAttempts} attempts", MaxRetryAttempts);
        throw new InvalidOperationException(
            "Chatbot tạm thời không khả dụng, vui lòng thử lại sau",
            lastException);
    }

    private async Task<string> SendRequestAsync(
        GeminiRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured");
        }

        var url = $"{_options.ApiBaseUrl}/models/{_options.ModelName}:generateContent?key={_options.ApiKey}";

        var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending request to Gemini API: {Url}", url.Replace(_options.ApiKey, "***"));

        var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini API returned error {StatusCode}: {Error}",
                response.StatusCode,
                errorContent);

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new UnauthorizedAccessException("Invalid Gemini API key"),
                HttpStatusCode.TooManyRequests => new InvalidOperationException("Rate limit exceeded. Please try again later."),
                HttpStatusCode.BadRequest => new ArgumentException($"Invalid request: {errorContent}"),
                _ => new HttpRequestException($"Gemini API error: {response.StatusCode}")
            };
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
        {
            _logger.LogWarning("Gemini API returned no candidates");
            throw new InvalidOperationException("No response generated from Gemini AI");
        }

        var generatedText = geminiResponse.Candidates[0]?.Content?.Parts?[0]?.Text;

        if (string.IsNullOrWhiteSpace(generatedText))
        {
            _logger.LogWarning("Gemini API returned empty text");
            throw new InvalidOperationException("Empty response from Gemini AI");
        }

        return generatedText;
    }

    private static bool IsTransientError(HttpRequestException ex)
    {
        // Retry on network errors, server errors (5xx), and some 4xx errors
        if (ex.StatusCode.HasValue)
        {
            var statusCode = (int)ex.StatusCode.Value;
            return statusCode >= 500 || statusCode == 429; // Server errors or rate limit
        }

        // Retry on network-level errors
        return true;
    }

    #region Request/Response Models

    private class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        public double Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    #endregion
}
