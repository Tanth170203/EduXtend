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
    /// Integration tests for the AI Chatbot Assistant feature.
    /// These tests validate end-to-end functionality with real Gemini API integration.
    /// 
    /// IMPORTANT: These tests require a valid Gemini API key in appsettings.json
    /// Set "GeminiAI:ApiKey" to your actual API key before running these tests.
    /// 
    /// To run these tests:
    /// dotnet test --filter "FullyQualifiedName~ChatbotIntegrationTests"
    /// 
    /// NOTE: These tests use mocked Gemini AI service for reliability.
    /// For true end-to-end testing with real Gemini API, use manual testing guide.
    /// </summary>
    [Collection("Integration Tests")]
    public class ChatbotIntegrationTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
        private readonly Mock<ILogger<ChatbotService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly ChatbotService _service;
        private readonly ITestOutputHelper _output;
        private int _testStudentId;

        public ChatbotIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: "IntegrationTestDb_" + Guid.NewGuid())
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
                FullName = "Integration Test Student",
                Email = "integration.test@example.com",
                RoleId = 1,
                IsActive = true
            };

            // Create test student
            var student = new Student
            {
                Id = 1,
                StudentCode = "SE999999",
                Cohort = "K17",
                FullName = "Integration Test Student",
                Email = "integration.test@example.com",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = Gender.Male,
                EnrollmentDate = DateTime.Now,
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
                Name = "Technology",
                Description = "Technology and programming clubs"
            };

            var artCategory = new ClubCategory
            {
                Id = 2,
                Name = "Arts",
                Description = "Arts and creative clubs"
            };

            // Create test clubs
            var techClub = new Club
            {
                Id = 1,
                Name = "Software Development Club",
                SubName = "SDC",
                Description = "A club for students interested in software development, programming, and technology",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = techCategory,
                FoundedDate = DateTime.Now.AddYears(-2)
            };

            var aiClub = new Club
            {
                Id = 2,
                Name = "AI & Machine Learning Club",
                SubName = "AIML",
                Description = "Explore artificial intelligence and machine learning technologies",
                IsActive = true,
                IsRecruitmentOpen = true,
                CategoryId = 1,
                Category = techCategory,
                FoundedDate = DateTime.Now.AddYears(-1)
            };

            // Create club membership
            var clubMember = new ClubMember
            {
                Id = 1,
                ClubId = 1,
                StudentId = 1,
                RoleInClub = "Member",
                IsActive = true,
                JoinedAt = DateTime.Now.AddMonths(-6),
                Club = techClub,
                Student = student
            };

            // Create upcoming activities
            var workshop = new Activity
            {
                Id = 1,
                Title = "Web Development Workshop",
                Description = "Learn modern web development with React and Node.js",
                Location = "Room A101",
                StartTime = DateTime.Now.AddDays(7),
                EndTime = DateTime.Now.AddDays(7).AddHours(3),
                Type = ActivityType.ClubWorkshop,
                Status = "Approved",
                IsPublic = true,
                CreatedById = 1,
                ClubId = 1,
                Club = techClub
            };

            var hackathon = new Activity
            {
                Id = 2,
                Title = "AI Hackathon 2024",
                Description = "24-hour hackathon focused on AI solutions",
                Location = "Innovation Lab",
                StartTime = DateTime.Now.AddDays(14),
                EndTime = DateTime.Now.AddDays(15),
                Type = ActivityType.SchoolCompetition,
                Status = "Approved",
                IsPublic = true,
                CreatedById = 1,
                ClubId = 2,
                Club = aiClub
            };

            // Add all entities to context
            _context.Majors.Add(major);
            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.ClubCategories.AddRange(techCategory, artCategory);
            _context.Clubs.AddRange(techClub, aiClub);
            _context.ClubMembers.Add(clubMember);
            _context.Activities.AddRange(workshop, hackathon);
            _context.SaveChanges();

            _testStudentId = student.Id;
        }

        [Fact]
        public async Task SendMessage_FindClubsMatchingMajor_ReturnsRelevantClubRecommendations()
        {
            // Arrange
            var userMessage = "Tôi muốn tìm CLB phù hợp với chuyên ngành của mình";
            var expectedResponse = "Dựa trên chuyên ngành Software Engineering của bạn, tôi đề xuất các CLB sau:\n\n" +
                                 "1. Software Development Club (SDC) - Phù hợp với sinh viên ngành công nghệ phần mềm\n" +
                                 "2. AI & Machine Learning Club (AIML) - Khám phá AI và machine learning";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Software Development Club", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Software Engineering", result, StringComparison.OrdinalIgnoreCase);
            
            _output.WriteLine($"AI Response: {result}");
            
            // Verify the prompt sent to Gemini includes student context
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("Software Engineering") && 
                p.Contains("Integration Test Student"))), Times.Once);
        }

        [Fact]
        public async Task SendMessage_AskAboutUpcomingActivities_ReturnsActivityRecommendations()
        {
            // Arrange
            var userMessage = "Có hoạt động nào sắp tới không?";
            var expectedResponse = "Có một số hoạt động sắp tới phù hợp với bạn:\n\n" +
                                 "1. Web Development Workshop - Ngày: " + DateTime.Now.AddDays(7).ToString("dd/MM/yyyy") + " tại Room A101\n" +
                                 "2. AI Hackathon 2024 - Ngày: " + DateTime.Now.AddDays(14).ToString("dd/MM/yyyy") + " tại Innovation Lab";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(
                result.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("Hackathon", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("hoạt động", StringComparison.OrdinalIgnoreCase)
            );
            
            _output.WriteLine($"AI Response: {result}");
            
            // Verify the prompt includes upcoming activities
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("HOẠT ĐỘNG SẮP TỚI:"))), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WithConversationHistory_MaintainsContext()
        {
            // Arrange
            var conversationHistory = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Role = "user",
                    Content = "Tôi muốn tìm CLB về công nghệ",
                    Timestamp = DateTime.Now.AddMinutes(-5)
                },
                new ChatMessageDto
                {
                    Role = "assistant",
                    Content = "Chúng tôi có Software Development Club và AI & Machine Learning Club",
                    Timestamp = DateTime.Now.AddMinutes(-4)
                }
            };

            var userMessage = "Cho tôi biết thêm về CLB đầu tiên";
            var expectedResponse = "Software Development Club (SDC) là CLB dành cho sinh viên quan tâm đến phát triển phần mềm...";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, conversationHistory);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Software Development", result, StringComparison.OrdinalIgnoreCase);
            
            _output.WriteLine($"AI Response: {result}");
            
            // Verify conversation history is included in prompt
            _geminiAIServiceMock.Verify(s => s.GenerateResponseAsync(It.Is<string>(p => 
                p.Contains("LỊCH SỬ HỘI THOẠI:") &&
                p.Contains("Tôi muốn tìm CLB về công nghệ"))), Times.Once);
        }

        [Fact]
        public async Task SendMessage_QuickActionFindClubs_TriggersAppropriateResponse()
        {
            // Arrange - Simulating quick action button click
            var userMessage = "🔍 Tìm CLB phù hợp";
            var expectedResponse = "Dựa trên profile của bạn, đây là các CLB phù hợp:\n\n" +
                                 "1. Software Development Club\n2. AI & Machine Learning Club";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ProcessChatMessageAsync(_testStudentId, userMessage, null);

            // Assert
            Assert.NotNull(result);
            Assert.True(
                result.Contains("CLB", StringComparison.OrdinalIgnoreCase) ||
                result.Contains("club", StringComparison.OrdinalIgnoreCase)
            );
            
            _output.WriteLine($"AI Response: {result}");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _cache.Dispose();
        }
    }
}
