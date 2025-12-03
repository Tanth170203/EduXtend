using System.Net;
using BusinessObject.DTOs.Chatbot;
using BusinessObject.Enum;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Chatbot;
using Xunit;
using Xunit.Abstractions;

namespace WebAPI.Tests.Integration
{
    /// <summary>
    /// Error handling tests for the AI Chatbot Assistant.
    /// These tests validate proper error handling for various failure scenarios.
    /// </summary>
    public class ChatbotErrorHandlingTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
        private readonly Mock<ILogger<ChatbotService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly ChatbotService _service;
        private readonly ITestOutputHelper _output;

        public ChatbotErrorHandlingTests(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: "ErrorTestDb_" + Guid.NewGuid())
                .Options;

            _context = new EduXtendContext(options);
            _geminiAIServiceMock = new Mock<IGeminiAIService>();
            _loggerMock = new Mock<ILogger<ChatbotService>>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _service = new ChatbotService(_context, _geminiAIServiceMock.Object, _loggerMock.Object, _cache);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var major = new Major
            {
                Id = 1,
                Code = "SE",
                Name = "Software Engineering",
                IsActive = true
            };

            var user = new User
            {
                Id = 1,
                FullName = "Error Test Student",
                Email = "error.test@example.com",
                RoleId = 1,
                IsActive = true
            };

            var student = new Student
            {
                Id = 1,
                StudentCode = "SE777777",
                Cohort = "K17",
                FullName = "Error Test Student",
                Email = "error.test@example.com",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                EnrollmentDate = DateTime.Now,
                Status = StudentStatus.Active,
                UserId = 1,
                MajorId = 1,
                Major = major,
                User = user
            };

            _context.Majors.Add(major);
            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.SaveChanges();
        }

        [Fact]
        public async Task ProcessChatMessage_WithInvalidApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid API key"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with user-friendly message
            Assert.Contains("Cấu hình AI Assistant không hợp lệ", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithNetworkError_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Network connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with user-friendly message
            Assert.Contains("Không thể kết nối đến AI Assistant", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithTimeout_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("Request timeout after 30 seconds"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with user-friendly message
            Assert.Contains("Không thể kết nối đến AI Assistant", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithInvalidStudentId_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidStudentId = 999;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(invalidStudentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with generic user-friendly message
            Assert.Contains("Đã xảy ra lỗi", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithDatabaseError_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            // Dispose the context to simulate database connection error
            _context.Dispose();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with generic user-friendly message
            Assert.Contains("Đã xảy ra lỗi", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithEmptyMessage_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var emptyMessage = "";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Please provide a message");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, emptyMessage, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Response for empty message: {result}");
        }

        [Fact]
        public async Task ProcessChatMessage_WithVeryLongMessage_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var longMessage = new string('a', 5000); // 5000 characters

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response to long message");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, longMessage, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Successfully handled message of length: {longMessage.Length}");
        }

        [Fact]
        public async Task ProcessChatMessage_WithSpecialCharacters_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var messageWithSpecialChars = "Test <script>alert('xss')</script> & special chars: @#$%^&*()";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, messageWithSpecialChars, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Successfully handled message with special characters");
        }

        [Fact]
        public async Task ProcessChatMessage_WithNullConversationHistory_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine("Successfully handled null conversation history");
        }

        [Fact]
        public async Task ProcessChatMessage_WithEmptyConversationHistory_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";
            var emptyHistory = new List<ChatMessageDto>();

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, emptyHistory);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine("Successfully handled empty conversation history");
        }

        [Fact]
        public async Task ProcessChatMessage_WithMalformedConversationHistory_HandlesGracefully()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";
            var malformedHistory = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Role = "invalid_role", // Invalid role
                    Content = "Test content",
                    Timestamp = DateTime.Now
                }
            };

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, malformedHistory);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine("Successfully handled malformed conversation history");
        }

        [Fact]
        public async Task ProcessChatMessage_WithGeminiQuotaExceeded_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Quota exceeded"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with generic user-friendly message
            Assert.Contains("Đã xảy ra lỗi", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithGeminiRateLimitError_ThrowsInvalidOperationException()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            var httpException = new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);
            
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(httpException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with user-friendly message
            Assert.Contains("Không thể kết nối đến AI Assistant", exception.Message);
        }

        [Fact]
        public async Task ProcessChatMessage_WithNoClubsAvailable_StillReturnsResponse()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tôi muốn tìm CLB phù hợp";

            // Remove all clubs from database
            _context.Clubs.RemoveRange(_context.Clubs);
            await _context.SaveChangesAsync();

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Currently there are no clubs available for recruitment");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Response when no clubs available: {result}");
        }

        [Fact]
        public async Task ProcessChatMessage_WithNoActivitiesAvailable_StillReturnsResponse()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Có hoạt động nào sắp tới không?";

            // Remove all activities from database
            _context.Activities.RemoveRange(_context.Activities);
            await _context.SaveChangesAsync();

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Currently there are no upcoming activities");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _output.WriteLine($"Response when no activities available: {result}");
        }

        [Fact]
        public async Task ProcessChatMessage_LogsErrors_WhenExceptionOccurs()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ProcessChatMessageAsync(studentId, userMessage, null));

            _output.WriteLine($"Exception message: {exception.Message}");
            // Service wraps exception with generic user-friendly message
            Assert.Contains("Đã xảy ra lỗi", exception.Message);

            // Verify logging occurred
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);

            _output.WriteLine("Error logging verified");
        }

        public void Dispose()
        {
            try
            {
                if (_context != null && !_context.Database.IsInMemory() || _context.Database.CanConnect())
                {
                    _context.Database.EnsureDeleted();
                }
            }
            catch (ObjectDisposedException)
            {
                // Context already disposed, ignore
            }
            _cache?.Dispose();
        }
    }
}
