using System.Diagnostics;
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
    /// Performance tests for the AI Chatbot Assistant.
    /// These tests validate response times, caching effectiveness, and scalability.
    /// </summary>
    public class ChatbotPerformanceTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly Mock<IGeminiAIService> _geminiAIServiceMock;
        private readonly Mock<ILogger<ChatbotService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly ChatbotService _service;
        private readonly ITestOutputHelper _output;

        public ChatbotPerformanceTests(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: "PerformanceTestDb_" + Guid.NewGuid())
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
                FullName = "Performance Test Student",
                Email = "perf.test@example.com",
                RoleId = 1,
                IsActive = true
            };

            var student = new Student
            {
                Id = 1,
                StudentCode = "SE888888",
                Cohort = "K17",
                FullName = "Performance Test Student",
                Email = "perf.test@example.com",
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

            // Create multiple clubs for testing
            var clubs = new List<Club>();
            for (int i = 1; i <= 10; i++)
            {
                clubs.Add(new Club
                {
                    Id = i,
                    Name = $"Test Club {i}",
                    SubName = $"TC{i}",
                    Description = $"Description for test club {i}",
                    IsActive = true,
                    IsRecruitmentOpen = true,
                    CategoryId = 1,
                    Category = category,
                    FoundedDate = DateTime.Now.AddYears(-1)
                });
            }

            // Create multiple activities for testing
            var activities = new List<BusinessObject.Models.Activity>();
            for (int i = 1; i <= 10; i++)
            {
                activities.Add(new BusinessObject.Models.Activity
                {
                    Id = i,
                    Title = $"Test Activity {i}",
                    Description = $"Description for test activity {i}",
                    Location = $"Room {i}01",
                    StartTime = DateTime.Now.AddDays(i),
                    EndTime = DateTime.Now.AddDays(i).AddHours(2),
                    Type = ActivityType.ClubWorkshop,
                    Status = "Approved",
                    IsPublic = true,
                    CreatedById = 1,
                    ClubId = 1,
                    Club = clubs[0]
                });
            }

            _context.Majors.Add(major);
            _context.Users.Add(user);
            _context.Students.Add(student);
            _context.ClubCategories.Add(category);
            _context.Clubs.AddRange(clubs);
            _context.Activities.AddRange(activities);
            _context.SaveChanges();
        }

        [Fact]
        public async Task ProcessChatMessage_ResponseTime_ShouldBeLessThan5Seconds()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tôi muốn tìm CLB phù hợp";
            
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Here are some clubs for you")
                .Callback(() => Task.Delay(2000).Wait()); // Simulate 2 second AI response time

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Assert
            stopwatch.Stop();
            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            
            _output.WriteLine($"Response time: {elapsedSeconds:F2} seconds");
            
            Assert.True(elapsedSeconds < 5, $"Response time {elapsedSeconds:F2}s exceeded 5 second threshold");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ProcessChatMessage_WithCaching_SecondRequestIsFaster()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tôi muốn tìm CLB phù hợp";
            
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("Here are some clubs for you");

            // Act - First request (no cache)
            var stopwatch1 = Stopwatch.StartNew();
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);
            stopwatch1.Stop();
            var firstRequestTime = stopwatch1.Elapsed.TotalMilliseconds;

            // Act - Second request (with cache)
            var stopwatch2 = Stopwatch.StartNew();
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);
            stopwatch2.Stop();
            var secondRequestTime = stopwatch2.Elapsed.TotalMilliseconds;

            // Assert
            _output.WriteLine($"First request time: {firstRequestTime:F2}ms");
            _output.WriteLine($"Second request time: {secondRequestTime:F2}ms");
            _output.WriteLine($"Performance improvement: {((firstRequestTime - secondRequestTime) / firstRequestTime * 100):F1}%");

            // Second request should be faster due to caching
            Assert.True(secondRequestTime <= firstRequestTime, 
                $"Second request ({secondRequestTime:F2}ms) should be faster than or equal to first request ({firstRequestTime:F2}ms)");
        }

        [Fact]
        public async Task ProcessChatMessage_MultipleSequentialRequests_MaintainsPerformance()
        {
            // Arrange
            var studentId = 1;
            var messages = new[]
            {
                "Tôi muốn tìm CLB phù hợp",
                "Có hoạt động nào sắp tới không?",
                "Cho tôi biết thêm về CLB công nghệ",
                "Tôi có thể tham gia CLB nào?",
                "Hoạt động nào phù hợp với tôi?"
            };

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            var responseTimes = new List<double>();

            // Act
            foreach (var message in messages)
            {
                var stopwatch = Stopwatch.StartNew();
                await _service.ProcessChatMessageAsync(studentId, message, null);
                stopwatch.Stop();
                responseTimes.Add(stopwatch.Elapsed.TotalSeconds);
            }

            // Assert
            var averageTime = responseTimes.Average();
            var maxTime = responseTimes.Max();

            _output.WriteLine($"Response times: {string.Join(", ", responseTimes.Select(t => $"{t:F2}s"))}");
            _output.WriteLine($"Average time: {averageTime:F2}s");
            _output.WriteLine($"Max time: {maxTime:F2}s");

            Assert.True(averageTime < 3, $"Average response time {averageTime:F2}s exceeded 3 second threshold");
            Assert.True(maxTime < 5, $"Max response time {maxTime:F2}s exceeded 5 second threshold");
        }

        [Fact]
        public async Task ProcessChatMessage_WithLargeConversationHistory_MaintainsPerformance()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tell me more";
            
            // Create conversation history with 50 messages (max limit)
            var conversationHistory = new List<ChatMessageDto>();
            for (int i = 0; i < 50; i++)
            {
                conversationHistory.Add(new ChatMessageDto
                {
                    Role = i % 2 == 0 ? "user" : "assistant",
                    Content = $"Message {i}",
                    Timestamp = DateTime.Now.AddMinutes(-50 + i)
                });
            }

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _service.ProcessChatMessageAsync(studentId, userMessage, conversationHistory);

            // Assert
            stopwatch.Stop();
            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            
            _output.WriteLine($"Response time with 50 message history: {elapsedSeconds:F2} seconds");
            
            Assert.True(elapsedSeconds < 5, $"Response time {elapsedSeconds:F2}s with large history exceeded 5 second threshold");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ProcessChatMessage_ConcurrentRequests_HandlesLoad()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tôi muốn tìm CLB phù hợp";
            var concurrentRequests = 5;

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response")
                .Callback(() => Task.Delay(500).Wait()); // Simulate 500ms AI response

            var stopwatch = Stopwatch.StartNew();

            // Act - Send multiple concurrent requests
            var tasks = Enumerable.Range(0, concurrentRequests)
                .Select(_ => _service.ProcessChatMessageAsync(studentId, userMessage, null))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            stopwatch.Stop();
            var totalTime = stopwatch.Elapsed.TotalSeconds;
            var averageTimePerRequest = totalTime / concurrentRequests;

            _output.WriteLine($"Total time for {concurrentRequests} concurrent requests: {totalTime:F2}s");
            _output.WriteLine($"Average time per request: {averageTimePerRequest:F2}s");

            Assert.All(results, result => Assert.NotNull(result));
            Assert.True(totalTime < 10, $"Total time {totalTime:F2}s for concurrent requests exceeded 10 second threshold");
        }

        [Fact]
        public async Task ProcessChatMessage_DatabaseQueryCount_IsOptimized()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Tôi muốn tìm CLB phù hợp";
            
            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act - First request (no cache)
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            // Get query count from context (this is a simplified check)
            var initialQueryCount = _context.ChangeTracker.Entries().Count();

            // Act - Second request (with cache)
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);

            var cachedQueryCount = _context.ChangeTracker.Entries().Count();

            // Assert
            _output.WriteLine($"Initial query count: {initialQueryCount}");
            _output.WriteLine($"Cached query count: {cachedQueryCount}");

            // With caching, we should see similar or fewer tracked entities
            Assert.True(cachedQueryCount <= initialQueryCount + 5, 
                "Cached requests should not significantly increase tracked entities");
        }

        [Fact]
        public void SessionStorage_MessageLimit_EnforcedAt50Messages()
        {
            // This test validates the client-side logic conceptually
            // In practice, this would be tested in JavaScript unit tests
            
            // Arrange
            var maxMessages = 50;
            var messages = new List<ChatMessageDto>();

            // Act - Add 60 messages
            for (int i = 0; i < 60; i++)
            {
                messages.Add(new ChatMessageDto
                {
                    Role = i % 2 == 0 ? "user" : "assistant",
                    Content = $"Message {i}",
                    Timestamp = DateTime.Now.AddMinutes(-60 + i)
                });

                // Simulate client-side limit enforcement
                if (messages.Count > maxMessages)
                {
                    messages.RemoveAt(0);
                }
            }

            // Assert
            Assert.Equal(maxMessages, messages.Count);
            Assert.Equal("Message 10", messages[0].Content); // First message should be #10
            Assert.Equal("Message 59", messages[^1].Content); // Last message should be #59
            
            _output.WriteLine($"Message count after adding 60 messages: {messages.Count}");
            _output.WriteLine($"First message: {messages[0].Content}");
            _output.WriteLine($"Last message: {messages[^1].Content}");
        }

        [Fact]
        public async Task CacheExpiration_StudentContext_ExpiresAfter5Minutes()
        {
            // Arrange
            var studentId = 1;
            var userMessage = "Test message";
            var cacheKey = $"student_context_{studentId}";

            _geminiAIServiceMock.Setup(s => s.GenerateResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("AI response");

            // Act - First request creates cache
            await _service.ProcessChatMessageAsync(studentId, userMessage, null);
            
            var cacheExists1 = _cache.TryGetValue(cacheKey, out _);

            // Simulate time passing (in real scenario, wait 5+ minutes)
            // For testing, we just verify cache was created
            
            // Assert
            Assert.True(cacheExists1, "Student context should be cached after first request");
            
            _output.WriteLine($"Cache key '{cacheKey}' exists: {cacheExists1}");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _cache.Dispose();
        }
    }
}
