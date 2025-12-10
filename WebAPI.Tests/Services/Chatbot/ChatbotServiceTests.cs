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

namespace WebAPI.Tests.Services.Chatbot
{
    public class ChatbotServiceTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
        private readonly Mock<ILogger<ChatbotService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly ChatbotService _service;

        public ChatbotServiceTests()
        {
            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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
                FullName = "Test Student",
                Email = "student1@example.com",
                RoleId = 1,
                IsActive = true
            };

            var student = new Student
            {
                Id = 1,
                StudentCode = "SE123456",
                Cohort = "K17",
                FullName = "Test Student",
                Email = "student1@example.com",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                EnrollmentDate = DateTime.Now,
                Status = StudentStatus.Active,
                UserId = 1,
                MajorId = 1,
                Major = major,
                User = user
            };

            var category = new ClubCategory
            {
                Id = 1,
                Name = "Technology",
                Description = "Tech clubs"
            };

            var club = new Club
            {
                Id = 1,
                Name = "Tech Club",
                SubName = "TC",
                Description = "A technology club",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = category,
                FoundedDate = DateTime.Now
            };

            var clubMember = new ClubMember
            {
                Id = 1,
                ClubId = 1,
                StudentId = 1,
                RoleInClub = "Member",
                IsActive = true,
                JoinedAt = DateTime.Now,
                Club = club,
                Student = student
            };

            var activity = new Activity
            {
                Id = 1,
                Title = "Tech Workshop",
                Description = "A workshop about technology",
                Location = "Room 101",
                StartTime = DateTime.Now.AddDays(7),
                EndTime = DateTime.Now.AddDays(7).AddHours(2),
                Type = ActivityType.ClubWorkshop,
                Status = "Approved",
                IsPublic = true,
                CreatedById = 1,
                ClubId = 1,
                Club = club
            };

            _context.Majors.Add(major);
            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.ClubCategories.Add(category);
            _context.Clubs.Add(club);
            _context.ClubMembers.Add(clubMember);
            _context.Activities.Add(activity);
            _context.SaveChanges();
        }

        [Fact]
        public async Task BuildStudentContextAsync_WithValidStudentId_ReturnsCorrectContext()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tell me about clubs";
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Here are some clubs for you");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("Test Student") && 
                p.Contains("Software Engineering") && 
                p.Contains("K17"))), Times.Once);
        }

        [Fact]
        public async Task BuildStudentContextAsync_WithInvalidStudentId_ThrowsException()
        {
            // Arrange
            var invalidStudentId = 999;
            var userMessage = "Test message";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.ProcessChatMessageAsync(invalidStudentId, userMessage, null));
        }

        [Fact]
        public async Task GetRelevantClubsAsync_ReturnsClubsMatchingStudentInterests()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "What clubs can I join?";
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Here are some clubs");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("Tech Club") && 
                p.Contains("Technology"))), Times.Once);
        }

        [Fact]
        public async Task GetUpcomingActivitiesAsync_FiltersCorrectly()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "What activities are coming up?";
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Here are upcoming activities");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("Tech Workshop") && 
                p.Contains("Room 101"))), Times.Once);
        }

        [Fact]
        public async Task BuildAIPrompt_FormatsContextCorrectly()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Help me find clubs";
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("THÔNG TIN SINH VIÊN:") &&
                p.Contains("DANH SÁCH CLB ĐANG MỞ TUYỂN:") &&
                p.Contains("HOẠT ĐỘNG SẮP TỚI:") &&
                p.Contains("HƯỚNG DẪN:") &&
                p.Contains("CÂU HỎI CỦA SINH VIÊN:"))), Times.Once);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithEmptyMessage_HandlesGracefully()
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
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithConversationHistory_IncludesInPrompt()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tell me more";
            var conversationHistory = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Role = "user",
                    Content = "What clubs are available?",
                    Timestamp = DateTime.Now.AddMinutes(-5)
                },
                new ChatMessageDto
                {
                    Role = "assistant",
                    Content = "We have Tech Club and Art Club",
                    Timestamp = DateTime.Now.AddMinutes(-4)
                }
            };

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("More information about clubs");

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, conversationHistory);

            // Assert
            Assert.NotNull(result);
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("LỊCH SỬ HỘI THOẠI:") &&
                p.Contains("What clubs are available?") &&
                p.Contains("We have Tech Club and Art Club"))), Times.Once);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithMalformedJSON_FallsBackToPlainText()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ về công nghệ";
            var malformedJson = @"{
                ""message"": ""Dưới đây là các câu lạc bộ phù hợp"",
                ""recommendations"": [
                    {
                        ""id"": 1,
                        ""name"": ""Tech Club"",
                        ""type"": ""club""
                        // Missing closing brace and other fields
                ";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(malformedJson);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(malformedJson, result); // Should return original malformed JSON as plain text
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithInvalidJSONStructure_FallsBackToPlainText()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ";
            var invalidStructure = @"{
                ""message"": ""Test message"",
                ""recommendations"": ""not an array""
            }";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(invalidStructure);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(invalidStructure, result); // Should return original as plain text
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithEmptyRecommendationsArray_FallsBackToPlainText()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ";
            var emptyRecommendations = @"{
                ""message"": ""Không tìm thấy câu lạc bộ phù hợp"",
                ""recommendations"": []
            }";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyRecommendations);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(emptyRecommendations, result); // Should return original as plain text
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithNullRecommendations_FallsBackToPlainText()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ";
            var nullRecommendations = @"{
                ""message"": ""Test message"",
                ""recommendations"": null
            }";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(nullRecommendations);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nullRecommendations, result); // Should return original as plain text
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithValidStructuredResponse_ReturnsJSON()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ về công nghệ";
            var validStructuredResponse = @"```json
{
    ""message"": ""Dưới đây là các câu lạc bộ phù hợp với bạn"",
    ""recommendations"": [
        {
            ""id"": 1,
            ""name"": ""Tech Club"",
            ""type"": ""club"",
            ""description"": ""Câu lạc bộ công nghệ"",
            ""reason"": ""Phù hợp với chuyên ngành Software Engineering"",
            ""relevanceScore"": 95
        }
    ]
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(validStructuredResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"hasRecommendations\":true", result);
            Assert.Contains("\"recommendations\":", result);
            Assert.Contains("Tech Club", result);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithPlainTextResponse_ReturnsPlainText()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Xin chào";
            var plainTextResponse = "Xin chào! Tôi có thể giúp gì cho bạn?";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(plainTextResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(plainTextResponse, result);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithInvalidRecommendationData_FiltersOutInvalid()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ";
            var mixedValidityResponse = @"```json
{
    ""message"": ""Các câu lạc bộ"",
    ""recommendations"": [
        {
            ""id"": 0,
            ""name"": ""Invalid Club"",
            ""type"": ""club"",
            ""description"": ""Invalid ID"",
            ""reason"": ""Test"",
            ""relevanceScore"": 90
        },
        {
            ""id"": 1,
            ""name"": """",
            ""type"": ""club"",
            ""description"": ""Empty name"",
            ""reason"": ""Test"",
            ""relevanceScore"": 85
        },
        {
            ""id"": 2,
            ""name"": ""Valid Club"",
            ""type"": ""club"",
            ""description"": ""Valid"",
            ""reason"": ""Good match"",
            ""relevanceScore"": 95
        }
    ]
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(mixedValidityResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            // Should only include the valid recommendation
            Assert.Contains("Valid Club", result);
            Assert.DoesNotContain("Invalid Club", result);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithEmptyResponse_FallsBackGracefully()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test";
            var emptyResponse = "";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ProcessChatMessageAsync_WithWhitespaceResponse_FallsBackGracefully()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tìm câu lạc bộ"; // Recommendation request to trigger parsing
            var whitespaceResponse = "   \n\t   ";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(whitespaceResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            // When parsing fails on whitespace, it should return empty string
            Assert.Equal(string.Empty, result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _cache.Dispose();
        }
    }
}
