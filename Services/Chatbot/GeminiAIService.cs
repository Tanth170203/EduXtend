using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Chatbot.Models;

namespace Services.Chatbot
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiAIOptions _options;
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiAIOptions> options,
            ILogger<GeminiAIService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("[{CorrelationId}] Starting Gemini AI request", correlationId);

            try
            {
                var response = await SendRequestWithRetryAsync(prompt, correlationId, maxRetries: 3);
                _logger.LogInformation("[{CorrelationId}] Gemini AI request completed successfully", correlationId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Gemini AI request failed: {Message}", correlationId, ex.Message);
                throw;
            }
        }

        private async Task<string> SendRequestWithRetryAsync(string prompt, string correlationId, int maxRetries = 3)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < maxRetries)
            {
                attempt++;
                _logger.LogInformation("[{CorrelationId}] Attempt {Attempt}/{MaxRetries}", correlationId, attempt, maxRetries);

                try
                {
                    var httpClient = _httpClientFactory.CreateClient("GeminiAI");
                    var request = BuildRequest(prompt);
                    var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogDebug("[{CorrelationId}] Request payload: {Request}", 
                        correlationId, 
                        requestJson.Length > 1000 ? requestJson.Substring(0, 1000) + "..." : requestJson);

                    var url = $"{_options.ApiBaseUrl}/models/{_options.ModelName}:generateContent?key={_options.ApiKey}";
                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("[{CorrelationId}] Response status: {StatusCode}, Content: {Content}", 
                        correlationId, 
                        response.StatusCode,
                        responseContent.Length > 1000 ? responseContent.Substring(0, 1000) + "..." : responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var aiResponse = ParseResponse(responseContent);
                        return aiResponse;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        if (attempt < maxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            _logger.LogWarning("[{CorrelationId}] Rate limited, retrying after {Delay}s", 
                                correlationId, delay.TotalSeconds);
                            await Task.Delay(delay);
                            continue;
                        }
                        throw new HttpRequestException("Gemini API rate limit exceeded");
                    }

                    _logger.LogError("[{CorrelationId}] Gemini API error: {StatusCode} - {Response}", 
                        correlationId, response.StatusCode, responseContent);
                    throw new HttpRequestException($"Gemini API request failed with status {response.StatusCode}");
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay);
                        continue;
                    }
                }
            }

            throw lastException ?? new Exception("Failed to get response from Gemini AI after multiple retries");
        }

        private GeminiRequest BuildRequest(string prompt)
        {
            return new GeminiRequest
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
        }

        private string ParseResponse(string jsonResponse)
        {
            try
            {
                _logger.LogDebug("Parsing Gemini response: {Response}", 
                    jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) + "..." : jsonResponse);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var response = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse, options);

                // Check if prompt was blocked
                if (response?.PromptFeedback?.BlockReason != null)
                {
                    _logger.LogWarning("Gemini blocked the prompt. Reason: {BlockReason}", 
                        response.PromptFeedback.BlockReason);
                    throw new InvalidOperationException(
                        $"Content was blocked by safety filters: {response.PromptFeedback.BlockReason}");
                }

                if (response?.Candidates == null || response.Candidates.Count == 0)
                {
                    _logger.LogError("No response candidates from Gemini AI. Response: {Response}", jsonResponse);
                    throw new InvalidOperationException("No response candidates from Gemini AI");
                }

                var candidate = response.Candidates[0];
                
                // Check finish reason
                if (candidate?.FinishReason != null && candidate.FinishReason != "STOP")
                {
                    _logger.LogWarning("Gemini response finished with reason: {FinishReason}", 
                        candidate.FinishReason);
                    
                    if (candidate.FinishReason == "SAFETY")
                    {
                        throw new InvalidOperationException("Response was blocked by safety filters");
                    }
                    
                    if (candidate.FinishReason == "MAX_TOKENS")
                    {
                        _logger.LogError("Response exceeded max tokens limit. Consider increasing MaxTokens in config or reducing prompt size.");
                        throw new InvalidOperationException("Response was too long and was cut off. Please try a shorter question.");
                    }
                }

                if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
                {
                    _logger.LogError("No content parts in Gemini response. Candidate: {Candidate}", 
                        JsonSerializer.Serialize(candidate));
                    
                    // If MAX_TOKENS caused empty parts, provide specific error
                    if (candidate?.FinishReason == "MAX_TOKENS")
                    {
                        throw new InvalidOperationException("Response was too long. Please increase MaxTokens in configuration or simplify your prompt.");
                    }
                    
                    throw new InvalidOperationException("No content parts in Gemini AI response");
                }

                var text = candidate.Content.Parts[0]?.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogError("Empty text in Gemini response. Response: {Response}", jsonResponse);
                    throw new InvalidOperationException("Empty response from Gemini AI");
                }

                return text;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", jsonResponse);
                throw new InvalidOperationException("Failed to parse Gemini AI response", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, "Index out of range parsing Gemini response: {Response}", jsonResponse);
                throw new InvalidOperationException("Invalid response structure from Gemini AI", ex);
            }
        }
    }
}
