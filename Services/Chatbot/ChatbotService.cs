using BusinessObject.DTOs.Chatbot;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Services.Chatbot.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Services.Chatbot
{
    public class ChatbotService : IChatbotService
    {
        private readonly EduXtendContext _context;
        private readonly IGeminiAIService _geminiAIService;
        private readonly ILogger<ChatbotService> _logger;
        private readonly IMemoryCache _cache;

        public ChatbotService(
            EduXtendContext context,
            IGeminiAIService geminiAIService,
            ILogger<ChatbotService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _geminiAIService = geminiAIService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> ProcessChatMessageAsync(
            int userId,
            string userMessage,
            List<ChatMessageDto>? conversationHistory)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("[{CorrelationId}] Processing chat message for user {UserId}", 
                correlationId, userId);

            try
            {
                // Build student context
                var studentContext = await BuildStudentContextAsync(userId);
                _logger.LogInformation("[{CorrelationId}] Built student context for {StudentName}", 
                    correlationId, studentContext.FullName);

                // Get relevant clubs
                var clubs = await GetRelevantClubsAsync(studentContext);
                _logger.LogInformation("[{CorrelationId}] Found {ClubCount} relevant clubs", 
                    correlationId, clubs.Count);

                // Get upcoming activities
                var activities = await GetUpcomingActivitiesAsync(studentContext);
                _logger.LogInformation("[{CorrelationId}] Found {ActivityCount} upcoming activities", 
                    correlationId, activities.Count);

                // Get recent news/posts
                var news = await GetRecentNewsAsync();
                _logger.LogInformation("[{CorrelationId}] Found {NewsCount} recent news posts", 
                    correlationId, news.Count);
                
                // Log news titles for debugging
                if (news.Any())
                {
                    _logger.LogDebug("[{CorrelationId}] News titles: {NewsTitles}", 
                        correlationId, 
                        string.Join("; ", news.Select(n => n.Title)));
                }

                // Detect if user is requesting recommendations
                bool isRecommendationRequest = IsRecommendationRequest(userMessage);
                _logger.LogInformation("[{CorrelationId}] Message type: {MessageType}", 
                    correlationId, isRecommendationRequest ? "Recommendation Request" : "General Query");

                // Build appropriate prompt based on request type
                string prompt;
                if (isRecommendationRequest)
                {
                    prompt = BuildStructuredPrompt(studentContext, clubs, activities, news, userMessage, conversationHistory);
                    _logger.LogDebug("[{CorrelationId}] Built structured prompt for recommendations (length: {Length})", 
                        correlationId, prompt.Length);
                }
                else
                {
                    prompt = BuildAIPrompt(studentContext, clubs, activities, news, userMessage, conversationHistory);
                    _logger.LogDebug("[{CorrelationId}] Built standard AI prompt (length: {Length})", 
                        correlationId, prompt.Length);
                }

                // Call Gemini AI
                var aiResponse = await _geminiAIService.GenerateResponseAsync(prompt);
                _logger.LogInformation("[{CorrelationId}] Received AI response for user {UserId}", 
                    correlationId, userId);

                // If this was a recommendation request, try to parse structured response
                if (isRecommendationRequest)
                {
                    var (isStructured, structuredData, plainText) = ParseStructuredResponse(aiResponse);
                    
                    if (isStructured && structuredData != null)
                    {
                        _logger.LogInformation(
                            "[{CorrelationId}] Successfully parsed structured response with {Count} recommendations", 
                            correlationId, 
                            structuredData.Recommendations.Count
                        );
                        
                        // Filter recommendations based on user intent
                        var filteredRecommendations = FilterRecommendationsByIntent(
                            structuredData.Recommendations, 
                            userMessage
                        );
                        
                        _logger.LogInformation(
                            "[{CorrelationId}] Filtered to {Count} recommendations based on user intent", 
                            correlationId, 
                            filteredRecommendations.Count
                        );
                        
                        // Validate that recommended IDs actually exist
                        var validatedRecommendations = await ValidateRecommendationIds(
                            filteredRecommendations,
                            clubs,
                            activities
                        );
                        
                        _logger.LogInformation(
                            "[{CorrelationId}] Validated to {Count} recommendations with existing IDs", 
                            correlationId, 
                            validatedRecommendations.Count
                        );
                        
                        // Check if this is a news request
                        bool isNewsRequest = IsNewsRequest(userMessage);
                        
                        // Handle news recommendations
                        if (isNewsRequest && structuredData.NewsRecommendations != null && structuredData.NewsRecommendations.Any())
                        {
                            _logger.LogInformation(
                                "[{CorrelationId}] Found {Count} news recommendations", 
                                correlationId, 
                                structuredData.NewsRecommendations.Count
                            );
                            
                            // Validate news IDs
                            var validatedNews = await ValidateNewsRecommendationIds(
                                structuredData.NewsRecommendations,
                                news
                            );
                            
                            _logger.LogInformation(
                                "[{CorrelationId}] Validated to {Count} news recommendations with existing IDs", 
                                correlationId, 
                                validatedNews.Count
                            );
                            
                            // Return structured response with news
                            var newsResponse = new
                            {
                                message = structuredData.Message,
                                hasRecommendations = true,
                                hasNewsRecommendations = true,
                                newsRecommendations = validatedNews.Select(n => new
                                {
                                    id = n.Id,
                                    title = n.Title,
                                    type = n.Type,
                                    summary = n.Summary,
                                    source = n.Source,
                                    category = n.Category,
                                    publishedAt = n.PublishedAt,
                                    reason = n.Reason,
                                    relevanceScore = n.RelevanceScore
                                }).ToList()
                            };
                            
                            return JsonSerializer.Serialize(newsResponse);
                        }
                        
                        // Return structured response as JSON for the controller to handle (clubs/activities)
                        var structuredResponse = new
                        {
                            message = structuredData.Message,
                            hasRecommendations = true,
                            recommendations = validatedRecommendations.Select(r => new
                            {
                                id = r.Id,
                                name = r.Name,
                                type = r.Type,
                                description = r.Description,
                                reason = r.Reason,
                                relevanceScore = r.RelevanceScore
                            }).ToList()
                        };
                        
                        return JsonSerializer.Serialize(structuredResponse);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[{CorrelationId}] Failed to parse structured response, falling back to plain text", 
                            correlationId
                        );
                        return plainText;
                    }
                }

                // Return plain text response for non-recommendation queries
                _logger.LogDebug("[{CorrelationId}] Returning plain text response", correlationId);
                return aiResponse;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException)
            {
                _logger.LogError(ex, "[{CorrelationId}] AI service authentication error for user {UserId}", 
                    correlationId, userId);
                throw new InvalidOperationException("Cấu hình AI Assistant không hợp lệ. Vui lòng liên hệ quản trị viên.", ex);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TimeoutException)
            {
                _logger.LogError(ex, "[{CorrelationId}] AI service network error for user {UserId}: {Message}", 
                    correlationId, userId, ex.Message);
                throw new InvalidOperationException("Không thể kết nối đến AI Assistant. Vui lòng thử lại sau.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Database error for user {UserId}", 
                    correlationId, userId);
                throw new InvalidOperationException("Lỗi truy cập dữ liệu. Vui lòng thử lại sau.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Unexpected error processing chat for user {UserId}: {Message}", 
                    correlationId, userId, ex.Message);
                throw new InvalidOperationException("Đã xảy ra lỗi. Vui lòng thử lại sau.", ex);
            }
        }

        /// <summary>
        /// Detects if the user message is requesting recommendations for clubs, activities, or news.
        /// Uses keyword matching to identify recommendation requests.
        /// </summary>
        /// <param name="userMessage">The user's message text</param>
        /// <returns>True if the message is requesting recommendations, false otherwise</returns>
        private bool IsRecommendationRequest(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return false;
            }

            var messageLower = userMessage.ToLower();

            // Keywords that indicate recommendation requests (Vietnamese + English)
            var recommendationKeywords = new[]
            {
                // Club/Activity recommendations
                "tìm", "tìm kiếm", "tìm câu lạc bộ", "tìm clb", "tìm hoạt động",
                "đề xuất", "đề nghị", "gợi ý", "giới thiệu",
                "câu lạc bộ nào", "clb nào", "hoạt động nào",
                "phù hợp", "phù hợp với tôi", "dành cho tôi",
                "nên tham gia", "có thể tham gia",
                "câu lạc bộ về", "clb về", "hoạt động về",
                "muốn tham gia", "quan tâm đến",
                // News recommendations
                "tin tức", "bài báo", "thông báo", "bài viết", "tin", "bài đăng",
                "tin tức về", "bài báo về", "thông báo về",
                "có tin", "có bài", "có thông báo",
                "tin tức nào", "bài báo nào", "thông báo nào",
                "tin tức mới", "bài báo mới", "thông báo mới",
                // English
                "recommend", "suggest", "find", "search",
                "what club", "which club", "what activity", "which activity",
                "club for", "activity for", "clubs for", "activities for",
                "can i join", "should i join", "want to join",
                "suitable club", "suitable activity",
                "club about", "activity about",
                "interested in club", "interested in activity",
                "looking for club", "looking for activity",
                "show me club", "show me activity",
                "news", "post", "article", "announcement",
                "news about", "post about", "article about",
                "any news", "any post", "latest news", "recent news"
            };

            // Check if any recommendation keyword is present
            return recommendationKeywords.Any(keyword => messageLower.Contains(keyword));
        }

        /// <summary>
        /// Detects if the user message is specifically requesting news/posts
        /// </summary>
        private bool IsNewsRequest(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return false;
            }

            var messageLower = userMessage.ToLower();

            var newsKeywords = new[]
            {
                // Vietnamese
                "tin tức", "bài báo", "thông báo", "bài viết", "tin", "bài đăng", "post",
                "tin tức về", "bài báo về", "thông báo về",
                "có tin", "có bài", "có thông báo",
                "tin tức nào", "bài báo nào", "thông báo nào",
                "tin tức mới", "bài báo mới", "thông báo mới",
                "tin gần đây", "bài gần đây",
                // English
                "news", "post", "article", "announcement", "update",
                "news about", "post about", "article about",
                "any news", "any post", "latest news", "recent news",
                "show me news", "show me post"
            };

            return newsKeywords.Any(keyword => messageLower.Contains(keyword));
        }

        /// <summary>
        /// Validate that recommended IDs actually exist in the database
        /// </summary>
        private async Task<List<RecommendationItem>> ValidateRecommendationIds(
            List<RecommendationItem> recommendations,
            List<ClubRecommendation> availableClubs,
            List<ActivityRecommendation> availableActivities)
        {
            var validated = new List<RecommendationItem>();

            foreach (var rec in recommendations)
            {
                if (rec.Type.ToLower() == "club")
                {
                    // Check if club ID exists in available clubs
                    var clubExists = availableClubs.Any(c => c.ClubId == rec.Id);
                    if (clubExists)
                    {
                        validated.Add(rec);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "AI recommended non-existent club ID {ClubId} ({ClubName}). Skipping.",
                            rec.Id, rec.Name
                        );
                    }
                }
                else if (rec.Type.ToLower() == "activity")
                {
                    // Check if activity ID exists in available activities
                    var activityExists = availableActivities.Any(a => a.ActivityId == rec.Id);
                    if (activityExists)
                    {
                        validated.Add(rec);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "AI recommended non-existent activity ID {ActivityId} ({ActivityName}). Skipping.",
                            rec.Id, rec.Name
                        );
                    }
                }
            }

            return validated;
        }

        /// <summary>
        /// Validate that recommended news IDs actually exist in the database
        /// </summary>
        private async Task<List<NewsRecommendationItem>> ValidateNewsRecommendationIds(
            List<NewsRecommendationItem> recommendations,
            List<NewsRecommendation> availableNews)
        {
            var validated = new List<NewsRecommendationItem>();

            foreach (var rec in recommendations)
            {
                // Check if news ID exists in available news
                var newsExists = availableNews.Any(n => n.PostId == rec.Id);
                if (newsExists)
                {
                    validated.Add(rec);
                }
                else
                {
                    _logger.LogWarning(
                        "AI recommended non-existent news ID {NewsId} ({NewsTitle}). Skipping.",
                        rec.Id, rec.Title
                    );
                }
            }

            return validated;
        }

        /// <summary>
        /// Filter recommendations based on user intent (club vs activity)
        /// </summary>
        private List<RecommendationItem> FilterRecommendationsByIntent(
            List<RecommendationItem> recommendations, 
            string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || !recommendations.Any())
            {
                return recommendations;
            }

            var messageLower = userMessage.ToLower();

            // Keywords for club requests (Vietnamese + English)
            var clubKeywords = new[]
            {
                // Vietnamese
                "câu lạc bộ", "clb", "câu lac bộ", "cau lac bo",
                // English
                "club", "clubs"
            };

            // Keywords for activity requests (Vietnamese + English)
            var activityKeywords = new[]
            {
                // Vietnamese
                "hoạt động", "sự kiện", "hoat dong", "su kien",
                // English
                "activity", "activities", "event", "events"
            };

            // Check if user is asking specifically for clubs
            bool isClubRequest = clubKeywords.Any(keyword => messageLower.Contains(keyword));
            
            // Check if user is asking specifically for activities
            bool isActivityRequest = activityKeywords.Any(keyword => messageLower.Contains(keyword));

            // If asking for clubs specifically, filter out activities
            if (isClubRequest && !isActivityRequest)
            {
                var filtered = recommendations.Where(r => r.Type.ToLower() == "club").ToList();
                _logger.LogInformation(
                    "User asked for clubs specifically. Filtered {Original} recommendations to {Filtered} clubs",
                    recommendations.Count, filtered.Count
                );
                
                // Return exactly 3 clubs (or less if not enough available)
                return filtered.Take(3).ToList();
            }

            // If asking for activities specifically, filter out clubs
            if (isActivityRequest && !isClubRequest)
            {
                var filtered = recommendations.Where(r => r.Type.ToLower() == "activity").ToList();
                _logger.LogInformation(
                    "User asked for activities specifically. Filtered {Original} recommendations to {Filtered} activities",
                    recommendations.Count, filtered.Count
                );
                
                // Return exactly 3 activities (or less if not enough available)
                return filtered.Take(3).ToList();
            }

            // If both or neither, return all (but limit to 3)
            return recommendations.Take(3).ToList();
        }

        private async Task<StudentContext> BuildStudentContextAsync(int userId)
        {
            var cacheKey = $"student_context_{userId}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out StudentContext? cachedContext) && cachedContext != null)
            {
                _logger.LogInformation("Retrieved student context for user {UserId} ({StudentName}) from cache", 
                    userId, cachedContext.FullName);
                return cachedContext;
            }

            // If not in cache, fetch from database using UserId
            var student = await _context.Students
                .Include(s => s.Major)
                .Include(s => s.ClubMembers)
                    .ThenInclude(cm => cm.Club)
                        .ThenInclude(c => c.Category)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                _logger.LogError("Student with UserId {UserId} not found", userId);
                throw new InvalidOperationException("Không tìm thấy thông tin sinh viên.");
            }

            // Get current active clubs
            var currentClubs = student.ClubMembers
                .Where(cm => cm.IsActive)
                .Select(cm => cm.Club.Name)
                .ToList();

            // Get interests from club categories
            var interests = student.ClubMembers
                .Where(cm => cm.IsActive)
                .Select(cm => cm.Club.Category.Name)
                .Distinct()
                .ToList();

            var context = new StudentContext
            {
                StudentId = student.Id,
                FullName = student.FullName,
                MajorName = student.Major.Name,
                Cohort = student.Cohort,
                CurrentClubs = currentClubs,
                Interests = interests
            };

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, context, cacheOptions);
            _logger.LogInformation("Cached student context for user {UserId} ({StudentName}) for 5 minutes", 
                userId, student.FullName);

            return context;
        }

        private async Task<List<ClubRecommendation>> GetRelevantClubsAsync(StudentContext context)
        {
            var cacheKey = "active_clubs";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out List<ClubRecommendation>? cachedClubs) && cachedClubs != null)
            {
                _logger.LogDebug("Retrieved active clubs from cache");
                return cachedClubs;
            }

            // If not in cache, fetch from database
            var clubs = await _context.Clubs
                .Include(c => c.Category)
                .Where(c => c.IsActive && c.IsRecruitmentOpen)
                .Select(c => new ClubRecommendation
                {
                    ClubId = c.Id,
                    Name = c.Name,
                    SubName = c.SubName,
                    Description = c.Description ?? string.Empty,
                    CategoryName = c.Category.Name,
                    IsRecruitmentOpen = c.IsRecruitmentOpen
                })
                .Take(10) // Limit to top 10 clubs
                .ToListAsync();

            // Cache for 10 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, clubs, cacheOptions);
            _logger.LogDebug("Cached active clubs for 10 minutes");

            return clubs;
        }

        private async Task<List<ActivityRecommendation>> GetUpcomingActivitiesAsync(StudentContext context)
        {
            var cacheKey = "upcoming_activities";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out List<ActivityRecommendation>? cachedActivities) && cachedActivities != null)
            {
                _logger.LogDebug("Retrieved upcoming activities from cache");
                return cachedActivities;
            }

            // If not in cache, fetch from database
            var now = DateTime.Now;

            var activities = await _context.Activities
                .Include(a => a.Club)
                .Where(a => a.Status == "Approved" && a.StartTime > now)
                .OrderBy(a => a.StartTime)
                .Select(a => new ActivityRecommendation
                {
                    ActivityId = a.Id,
                    Title = a.Title,
                    Description = a.Description ?? string.Empty,
                    Location = a.Location ?? string.Empty,
                    StartTime = a.StartTime,
                    ClubName = a.Club != null ? a.Club.Name : "Toàn trường",
                    ActivityType = a.Type.ToString(),
                    IsPublic = a.IsPublic
                })
                .Take(10) // Limit to top 10 activities
                .ToListAsync();

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, activities, cacheOptions);
            _logger.LogDebug("Cached upcoming activities for 5 minutes");

            return activities;
        }

        private async Task<List<NewsRecommendation>> GetRecentNewsAsync()
        {
            var cacheKey = "recent_news";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out List<NewsRecommendation>? cachedNews) && cachedNews != null)
            {
                _logger.LogDebug("Retrieved recent news from cache");
                return cachedNews;
            }

            // Fetch from database - combine ClubNews and SystemNews
            var clubNews = await _context.ClubNews
                .Include(cn => cn.Club)
                .Include(cn => cn.CreatedBy)
                .Where(cn => cn.IsApproved)
                .OrderByDescending(cn => cn.PublishedAt)
                .Take(5)
                .Select(cn => new NewsRecommendation
                {
                    PostId = cn.Id,
                    Title = cn.Title,
                    Content = cn.Content ?? string.Empty,
                    ClubName = cn.Club != null ? cn.Club.Name : "CLB",
                    AuthorName = cn.CreatedBy != null ? cn.CreatedBy.FullName : "Admin",
                    CreatedAt = cn.PublishedAt,
                    Category = "Tin CLB"
                })
                .ToListAsync();

            var systemNews = await _context.SystemNews
                .Include(sn => sn.CreatedBy)
                .Where(sn => sn.IsActive)
                .OrderByDescending(sn => sn.PublishedAt)
                .Take(5)
                .Select(sn => new NewsRecommendation
                {
                    PostId = sn.Id,
                    Title = sn.Title,
                    Content = sn.Content ?? string.Empty,
                    ClubName = "Hệ thống",
                    AuthorName = sn.CreatedBy != null ? sn.CreatedBy.FullName : "Admin",
                    CreatedAt = sn.PublishedAt,
                    Category = "Thông báo"
                })
                .ToListAsync();

            // Combine and sort by date
            var allNews = clubNews.Concat(systemNews)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToList();

            // Cache for 10 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(cacheKey, allNews, cacheOptions);
            _logger.LogDebug("Cached {Count} recent news for 10 minutes", allNews.Count);

            return allNews;
        }

        private string BuildAIPrompt(
            StudentContext context,
            List<ClubRecommendation> clubs,
            List<ActivityRecommendation> activities,
            List<NewsRecommendation> news,
            string userMessage,
            List<ChatMessageDto>? conversationHistory)
        {
            var prompt = new StringBuilder();

            // IMPORTANT: Clear system prompt with student context
            prompt.AppendLine("=== THÔNG TIN QUAN TRỌNG - ĐỌC KỸ ===");
            prompt.AppendLine();
            prompt.AppendLine("=== VỀ EDUXTEND ===");
            prompt.AppendLine("EduXtend là hệ thống quản lý câu lạc bộ và hoạt động sinh viên tại trường Đại học FPT.");
            prompt.AppendLine("Chức năng chính:");
            prompt.AppendLine("- Quản lý thông tin các câu lạc bộ sinh viên");
            prompt.AppendLine("- Tổ chức và theo dõi các hoạt động, sự kiện");
            prompt.AppendLine("- Hỗ trợ sinh viên tìm kiếm và tham gia CLB phù hợp");
            prompt.AppendLine("- Quản lý thành viên, điểm danh, và báo cáo hoạt động");
            prompt.AppendLine("- Tích hợp thanh toán và quản lý tài chính CLB");
            prompt.AppendLine();
            prompt.AppendLine("Bạn đang nói chuyện với sinh viên sau đây:");
            prompt.AppendLine();
            prompt.AppendLine($"TÊN: {context.FullName}");
            prompt.AppendLine($"MÃ SINH VIÊN: {context.StudentId}");
            prompt.AppendLine($"KHÓA: {context.Cohort}");
            prompt.AppendLine($"CHUYÊN NGÀNH: {context.MajorName}");
            
            if (context.CurrentClubs.Any())
            {
                prompt.AppendLine($"CLB HIỆN TẠI: {string.Join(", ", context.CurrentClubs)}");
            }
            else
            {
                prompt.AppendLine("CLB HIỆN TẠI: Chưa tham gia CLB nào");
            }
            
            if (context.Interests.Any())
            {
                prompt.AppendLine($"SỞ THÍCH: {string.Join(", ", context.Interests)}");
            }
            
            prompt.AppendLine();
            prompt.AppendLine("=== QUY TẮC QUAN TRỌNG ===");
            prompt.AppendLine("1. Khi sinh viên hỏi 'tôi là ai', 'thông tin của tôi', hãy trả lời CHÍNH XÁC thông tin ở trên");
            prompt.AppendLine("2. KHÔNG được nhầm lẫn với sinh viên khác");
            prompt.AppendLine("3. Luôn dựa vào thông tin profile ở trên khi trả lời");
            prompt.AppendLine("4. Khi đề xuất câu lạc bộ/hoạt động, xem xét chuyên ngành và sở thích");
            prompt.AppendLine();
            prompt.AppendLine("Bạn là AI Assistant của EduXtend - hệ thống quản lý câu lạc bộ và hoạt động ngoại khóa.");
            prompt.AppendLine("Nhiệm vụ của bạn là hỗ trợ sinh viên tìm kiếm và tham gia các câu lạc bộ (CLB) và hoạt động phù hợp.");
            prompt.AppendLine();

            // Club list
            prompt.AppendLine("DANH SÁCH CLB ĐANG MỞ TUYỂN:");
            if (clubs.Any())
            {
                foreach (var club in clubs)
                {
                    prompt.AppendLine($"- ID: {club.ClubId} | {club.Name} ({club.SubName})");
                    prompt.AppendLine($"  Danh mục: {club.CategoryName}");
                    if (!string.IsNullOrWhiteSpace(club.Description))
                    {
                        prompt.AppendLine($"  Mô tả: {club.Description}");
                    }
                    prompt.AppendLine($"  Format đề xuất: [CLUB:{club.ClubId}:{club.Name}]");
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại không có CLB nào đang mở tuyển.");
                prompt.AppendLine();
            }

            // Activity list
            prompt.AppendLine("HOẠT ĐỘNG SẮP TỚI:");
            if (activities.Any())
            {
                foreach (var activity in activities.Take(5)) // Limit to 5 for prompt size
                {
                    prompt.AppendLine($"- ID: {activity.ActivityId} | {activity.Title}");
                    prompt.AppendLine($"  CLB: {activity.ClubName}");
                    prompt.AppendLine($"  Thời gian: {activity.StartTime:dd/MM/yyyy HH:mm}");
                    prompt.AppendLine($"  Địa điểm: {activity.Location}");
                    prompt.AppendLine($"  Loại: {activity.ActivityType}");
                    if (!string.IsNullOrWhiteSpace(activity.Description))
                    {
                        prompt.AppendLine($"  Mô tả: {activity.Description}");
                    }
                    prompt.AppendLine($"  Format đề xuất: [ACTIVITY:{activity.ActivityId}:{activity.Title}]");
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại không có hoạt động nào sắp diễn ra.");
                prompt.AppendLine();
            }

            // News/Posts list - Format clearly for better AI understanding
            prompt.AppendLine("=== TIN TỨC & BÀI VIẾT GẦN ĐÂY ===");
            prompt.AppendLine("(Đọc kỹ danh sách này khi sinh viên hỏi về tin tức/bài báo/thông báo)");
            prompt.AppendLine();
            if (news.Any())
            {
                int newsIndex = 1;
                foreach (var post in news.Take(10)) // Increase to 10 for better coverage
                {
                    prompt.AppendLine($"[TIN {newsIndex}]");
                    prompt.AppendLine($"Tiêu đề: {post.Title}");
                    prompt.AppendLine($"Nguồn: {post.ClubName}");
                    prompt.AppendLine($"Tác giả: {post.AuthorName}");
                    prompt.AppendLine($"Ngày đăng: {post.CreatedAt:dd/MM/yyyy}");
                    prompt.AppendLine($"Danh mục: {post.Category}");
                    if (!string.IsNullOrWhiteSpace(post.Content))
                    {
                        // Truncate content to 200 characters for better context
                        var content = post.Content.Length > 200 
                            ? post.Content.Substring(0, 200) + "..." 
                            : post.Content;
                        prompt.AppendLine($"Nội dung: {content}");
                    }
                    prompt.AppendLine();
                    newsIndex++;
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại chưa có tin tức nào.");
                prompt.AppendLine();
            }

            // Guidelines
            prompt.AppendLine("HƯỚNG DẪN:");
            prompt.AppendLine("1. ĐỊNH DẠNG TRẢ LỜI:");
            prompt.AppendLine("   - Trả lời bằng tiếng Việt, thân thiện và nhiệt tình");
            prompt.AppendLine("   - TUYỆT ĐỐI KHÔNG dùng markdown formatting như **, *, _, ##");
            prompt.AppendLine("   - Sử dụng emoji để làm nổi bật (📌, 🎯, 📅, 📍, 👥, ✨)");
            prompt.AppendLine("   - Trình bày thông tin dạng danh sách với emoji thay vì bullet points");
            prompt.AppendLine("   - Sử dụng line breaks để tách các phần thông tin");
            prompt.AppendLine();
            prompt.AppendLine("   VÍ DỤ ĐỊNH DẠNG TỐT:");
            prompt.AppendLine("   Chào bạn! Về hoạt động Basic Information:");
            prompt.AppendLine("   ");
            prompt.AppendLine("   📌 Tên hoạt động: Basic Information");
            prompt.AppendLine("   👥 CLB: FPT Code Club");
            prompt.AppendLine("   📅 Thời gian: 08/12/2025 20:19");
            prompt.AppendLine("   📍 Địa điểm: Basic Information");
            prompt.AppendLine("   🎯 Loại: ClubMeeting");
            prompt.AppendLine();
            prompt.AppendLine("   VÍ DỤ ĐỊNH DẠNG XẤU (TRÁNH):");
            prompt.AppendLine("   **Tên hoạt động:** Basic Information");
            prompt.AppendLine("   * CLB: FPT Code Club");
            prompt.AppendLine();
            prompt.AppendLine("2. QUAN TRỌNG - Khi sinh viên hỏi về TIN TỨC/BÀI BÁO/THÔNG BÁO:");
            prompt.AppendLine("   CÁC TỪ KHÓA CẦN NHẬN DIỆN:");
            prompt.AppendLine("   - Tiếng Việt: tin tức, bài báo, thông báo, bài viết, tin, bài đăng, post");
            prompt.AppendLine("   - Tiếng Anh: news, post, article, announcement, update");
            prompt.AppendLine("   - Câu hỏi mẫu: 'có tin tức gì?', 'có bài báo nào?', 'thông báo mới nhất?'");
            prompt.AppendLine();
            prompt.AppendLine("   CÁCH TÌM KIẾM TIN TỨC:");
            prompt.AppendLine("   a) Đọc kỹ danh sách \"TIN TỨC & BÀI VIẾT GẦN ĐÂY\" bên trên");
            prompt.AppendLine("   b) Tìm tin tức có từ khóa trong TIÊU ĐỀ hoặc NỘI DUNG");
            prompt.AppendLine("      Ví dụ: Nếu hỏi về 'khai giảng' → tìm tin có chứa 'khai giảng', 'khai giang', 'học kỳ'");
            prompt.AppendLine("      Ví dụ: Nếu hỏi về 'spring' → tìm tin có chứa 'spring', 'học kỳ spring'");
            prompt.AppendLine("   c) So khớp KHÔNG PHÂN BIỆT HOA THƯỜNG và CHỮ CÓ DẤU");
            prompt.AppendLine("   d) Nếu tìm thấy tin tức phù hợp:");
            prompt.AppendLine("      - Liệt kê TẤT CẢ các tin tức có liên quan");
            prompt.AppendLine("      - Hiển thị: Tiêu đề, Nguồn (CLB/Hệ thống), Ngày đăng");
            prompt.AppendLine("      - Tóm tắt nội dung chính");
            prompt.AppendLine("      - Sắp xếp theo ngày đăng (mới nhất trước)");
            prompt.AppendLine("      - Dùng emoji thay vì markdown");
            prompt.AppendLine("   e) Nếu KHÔNG tìm thấy tin tức phù hợp:");
            prompt.AppendLine("      - Nói rõ: 'Hiện tại không có tin tức về [từ khóa]'");
            prompt.AppendLine("      - Liệt kê các tin tức gần đây nhất để sinh viên tham khảo");
            prompt.AppendLine();
            prompt.AppendLine("3. Đề xuất CLB và hoạt động phù hợp với chuyên ngành và sở thích của sinh viên");
            prompt.AppendLine("4. Giải thích lý do tại sao CLB/hoạt động phù hợp");
            prompt.AppendLine("5. Cung cấp thông tin cụ thể: tên CLB, mô tả, thời gian hoạt động");
            prompt.AppendLine("6. Khuyến khích sinh viên tham gia và phát triển kỹ năng");
            prompt.AppendLine("7. Nếu không có thông tin phù hợp, gợi ý sinh viên khám phá các lựa chọn khác");
            prompt.AppendLine("8. Giữ câu trả lời ngắn gọn (dưới 500 từ)");
            prompt.AppendLine();
            prompt.AppendLine("ĐỊNH DẠNG ĐỀ XUẤT:");
            prompt.AppendLine("Khi đề xuất CLB hoặc hoạt động, sử dụng format sau để hệ thống có thể tạo link:");
            prompt.AppendLine("[CLUB:ID:Tên CLB] - để tạo link đến trang CLB");
            prompt.AppendLine("[ACTIVITY:ID:Tên hoạt động] - để tạo link đến trang hoạt động");
            prompt.AppendLine("Ví dụ: 'Tôi đề xuất bạn tham gia [CLUB:1:FPT Code Club] và hoạt động [ACTIVITY:5:Workshop React]'");
            prompt.AppendLine();

            // Conversation history
            if (conversationHistory != null && conversationHistory.Any())
            {
                prompt.AppendLine("LỊCH SỬ HỘI THOẠI:");
                var recentHistory = conversationHistory.TakeLast(10).ToList(); // Last 10 messages
                foreach (var message in recentHistory)
                {
                    var role = message.Role == "user" ? "Sinh viên" : "AI Assistant";
                    prompt.AppendLine($"{role}: {message.Content}");
                }
                prompt.AppendLine();
            }

            // User message
            prompt.AppendLine("CÂU HỎI CỦA SINH VIÊN:");
            prompt.AppendLine(userMessage);

            return prompt.ToString();
        }

        private string BuildStructuredPrompt(
            StudentContext context,
            List<ClubRecommendation> clubs,
            List<ActivityRecommendation> activities,
            List<NewsRecommendation> news,
            string userMessage,
            List<ChatMessageDto>? conversationHistory)
        {
            var prompt = new StringBuilder();

            // System instructions for structured output
            prompt.AppendLine("=== HƯỚNG DẪN QUAN TRỌNG - ĐỊNH DẠNG TRẢ LỜI ===");
            prompt.AppendLine();
            prompt.AppendLine("=== VỀ EDUXTEND ===");
            prompt.AppendLine("EduXtend là hệ thống quản lý câu lạc bộ và hoạt động sinh viên tại trường Đại học FPT.");
            prompt.AppendLine("Chức năng chính:");
            prompt.AppendLine("- Quản lý thông tin các câu lạc bộ sinh viên");
            prompt.AppendLine("- Tổ chức và theo dõi các hoạt động, sự kiện");
            prompt.AppendLine("- Hỗ trợ sinh viên tìm kiếm và tham gia CLB phù hợp với sở thích");
            prompt.AppendLine("- Quản lý thành viên, điểm danh, và báo cáo hoạt động");
            prompt.AppendLine("- Tích hợp thanh toán và quản lý tài chính CLB");
            prompt.AppendLine("- AI Assistant giúp tư vấn và gợi ý CLB/hoạt động phù hợp");
            prompt.AppendLine();
            prompt.AppendLine("BẠN LÀ TRỢ LÝ AI HỖ TRỢ SINH VIÊN TÌM CÂU LẠC BỘ VÀ HOẠT ĐỘNG TRÊN EDUXTEND.");
            prompt.AppendLine();
            prompt.AppendLine("=== QUY TẮC ĐỊNH DẠNG ===");
            prompt.AppendLine("- TUYỆT ĐỐI KHÔNG dùng markdown formatting như **, *, _, ## trong message");
            prompt.AppendLine("- Sử dụng emoji để làm nổi bật (📌, 🎯, 📅, 📍, 👥, ✨)");
            prompt.AppendLine("- Trình bày thông tin dạng danh sách với emoji");
            prompt.AppendLine("- Văn bản phải dễ đọc, thân thiện, hiện đại");
            prompt.AppendLine();
            prompt.AppendLine("QUAN TRỌNG: Khi đề xuất câu lạc bộ, hoạt động, hoặc tin tức, bạn PHẢI trả về JSON theo format sau:");
            prompt.AppendLine();
            prompt.AppendLine("FORMAT 1 - ĐỀ XUẤT CLB/HOẠT ĐỘNG:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"message\": \"Văn bản giới thiệu ngắn gọn bằng tiếng Việt\",");
            prompt.AppendLine("  \"recommendations\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"id\": 123,");
            prompt.AppendLine("      \"name\": \"Tên câu lạc bộ hoặc hoạt động\",");
            prompt.AppendLine("      \"type\": \"club\",");
            prompt.AppendLine("      \"description\": \"Mô tả ngắn gọn bằng tiếng Việt\",");
            prompt.AppendLine("      \"reason\": \"Lý do phù hợp với sinh viên này (dựa trên chuyên ngành, sở thích)\",");
            prompt.AppendLine("      \"relevanceScore\": 95");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("FORMAT 2 - ĐỀ XUẤT TIN TỨC:");
            prompt.AppendLine("```json");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"message\": \"Văn bản giới thiệu ngắn gọn\",");
            prompt.AppendLine("  \"newsRecommendations\": [");
            prompt.AppendLine("    {");
            prompt.AppendLine("      \"id\": 789,");
            prompt.AppendLine("      \"title\": \"Tiêu đề tin tức\",");
            prompt.AppendLine("      \"type\": \"club_news\",");
            prompt.AppendLine("      \"summary\": \"Tóm tắt nội dung tin tức\",");
            prompt.AppendLine("      \"source\": \"Tên CLB hoặc Hệ thống\",");
            prompt.AppendLine("      \"category\": \"Tin CLB hoặc Thông báo\",");
            prompt.AppendLine("      \"publishedAt\": \"2025-11-17T00:00:00Z\",");
            prompt.AppendLine("      \"reason\": \"Lý do tin tức này liên quan đến câu hỏi\",");
            prompt.AppendLine("      \"relevanceScore\": 90");
            prompt.AppendLine("    }");
            prompt.AppendLine("  ]");
            prompt.AppendLine("}");
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("CHÚ Ý:");
            prompt.AppendLine("- \"type\" phải là \"club\" hoặc \"activity\"");
            prompt.AppendLine("- \"id\" PHẢI là ID có thật từ danh sách bên dưới");
            prompt.AppendLine("- \"name\" PHẢI là tên chính xác từ danh sách bên dưới");
            prompt.AppendLine("- TUYỆT ĐỐI KHÔNG tự tạo ra CLB hoặc hoạt động không có trong danh sách");
            prompt.AppendLine("- \"relevanceScore\" là số từ 0-100 (phần trăm độ phù hợp)");
            prompt.AppendLine("- Tính relevanceScore dựa trên:");
            prompt.AppendLine("  + Chuyên ngành của sinh viên (40%)");
            prompt.AppendLine("  + Sở thích hiện tại (30%)");
            prompt.AppendLine("  + Câu lạc bộ đang tham gia (20%)");
            prompt.AppendLine("  + Nội dung câu hỏi (10%)");
            prompt.AppendLine("- Văn bản trả lời phải CÙNG NGÔN NGỮ với câu hỏi:");
            prompt.AppendLine("  + Nếu sinh viên hỏi bằng tiếng Việt → Trả lời bằng tiếng Việt");
            prompt.AppendLine("  + Nếu sinh viên hỏi bằng tiếng Anh → Trả lời bằng tiếng Anh");
            prompt.AppendLine("- Đề xuất TỐI ĐA 3 items (nếu có ít hơn 3 thì đề xuất ít hơn)");
            prompt.AppendLine("- Sắp xếp theo relevanceScore từ cao đến thấp");
            prompt.AppendLine();
            prompt.AppendLine("QUAN TRỌNG - PHÂN LOẠI ĐỀ XUẤT:");
            prompt.AppendLine("- Nếu sinh viên hỏi về CÂU LẠC BỘ/CLB/CLUB → CHỈ đề xuất type=\"club\"");
            prompt.AppendLine("- Nếu sinh viên hỏi về HOẠT ĐỘNG/ACTIVITY/SỰ KIỆN → CHỈ đề xuất type=\"activity\"");
            prompt.AppendLine("- KHÔNG trộn lẫn club và activity trong cùng một response");
            prompt.AppendLine("- Phân tích kỹ câu hỏi để xác định sinh viên muốn tìm gì");
            prompt.AppendLine();
            prompt.AppendLine("VÍ DỤ:");
            prompt.AppendLine("❌ SAI: Câu hỏi \"Tìm CLB về công nghệ\" → Trả về cả clubs VÀ activities");
            prompt.AppendLine("✅ ĐÚNG: Câu hỏi \"Tìm CLB về công nghệ\" → CHỈ trả về clubs (type=\"club\")");
            prompt.AppendLine("✅ ĐÚNG: Câu hỏi \"Có hoạt động nào sắp tới?\" → CHỈ trả về activities (type=\"activity\")");
            prompt.AppendLine("✅ ĐÚNG: Câu hỏi \"What club can I join?\" → CHỈ trả về clubs, message bằng tiếng Anh");
            prompt.AppendLine("✅ ĐÚNG: Câu hỏi \"Show me activities\" → CHỈ trả về activities, message bằng tiếng Anh");
            prompt.AppendLine();

            // Student context
            prompt.AppendLine("=== THÔNG TIN SINH VIÊN ===");
            prompt.AppendLine();
            prompt.AppendLine($"Họ tên: {context.FullName}");
            prompt.AppendLine($"Mã sinh viên: {context.StudentId}");
            prompt.AppendLine($"Chuyên ngành: {context.MajorName}");
            prompt.AppendLine($"Khóa: {context.Cohort}");

            if (context.CurrentClubs.Any())
            {
                prompt.AppendLine($"Câu lạc bộ hiện tại: {string.Join(", ", context.CurrentClubs)}");
            }
            else
            {
                prompt.AppendLine("Câu lạc bộ hiện tại: Chưa tham gia CLB nào");
            }

            if (context.Interests.Any())
            {
                prompt.AppendLine($"Sở thích/Lĩnh vực quan tâm: {string.Join(", ", context.Interests)}");
            }

            prompt.AppendLine();

            // Available clubs - Limit to top 15 to reduce prompt size
            prompt.AppendLine("=== CÁC CÂU LẠC BỘ ĐANG MỞ TUYỂN (TOP 15) ===");
            prompt.AppendLine();
            if (clubs.Any())
            {
                var topClubs = clubs.Take(15);
                foreach (var club in topClubs)
                {
                    prompt.AppendLine($"ID: {club.ClubId} | Tên: {club.Name} | Danh mục: {club.CategoryName}");
                    if (!string.IsNullOrWhiteSpace(club.Description))
                    {
                        // Truncate description to 150 characters
                        var desc = club.Description.Length > 150 
                            ? club.Description.Substring(0, 150) + "..." 
                            : club.Description;
                        prompt.AppendLine($"Mô tả: {desc}");
                    }
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại không có câu lạc bộ nào đang mở tuyển.");
                prompt.AppendLine();
            }

            // Available activities - Limit to top 10 and compress format
            prompt.AppendLine("=== HOẠT ĐỘNG SẮP TỚI (TOP 10) ===");
            prompt.AppendLine();
            if (activities.Any())
            {
                foreach (var activity in activities.Take(10))
                {
                    prompt.AppendLine($"ID: {activity.ActivityId} | {activity.Title} | CLB: {activity.ClubName}");
                    prompt.AppendLine($"Loại: {activity.ActivityType} | Thời gian: {activity.StartTime:dd/MM/yyyy} | Địa điểm: {activity.Location}");
                    if (!string.IsNullOrWhiteSpace(activity.Description))
                    {
                        // Truncate description to 100 characters
                        var desc = activity.Description.Length > 100 
                            ? activity.Description.Substring(0, 100) + "..." 
                            : activity.Description;
                        prompt.AppendLine($"Mô tả: {desc}");
                    }
                    prompt.AppendLine();
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại không có hoạt động nào sắp diễn ra.");
                prompt.AppendLine();
            }

            // Recent news/posts - Format clearly for better AI understanding
            prompt.AppendLine("=== TIN TỨC & BÀI VIẾT GẦN ĐÂY (TOP 10) ===");
            prompt.AppendLine("(Đọc kỹ danh sách này khi sinh viên hỏi về tin tức/bài báo/thông báo)");
            prompt.AppendLine();
            if (news.Any())
            {
                int newsIndex = 1;
                foreach (var post in news.Take(10)) // Increase to 10 for better coverage
                {
                    prompt.AppendLine($"[TIN {newsIndex}]");
                    prompt.AppendLine($"Tiêu đề: {post.Title}");
                    prompt.AppendLine($"Nguồn: {post.ClubName}");
                    prompt.AppendLine($"Tác giả: {post.AuthorName}");
                    prompt.AppendLine($"Ngày: {post.CreatedAt:dd/MM/yyyy}");
                    prompt.AppendLine($"Danh mục: {post.Category}");
                    if (!string.IsNullOrWhiteSpace(post.Content))
                    {
                        // Truncate content to 150 characters
                        var content = post.Content.Length > 150 
                            ? post.Content.Substring(0, 150) + "..." 
                            : post.Content;
                        prompt.AppendLine($"Nội dung: {content}");
                    }
                    prompt.AppendLine();
                    newsIndex++;
                }
            }
            else
            {
                prompt.AppendLine("Hiện tại chưa có tin tức nào.");
                prompt.AppendLine();
            }

            // Conversation history - Limit to last 3 messages to save tokens
            if (conversationHistory != null && conversationHistory.Any())
            {
                prompt.AppendLine("=== LỊCH SỬ HỘI THOẠI (3 TIN NHẮN GẦN NHẤT) ===");
                prompt.AppendLine();
                var recentHistory = conversationHistory.TakeLast(3).ToList();
                foreach (var message in recentHistory)
                {
                    var role = message.Role == "user" ? "SV" : "AI";
                    // Truncate long messages
                    var content = message.Content.Length > 200 
                        ? message.Content.Substring(0, 200) + "..." 
                        : message.Content;
                    prompt.AppendLine($"{role}: {content}");
                }
                prompt.AppendLine();
            }

            // User message
            prompt.AppendLine("=== CÂU HỎI CỦA SINH VIÊN ===");
            prompt.AppendLine();
            prompt.AppendLine(userMessage);
            prompt.AppendLine();

            // Final instructions
            prompt.AppendLine("=== HƯỚNG DẪN TRẢ LỜI ===");
            prompt.AppendLine();
            prompt.AppendLine("1. Phân tích câu hỏi của sinh viên:");
            prompt.AppendLine("   - Xác định sinh viên muốn tìm CÂU LẠC BỘ hay HOẠT ĐỘNG");
            prompt.AppendLine("   - Từ khóa CLB/câu lạc bộ/club → CHỈ đề xuất clubs");
            prompt.AppendLine("   - Từ khóa hoạt động/activity/sự kiện → CHỈ đề xuất activities");
            prompt.AppendLine();
            prompt.AppendLine("2. Nếu sinh viên hỏi về CÂU LẠC BỘ:");
            prompt.AppendLine("   - Kiểm tra danh sách \"CÁC CÂU LẠC BỘ ĐANG MỞ TUYỂN\" bên dưới");
            prompt.AppendLine("   - Nếu TÌM THẤY CLB phù hợp:");
            prompt.AppendLine("     + Trả về JSON với CHỈ type=\"club\"");
            prompt.AppendLine("     + Sử dụng ĐÚNG ID và tên từ danh sách");
            prompt.AppendLine("     + Chọn TỐI ĐA 3 CLB phù hợp nhất");
            prompt.AppendLine("     + TUYỆT ĐỐI KHÔNG bao gồm activities trong recommendations");
            prompt.AppendLine("   - Nếu KHÔNG TÌM THẤY CLB phù hợp:");
            prompt.AppendLine("     + KHÔNG trả về JSON");
            prompt.AppendLine("     + Trả lời bằng VĂN BẢN thông thường");
            prompt.AppendLine("     + Giải thích không tìm thấy CLB về [chủ đề]");
            prompt.AppendLine("     + Gợi ý sinh viên xem các CLB khác hoặc liên hệ admin");
            prompt.AppendLine("   - TUYỆT ĐỐI KHÔNG tự tạo ra CLB không có trong danh sách");
            prompt.AppendLine();
            prompt.AppendLine("3. Nếu sinh viên hỏi về HOẠT ĐỘNG:");
            prompt.AppendLine("   - Kiểm tra danh sách \"HOẠT ĐỘNG SẮP TỚI\" bên dưới");
            prompt.AppendLine("   - Nếu TÌM THẤY hoạt động phù hợp:");
            prompt.AppendLine("     + Trả về JSON với CHỈ type=\"activity\"");
            prompt.AppendLine("     + Sử dụng ĐÚNG ID và tên từ danh sách");
            prompt.AppendLine("     + Chọn TỐI ĐA 3 hoạt động phù hợp nhất");
            prompt.AppendLine("     + TUYỆT ĐỐI KHÔNG bao gồm clubs trong recommendations");
            prompt.AppendLine("   - Nếu KHÔNG TÌM THẤY hoạt động phù hợp:");
            prompt.AppendLine("     + KHÔNG trả về JSON");
            prompt.AppendLine("     + Trả lời bằng VĂN BẢN thông thường");
            prompt.AppendLine("     + Giải thích không có hoạt động về [chủ đề]");
            prompt.AppendLine("     + Gợi ý sinh viên theo dõi thông báo hoặc xem hoạt động khác");
            prompt.AppendLine("   - TUYỆT ĐỐI KHÔNG tự tạo ra hoạt động không có trong danh sách");
            prompt.AppendLine();
            prompt.AppendLine("4. Tính relevanceScore chính xác dựa trên profile sinh viên");
            prompt.AppendLine("5. Giải thích lý do phù hợp trong trường \"reason\"");
            prompt.AppendLine("6. Nếu sinh viên hỏi về EDUXTEND:");
            prompt.AppendLine("   - Giải thích EduXtend là hệ thống quản lý CLB và hoạt động sinh viên");
            prompt.AppendLine("   - Nêu các chức năng chính: quản lý CLB, tổ chức hoạt động, tìm kiếm CLB phù hợp");
            prompt.AppendLine("   - Nhấn mạnh AI Assistant giúp tư vấn và gợi ý CLB/hoạt động");
            prompt.AppendLine("   - Trả lời ngắn gọn, dễ hiểu");
            prompt.AppendLine();
            prompt.AppendLine("7. QUAN TRỌNG - Nếu sinh viên hỏi về TIN TỨC/BÀI VIẾT:");
            prompt.AppendLine("   CÁC TỪ KHÓA CẦN NHẬN DIỆN:");
            prompt.AppendLine("   - Tiếng Việt: tin tức, bài báo, thông báo, bài viết, tin, bài đăng, post");
            prompt.AppendLine("   - Tiếng Anh: news, post, article, announcement, update");
            prompt.AppendLine();
            prompt.AppendLine("   CÁCH TÌM KIẾM VÀ TRẢ LỜI:");
            prompt.AppendLine("   a) Đọc kỹ danh sách \"TIN TỨC & BÀI VIẾT GẦN ĐÂY\" bên trên");
            prompt.AppendLine("   b) Tìm tin tức có từ khóa trong TIÊU ĐỀ hoặc NỘI DUNG");
            prompt.AppendLine("      Ví dụ: 'khai giảng' → tìm tin có 'khai giảng', 'khai giang', 'học kỳ'");
            prompt.AppendLine("   c) So khớp KHÔNG PHÂN BIỆT HOA THƯỜNG");
            prompt.AppendLine("   d) Nếu TÌM THẤY tin tức phù hợp:");
            prompt.AppendLine("      - Trả về JSON với \"newsRecommendations\" (FORMAT 2 bên trên)");
            prompt.AppendLine("      - Chọn TỐI ĐA 3-5 tin tức phù hợp nhất");
            prompt.AppendLine("      - Sử dụng ĐÚNG ID từ danh sách [TIN 1], [TIN 2], ...");
            prompt.AppendLine("      - \"type\": \"club_news\" hoặc \"system_news\" (dựa vào Danh mục)");
            prompt.AppendLine("      - \"summary\": Tóm tắt nội dung (100-150 từ)");
            prompt.AppendLine("      - \"reason\": Giải thích tại sao tin này liên quan đến câu hỏi");
            prompt.AppendLine("      - \"relevanceScore\": Tính dựa trên độ khớp từ khóa và độ mới");
            prompt.AppendLine("      - Sắp xếp theo relevanceScore từ cao đến thấp");
            prompt.AppendLine("   e) Nếu KHÔNG tìm thấy tin phù hợp:");
            prompt.AppendLine("      - CÓ THỂ trả về JSON với 3-5 tin gần đây nhất");
            prompt.AppendLine("      - HOẶC trả lời bằng VĂN BẢN: 'Không tìm thấy tin về [từ khóa]'");
            prompt.AppendLine();
            prompt.AppendLine("8. Nếu câu hỏi không liên quan đến đề xuất, EduXtend, hoặc tin tức:");
            prompt.AppendLine("   - Trả lời bình thường bằng văn bản tiếng Việt");
            prompt.AppendLine("   - KHÔNG trả về JSON format");
            prompt.AppendLine("   - Hướng dẫn sinh viên về các chức năng của EduXtend nếu phù hợp");
            prompt.AppendLine();
            prompt.AppendLine("9. Luôn thân thiện, nhiệt tình và hữu ích");
            prompt.AppendLine();
            prompt.AppendLine("=== VÍ DỤ CỤ THỂ ===");
            prompt.AppendLine();
            prompt.AppendLine("VÍ DỤ 1 - TÌM THẤY CLB:");
            prompt.AppendLine("Câu hỏi: 'Tìm CLB về công nghệ'");
            prompt.AppendLine("Trả lời: ```json { \"message\": \"...\", \"recommendations\": [...] } ```");
            prompt.AppendLine();
            prompt.AppendLine("VÍ DỤ 2 - KHÔNG TÌM THẤY CLB:");
            prompt.AppendLine("Câu hỏi: 'Tìm CLB về nhảy'");
            prompt.AppendLine("Trả lời: Hiện tại tôi không tìm thấy câu lạc bộ về nhảy trong danh sách các CLB đang mở tuyển. Bạn có thể xem các CLB khác hoặc liên hệ với phòng Công tác sinh viên để biết thêm thông tin.");
            prompt.AppendLine("(KHÔNG có ```json, CHỈ văn bản thuần túy)");
            prompt.AppendLine();
            prompt.AppendLine("VÍ DỤ 3 - TÌM THẤY TIN TỨC:");
            prompt.AppendLine("Câu hỏi: 'Có tin tức về khai giảng không?'");
            prompt.AppendLine("Trả lời: ```json { \"message\": \"...\", \"newsRecommendations\": [...] } ```");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// Parses the AI response to extract structured recommendation data if present.
        /// Attempts to extract JSON from markdown code blocks and deserialize it.
        /// Implements comprehensive error handling with fallback to plain text.
        /// </summary>
        /// <param name="aiResponse">The raw response from the AI service</param>
        /// <returns>
        /// A tuple containing:
        /// - isStructured: true if the response contains valid structured data
        /// - structuredData: the parsed StructuredResponse object (null if parsing fails)
        /// - plainText: the original response text for fallback display
        /// </returns>
        private (bool isStructured, StructuredResponse? structuredData, string plainText) 
            ParseStructuredResponse(string aiResponse)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            // Validate input
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Received empty or null AI response, returning empty plain text", 
                    correlationId
                );
                return (false, null, string.Empty);
            }
            
            try
            {
                _logger.LogDebug(
                    "[{CorrelationId}] Attempting to parse structured response (length: {Length} chars)", 
                    correlationId, 
                    aiResponse.Length
                );

                // Try to extract JSON from markdown code blocks (```json...```)
                var jsonMatch = Regex.Match(
                    aiResponse, 
                    @"```json\s*(\{.*?\})\s*```", 
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                string jsonContent;
                if (jsonMatch.Success)
                {
                    jsonContent = jsonMatch.Groups[1].Value;
                    _logger.LogDebug(
                        "[{CorrelationId}] Extracted JSON from markdown code block (length: {Length} chars)", 
                        correlationId, 
                        jsonContent.Length
                    );
                }
                else
                {
                    // Try parsing the entire response as JSON
                    jsonContent = aiResponse.Trim();
                    _logger.LogDebug(
                        "[{CorrelationId}] No markdown code block found, attempting to parse entire response as JSON", 
                        correlationId
                    );
                }

                // Validate JSON content is not empty
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] Extracted JSON content is empty, falling back to plain text", 
                        correlationId
                    );
                    return (false, null, aiResponse);
                }

                // Try to deserialize as JSON with case-insensitive options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                StructuredResponse? structured = null;
                
                try
                {
                    structured = JsonSerializer.Deserialize<StructuredResponse>(jsonContent, options);
                }
                catch (JsonException jsonEx)
                {
                    // Log detailed JSON parsing error with original content
                    _logger.LogWarning(
                        jsonEx,
                        "[{CorrelationId}] JSON deserialization failed at line {LineNumber}, position {BytePosition}. " +
                        "Error: {ErrorMessage}. Original response (first 500 chars): {ResponsePreview}", 
                        correlationId,
                        jsonEx.LineNumber,
                        jsonEx.BytePositionInLine,
                        jsonEx.Message,
                        aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse
                    );
                    
                    // Re-throw to be caught by outer catch block
                    throw;
                }

                // Validate that the structure contains valid data
                if (structured == null)
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] Deserialized object is null, falling back to plain text. " +
                        "JSON content (first 200 chars): {JsonPreview}", 
                        correlationId,
                        jsonContent.Length > 200 ? jsonContent.Substring(0, 200) + "..." : jsonContent
                    );
                    return (false, null, aiResponse);
                }

                // Validate recommendations array
                if (structured.Recommendations == null)
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] Recommendations array is null, falling back to plain text. " +
                        "Message field: {Message}", 
                        correlationId,
                        structured.Message ?? "(null)"
                    );
                    return (false, null, aiResponse);
                }

                if (!structured.Recommendations.Any())
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] Recommendations array is empty, falling back to plain text. " +
                        "Message field: {Message}", 
                        correlationId,
                        structured.Message ?? "(null)"
                    );
                    return (false, null, aiResponse);
                }

                // Validate individual recommendations
                var validRecommendations = structured.Recommendations
                    .Where(r => r != null && 
                                r.Id > 0 && 
                                !string.IsNullOrWhiteSpace(r.Name) && 
                                !string.IsNullOrWhiteSpace(r.Type))
                    .ToList();

                if (!validRecommendations.Any())
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] No valid recommendations found after validation " +
                        "(total: {Total}, valid: {Valid}), falling back to plain text", 
                        correlationId,
                        structured.Recommendations.Count,
                        validRecommendations.Count
                    );
                    return (false, null, aiResponse);
                }

                // Update structured response with only valid recommendations
                structured.Recommendations = validRecommendations;

                _logger.LogInformation(
                    "[{CorrelationId}] Successfully parsed structured response with {Count} valid recommendations. " +
                    "Types: {Types}", 
                    correlationId, 
                    structured.Recommendations.Count,
                    string.Join(", ", structured.Recommendations.Select(r => $"{r.Type}:{r.Id}"))
                );
                
                return (true, structured, string.Empty);
            }
            catch (JsonException jsonEx)
            {
                // Detailed JSON parsing error logging
                _logger.LogWarning(
                    jsonEx,
                    "[{CorrelationId}] JSON parsing failed: {ErrorMessage}. " +
                    "Line: {LineNumber}, Position: {BytePosition}. " +
                    "Original response length: {Length} chars. " +
                    "Response preview (first 300 chars): {ResponsePreview}. " +
                    "Falling back to plain text.", 
                    correlationId, 
                    jsonEx.Message,
                    jsonEx.LineNumber ?? 0,
                    jsonEx.BytePositionInLine ?? 0,
                    aiResponse.Length,
                    aiResponse.Length > 300 ? aiResponse.Substring(0, 300) + "..." : aiResponse
                );
            }
            catch (ArgumentException argEx)
            {
                // Handle argument exceptions (e.g., invalid regex patterns)
                _logger.LogError(
                    argEx,
                    "[{CorrelationId}] Argument error during response parsing: {ErrorMessage}. " +
                    "Original response length: {Length} chars. " +
                    "Falling back to plain text.", 
                    correlationId, 
                    argEx.Message,
                    aiResponse.Length
                );
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors
                _logger.LogError(
                    ex,
                    "[{CorrelationId}] Unexpected error parsing structured response: {ErrorType} - {ErrorMessage}. " +
                    "Original response length: {Length} chars. " +
                    "Response preview (first 300 chars): {ResponsePreview}. " +
                    "Stack trace: {StackTrace}. " +
                    "Falling back to plain text.", 
                    correlationId, 
                    ex.GetType().Name,
                    ex.Message,
                    aiResponse.Length,
                    aiResponse.Length > 300 ? aiResponse.Substring(0, 300) + "..." : aiResponse,
                    ex.StackTrace
                );
            }

            // Fallback to plain text - always return the original response
            _logger.LogDebug(
                "[{CorrelationId}] Returning plain text response (length: {Length} chars)", 
                correlationId,
                aiResponse.Length
            );
            return (false, null, aiResponse);
        }

        private string FormatConversationHistory(List<ChatMessageDto> history)
        {
            if (history == null || !history.Any())
            {
                return string.Empty;
            }

            var formatted = new StringBuilder();
            var recentHistory = history.TakeLast(10).ToList(); // Last 10 messages

            foreach (var message in recentHistory)
            {
                var role = message.Role == "user" ? "Sinh viên" : "AI Assistant";
                formatted.AppendLine($"{role}: {message.Content}");
            }

            return formatted.ToString();
        }

        /// <summary>
        /// Invalidates the cached student context for a specific user.
        /// Should be called when student profile, major, or club memberships are updated.
        /// </summary>
        public void InvalidateStudentContext(int userId)
        {
            var cacheKey = $"student_context_{userId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated student context cache for user {UserId}", userId);
        }

        /// <summary>
        /// Invalidates the cached active clubs list.
        /// Should be called when clubs are created, updated, or recruitment status changes.
        /// </summary>
        public void InvalidateActiveClubs()
        {
            var cacheKey = "active_clubs";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated active clubs cache");
        }

        /// <summary>
        /// Invalidates the cached upcoming activities list.
        /// Should be called when activities are created, updated, or status changes.
        /// </summary>
        public void InvalidateUpcomingActivities()
        {
            var cacheKey = "upcoming_activities";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated upcoming activities cache");
        }

        /// <summary>
        /// Invalidates the cached recent news list.
        /// Should be called when news/posts are created, updated, or approval status changes.
        /// </summary>
        public void InvalidateRecentNews()
        {
            var cacheKey = "recent_news";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated recent news cache");
        }
    }
}
