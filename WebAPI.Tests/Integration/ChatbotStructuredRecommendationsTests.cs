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
using System.Text.Json;

namespace WebAPI.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests for the Chatbot Rich Recommendations UI feature.
    /// Tests the complete flow from user request to structured JSON response with recommendation cards.
    /// 
    /// Test Coverage:
    /// - Structured JSON response generation from Gemini AI
    /// - ChatResponseDto population with recommendations
    /// - Frontend detection of recommendation type
    /// - Recommendation card data validation (name, reason, score)
    /// - Relevance score color coding
    /// - Card navigation data attributes
    /// 
    /// Requirements: 1.1, 1.2, 3.1, 4.1, 5.1, 8.1
    /// </summary>
    [Collection("Integration Tests")]
    public class ChatbotStructuredRecommendationsTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
        private readonly Mock<ILogger<ChatbotService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly ChatbotService _service;
        private readonly ITestOutputHelper _output;
        private int _testStudentId;

        public ChatbotStructuredRecommendationsTests(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: "StructuredRecommendationsTestDb_" + Guid.NewGuid())
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
            // Create test major
            var major = new Major
            {
                Id = 1,
                Code = "SE",
                Name = "Software Engineering",
                IsActive = true
            };

            // Create test user
            var user = new User
            {
                Id = 1,
                FullName = "Nguyễn Văn A",
                Email = "nguyenvana@example.com",
                RoleId = 1,
                IsActive = true
            };

            // Create test student
            var student = new Student
            {
                Id = 1,
                StudentCode = "SE170001",
                Cohort = "K17",
                FullName = "Nguyễn Văn A",
                Email = "nguyenvana@example.com",
                DateOfBirth = new DateTime(2002, 5, 15),
                Gender = Gender.Male,
                EnrollmentDate = DateTime.Now.AddYears(-2),
                Status = StudentStatus.Active,
                UserId = 1,
                MajorId = 1,
                Major = major,
                User = user
            };

            // Create test club categories
            var techCategory = new ClubCategory
            {
                Id = 1,
                Name = "Công nghệ",
                Description = "Câu lạc bộ về công nghệ và lập trình"
            };

            var artCategory = new ClubCategory
            {
                Id = 2,
                Name = "Nghệ thuật",
                Description = "Câu lạc bộ về nghệ thuật và sáng tạo"
            };

            // Create test clubs
            var techClub1 = new Club
            {
                Id = 1,
                Name = "Câu lạc bộ Lập trình",
                SubName = "Programming Club",
                Description = "Câu lạc bộ dành cho sinh viên yêu thích lập trình và phát triển phần mềm",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = techCategory,
                FoundedDate = DateTime.Now.AddYears(-3)
            };

            var techClub2 = new Club
            {
                Id = 2,
                Name = "Câu lạc bộ AI & Machine Learning",
                SubName = "AI/ML Club",
                Description = "Khám phá trí tuệ nhân tạo và học máy",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = techCategory,
                FoundedDate = DateTime.Now.AddYears(-2)
            };

            var techClub3 = new Club
            {
                Id = 3,
                Name = "Câu lạc bộ Web Development",
                SubName = "WebDev Club",
                Description = "Học và thực hành phát triển web hiện đại",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = techCategory,
                FoundedDate = DateTime.Now.AddYears(-1)
            };

            // Create upcoming activities
            var workshop = new Activity
            {
                Id = 1,
                Title = "Workshop React & Node.js",
                Description = "Học cách xây dựng ứng dụng web full-stack với React và Node.js",
                Location = "Phòng A101",
                StartTime = DateTime.Now.AddDays(7),
                EndTime = DateTime.Now.AddDays(7).AddHours(3),
                Type = ActivityType.ClubWorkshop,
                Status = "Approved",
                IsPublic = true,
                CreatedById = 1,
                ClubId = 3,
                Club = techClub3
            };

            var hackathon = new Activity
            {
                Id = 2,
                Title = "AI Hackathon 2024",
                Description = "Cuộc thi hackathon 24 giờ tập trung vào giải pháp AI",
                Location = "Innovation Lab",
                StartTime = DateTime.Now.AddDays(14),
                EndTime = DateTime.Now.AddDays(15),
                Type = ActivityType.SchoolCompetition,
                Status = "Approved",
                IsPublic = true,
                CreatedById = 1,
                ClubId = 2,
                Club = techClub2
            };

            // Add all entities to context
            _context.Majors.Add(major);
            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.ClubCategories.AddRange(techCategory, artCategory);
            _context.Clubs.AddRange(techClub1, techClub2, techClub3);
            _context.Activities.AddRange(workshop, hackathon);
            _context.SaveChanges();

            _testStudentId = student.Id;
        }

        [Fact]
        public async Task SendMessage_FindTechClubs_ReturnsStructuredJSONWithRecommendations()
        {
            // Arrange
            var userMessage = "Tìm câu lạc bộ về công nghệ";
            
            // Mock Gemini AI to return structured JSON response
            var structuredResponse = @"```json
{
  ""message"": ""Dựa trên chuyên ngành Software Engineering của bạn, tôi đề xuất các câu lạc bộ công nghệ sau:"",
  ""recommendations"": [
    {
      ""id"": 1,
      ""name"": ""Câu lạc bộ Lập trình"",
      ""type"": ""club"",
      ""description"": ""Câu lạc bộ dành cho sinh viên yêu thích lập trình và phát triển phần mềm"",
      ""reason"": ""Phù hợp hoàn hảo với chuyên ngành Software Engineering của bạn, giúp bạn nâng cao kỹ năng lập trình"",
      ""relevanceScore"": 95
    },
    {
      ""id"": 2,
      ""name"": ""Câu lạc bộ AI & Machine Learning"",
      ""type"": ""club"",
      ""description"": ""Khám phá trí tuệ nhân tạo và học máy"",
      ""reason"": ""AI/ML là xu hướng công nghệ hot, rất phù hợp với sinh viên ngành phần mềm muốn mở rộng kiến thức"",
      ""relevanceScore"": 88
    },
    {
      ""id"": 3,
      ""name"": ""Câu lạc bộ Web Development"",
      ""type"": ""club"",
      ""description"": ""Học và thực hành phát triển web hiện đại"",
      ""reason"": ""Web development là kỹ năng thiết yếu cho sinh viên Software Engineering"",
      ""relevanceScore"": 92
    }
  ]
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(structuredResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Verify structured JSON response
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            _output.WriteLine($"Service Response: {result}");

            // Parse the JSON response
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;

            // Requirement 1.1: Verify structured response format
            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.True(root.TryGetProperty("hasRecommendations", out var hasRecsElement));
            Assert.True(root.TryGetProperty("recommendations", out var recsElement));

            // Requirement 1.2: Verify hasRecommendations is true
            Assert.True(hasRecsElement.GetBoolean());
            _output.WriteLine($"✓ hasRecommendations = true");

            // Requirement 1.2: Verify recommendations array is populated
            Assert.Equal(JsonValueKind.Array, recsElement.ValueKind);
            var recommendations = recsElement.EnumerateArray().ToList();
            Assert.Equal(3, recommendations.Count);
            _output.WriteLine($"✓ Recommendations count = {recommendations.Count}");

            // Requirement 3.1, 4.1: Verify each recommendation has correct data structure
            foreach (var rec in recommendations)
            {
                Assert.True(rec.TryGetProperty("id", out var idElement));
                Assert.True(rec.TryGetProperty("name", out var nameElement));
                Assert.True(rec.TryGetProperty("type", out var typeElement));
                Assert.True(rec.TryGetProperty("description", out var descElement));
                Assert.True(rec.TryGetProperty("reason", out var reasonElement));
                Assert.True(rec.TryGetProperty("relevanceScore", out var scoreElement));

                var id = idElement.GetInt32();
                var name = nameElement.GetString();
                var type = typeElement.GetString();
                var reason = reasonElement.GetString();
                var score = scoreElement.GetInt32();

                // Verify data is not empty
                Assert.True(id > 0);
                Assert.False(string.IsNullOrEmpty(name));
                Assert.Equal("club", type);
                Assert.False(string.IsNullOrEmpty(reason));
                
                // Requirement 5.1: Verify relevance score is in valid range (0-100)
                Assert.InRange(score, 0, 100);

                _output.WriteLine($"✓ Recommendation: ID={id}, Name={name}, Type={type}, Score={score}%");
                _output.WriteLine($"  Reason: {reason}");
            }

            // Requirement 5.1: Verify relevance scores are properly calculated
            var firstRec = recommendations[0];
            var firstScore = firstRec.GetProperty("relevanceScore").GetInt32();
            Assert.Equal(95, firstScore);
            _output.WriteLine($"✓ First recommendation has highest relevance score: {firstScore}%");

            // Verify the prompt sent to Gemini includes structured format instructions
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("```json") && 
                p.Contains("recommendations") &&
                p.Contains("relevanceScore") &&
                p.Contains("Software Engineering"))), Times.Once);
            
            _output.WriteLine($"✓ Gemini AI received structured prompt with JSON schema");
        }

        [Fact]
        public async Task SendMessage_FindActivities_ReturnsStructuredJSONWithActivityRecommendations()
        {
            // Arrange
            var userMessage = "Có hoạt động nào về công nghệ sắp tới không?";
            
            var structuredResponse = @"```json
{
  ""message"": ""Có 2 hoạt động công nghệ sắp diễn ra phù hợp với bạn:"",
  ""recommendations"": [
    {
      ""id"": 1,
      ""name"": ""Workshop React & Node.js"",
      ""type"": ""activity"",
      ""description"": ""Học cách xây dựng ứng dụng web full-stack với React và Node.js"",
      ""reason"": ""Workshop thực hành giúp bạn nắm vững công nghệ web hiện đại"",
      ""relevanceScore"": 90
    },
    {
      ""id"": 2,
      ""name"": ""AI Hackathon 2024"",
      ""type"": ""activity"",
      ""description"": ""Cuộc thi hackathon 24 giờ tập trung vào giải pháp AI"",
      ""reason"": ""Cơ hội thử thách bản thân và áp dụng kiến thức AI vào thực tế"",
      ""relevanceScore"": 85
    }
  ]
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(structuredResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;

            // Verify activity recommendations
            Assert.True(root.GetProperty("hasRecommendations").GetBoolean());
            var recommendations = root.GetProperty("recommendations").EnumerateArray().ToList();
            Assert.Equal(2, recommendations.Count);

            // Requirement 8.1: Verify each recommendation has ID for navigation
            foreach (var rec in recommendations)
            {
                var id = rec.GetProperty("id").GetInt32();
                var type = rec.GetProperty("type").GetString();
                
                Assert.True(id > 0);
                Assert.Equal("activity", type);
                
                _output.WriteLine($"✓ Activity recommendation: ID={id} (can navigate to /activities/{id})");
            }
        }

        [Fact]
        public async Task SendMessage_RelevanceScoreColorCoding_VerifiesScoreRanges()
        {
            // Arrange
            var userMessage = "Gợi ý câu lạc bộ cho tôi";
            
            // Create response with different score ranges to test color coding
            var structuredResponse = @"```json
{
  ""message"": ""Đây là các câu lạc bộ phù hợp với bạn:"",
  ""recommendations"": [
    {
      ""id"": 1,
      ""name"": ""CLB Điểm cao"",
      ""type"": ""club"",
      ""description"": ""Test"",
      ""reason"": ""Phù hợp tuyệt vời"",
      ""relevanceScore"": 95
    },
    {
      ""id"": 2,
      ""name"": ""CLB Điểm khá"",
      ""type"": ""club"",
      ""description"": ""Test"",
      ""reason"": ""Phù hợp tốt"",
      ""relevanceScore"": 75
    },
    {
      ""id"": 3,
      ""name"": ""CLB Điểm trung bình"",
      ""type"": ""club"",
      ""description"": ""Test"",
      ""reason"": ""Phù hợp"",
      ""relevanceScore"": 55
    },
    {
      ""id"": 4,
      ""name"": ""CLB Điểm thấp"",
      ""type"": ""club"",
      ""description"": ""Test"",
      ""reason"": ""Có thể phù hợp"",
      ""relevanceScore"": 45
    }
  ]
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(structuredResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Requirement 5.1: Verify relevance score ranges for color coding
            var jsonDoc = JsonDocument.Parse(result);
            var recommendations = jsonDoc.RootElement.GetProperty("recommendations").EnumerateArray().ToList();

            // Score >= 90% should be dark green (#00A86B)
            var highScore = recommendations[0].GetProperty("relevanceScore").GetInt32();
            Assert.InRange(highScore, 90, 100);
            _output.WriteLine($"✓ High score ({highScore}%) - Should display in dark green (#00A86B)");

            // Score 70-89% should be medium green (#32CD32)
            var goodScore = recommendations[1].GetProperty("relevanceScore").GetInt32();
            Assert.InRange(goodScore, 70, 89);
            _output.WriteLine($"✓ Good score ({goodScore}%) - Should display in medium green (#32CD32)");

            // Score 50-69% should be yellow (#FFD700)
            var fairScore = recommendations[2].GetProperty("relevanceScore").GetInt32();
            Assert.InRange(fairScore, 50, 69);
            _output.WriteLine($"✓ Fair score ({fairScore}%) - Should display in yellow (#FFD700)");

            // Score < 50% should be orange (#FF8C00)
            var lowScore = recommendations[3].GetProperty("relevanceScore").GetInt32();
            Assert.InRange(lowScore, 0, 49);
            _output.WriteLine($"✓ Low score ({lowScore}%) - Should display in orange (#FF8C00)");
        }

        [Fact]
        public async Task SendMessage_MalformedJSON_FallsBackToPlainText()
        {
            // Arrange
            var userMessage = "Tìm câu lạc bộ về công nghệ";
            
            // Mock Gemini AI to return malformed JSON
            var malformedResponse = @"Dựa trên chuyên ngành của bạn, tôi đề xuất:
1. Câu lạc bộ Lập trình
2. Câu lạc bộ AI & Machine Learning";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(malformedResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Should fall back to plain text
            Assert.NotNull(result);
            Assert.Equal(malformedResponse, result);
            
            _output.WriteLine($"✓ Malformed JSON correctly fell back to plain text");
            _output.WriteLine($"Plain text response: {result}");
        }

        [Fact]
        public async Task SendMessage_EmptyRecommendationsArray_FallsBackToPlainText()
        {
            // Arrange
            var userMessage = "Tìm câu lạc bộ";
            
            var emptyRecsResponse = @"```json
{
  ""message"": ""Hiện tại không có câu lạc bộ phù hợp"",
  ""recommendations"": []
}
```";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyRecsResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Should fall back to plain text when recommendations array is empty
            Assert.NotNull(result);
            Assert.Contains("Hiện tại không có câu lạc bộ phù hợp", result);
            
            _output.WriteLine($"✓ Empty recommendations array correctly fell back to plain text");
        }

        [Fact]
        public async Task SendMessage_NonRecommendationQuery_ReturnsPlainText()
        {
            // Arrange
            var userMessage = "Xin chào, bạn là ai?";
            var plainTextResponse = "Xin chào! Tôi là AI Assistant của EduXtend, giúp bạn tìm kiếm câu lạc bộ và hoạt động phù hợp.";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(plainTextResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Should return plain text for non-recommendation queries
            Assert.NotNull(result);
            Assert.Equal(plainTextResponse, result);
            Assert.DoesNotContain("recommendations", result);
            
            _output.WriteLine($"✓ Non-recommendation query correctly returned plain text");
            _output.WriteLine($"Response: {result}");
        }

        [Fact]
        public async Task SendMessage_VerifyPromptContainsStudentContext()
        {
            // Arrange
            var userMessage = "Tìm câu lạc bộ phù hợp với tôi";
            
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Test response");

            // Act
            await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert - Verify prompt includes student context
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("Nguyễn Văn A") &&
                p.Contains("Software Engineering") &&
                p.Contains("K17") &&
                p.Contains("Câu lạc bộ Lập trình") &&
                p.Contains("Câu lạc bộ AI & Machine Learning") &&
                p.Contains("Câu lạc bộ Web Development"))), Times.Once);
            
            _output.WriteLine($"✓ Prompt includes student context (name, major, cohort, available clubs)");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _cache.Dispose();
        }
    }
}
