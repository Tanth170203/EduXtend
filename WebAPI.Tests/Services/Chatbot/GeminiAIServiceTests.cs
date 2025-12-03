using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Services.Chatbot;
using Services.Chatbot.Models;
using Xunit;

namespace WebAPI.Tests.Services.Chatbot
{
    public class GeminiAIServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<GeminiAIService>> _loggerMock;
        private readonly GeminiAIOptions _options;
        private readonly GeminiAIService _service;

        public GeminiAIServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<GeminiAIService>>();
            _options = new GeminiAIOptions
            {
                ApiKey = "test-api-key",
                ModelName = "gemini-1.5-flash",
                Temperature = 0.7,
                MaxTokens = 1024,
                ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta"
            };

            var optionsMock = new Mock<IOptions<GeminiAIOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            _service = new GeminiAIService(_httpClientFactoryMock.Object, optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GenerateResponseAsync_WithValidPrompt_ReturnsExpectedResponse()
        {
            // Arrange
            var prompt = "Hello, how are you?";
            var expectedResponse = "I'm doing well, thank you!";
            var geminiResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = expectedResponse }
                            }
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(geminiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act
            var result = await _service.GenerateResponseAsync(prompt);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task GenerateResponseAsync_WithInvalidApiKey_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var prompt = "Test prompt";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("{\"error\": \"Invalid API key\"}")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GenerateResponseAsync(prompt));
        }

        [Fact]
        public async Task GenerateResponseAsync_WithNetworkError_RetriesAndThrowsHttpRequestException()
        {
            // Arrange
            var prompt = "Test prompt";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.GenerateResponseAsync(prompt));
        }

        [Fact]
        public async Task GenerateResponseAsync_With500Error_RetriesAndThrowsHttpRequestException()
        {
            // Arrange
            var prompt = "Test prompt";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("{\"error\": \"Server error\"}")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _service.GenerateResponseAsync(prompt));
        }

        [Fact]
        public async Task GenerateResponseAsync_WithTimeout_RetriesAndThrowsTimeoutException()
        {
            // Arrange
            var prompt = "Test prompt";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _service.GenerateResponseAsync(prompt));
        }

        [Fact]
        public async Task GenerateResponseAsync_WithValidResponse_ParsesCorrectly()
        {
            // Arrange
            var prompt = "Test prompt";
            var expectedText = "Parsed response text";
            var geminiResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = expectedText }
                            }
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(geminiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act
            var result = await _service.GenerateResponseAsync(prompt);

            // Assert
            Assert.Equal(expectedText, result);
        }

        [Fact]
        public async Task GenerateResponseAsync_WithMalformedJson_ThrowsInvalidOperationException()
        {
            // Arrange
            var prompt = "Test prompt";
            var malformedJson = "{invalid json}";

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(malformedJson)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("GeminiAI")).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateResponseAsync(prompt));
        }
    }
}
