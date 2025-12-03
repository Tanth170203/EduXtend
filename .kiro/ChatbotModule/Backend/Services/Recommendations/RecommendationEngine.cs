using BusinessObject.DTOs.Chatbot;
using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Activities;
using Repositories.ClubMembers;
using Repositories.Clubs;
using Repositories.Students;
using Services.Chatbot;
using System.Text;
using System.Text.Json;

namespace Services.Recommendations;

public class RecommendationEngine : IRecommendationEngine
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClubMemberRepository _clubMemberRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IClubRepository _clubRepository;
    private readonly IGeminiApiClient _geminiApiClient;
    private readonly ILogger<RecommendationEngine> _logger;

    public RecommendationEngine(
        IStudentRepository studentRepository,
        IClubMemberRepository clubMemberRepository,
        IActivityRepository activityRepository,
        IClubRepository clubRepository,
        IGeminiApiClient geminiApiClient,
        ILogger<RecommendationEngine> logger)
    {
        _studentRepository = studentRepository;
        _clubMemberRepository = clubMemberRepository;
        _activityRepository = activityRepository;
        _clubRepository = clubRepository;
        _geminiApiClient = geminiApiClient;
        _logger = logger;
    }

    public async Task<string> BuildStudentContextAsync(int studentId)
    {
        _logger.LogInformation("Building student context for StudentId: {StudentId}", studentId);

        // Get student with major information
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            _logger.LogWarning("Student not found: {StudentId}", studentId);
            return "Không tìm thấy thông tin sinh viên.";
        }

        var context = new StringBuilder();
        
        // Basic student information
        context.AppendLine($"- Tên: {student.FullName}");
        context.AppendLine($"- Mã sinh viên: {student.StudentCode}");
        context.AppendLine($"- Chuyên ngành: {student.Major?.Name ?? "Chưa xác định"}");
        context.AppendLine($"- Khóa: {student.Cohort}");

        // Get clubs the student has joined
        var clubMembers = await _clubMemberRepository.GetByStudentIdAsync(studentId);
        var clubMembersList = clubMembers.ToList();
        
        if (clubMembersList.Any())
        {
            var clubNames = clubMembersList
                .Where(cm => cm.IsActive)
                .Select(cm => $"{cm.Club?.Name ?? "Unknown"} ({cm.RoleInClub})")
                .ToList();
            
            if (clubNames.Any())
            {
                context.AppendLine($"- Các CLB đã tham gia: {string.Join(", ", clubNames)}");
            }
            else
            {
                context.AppendLine("- Các CLB đã tham gia: Chưa tham gia CLB nào");
            }
        }
        else
        {
            context.AppendLine("- Các CLB đã tham gia: Chưa tham gia CLB nào");
        }

        // Get activity registration and attendance history
        var userRegistrations = await _activityRepository.GetUserRegistrationsAsync(student.UserId);
        
        if (userRegistrations.Any())
        {
            var activityTypes = userRegistrations
                .Where(r => r.Activity != null)
                .GroupBy(r => r.Activity!.Type)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()} lần)")
                .ToList();
            
            if (activityTypes.Any())
            {
                context.AppendLine($"- Lịch sử tham gia hoạt động: {string.Join(", ", activityTypes)}");
            }
            else
            {
                context.AppendLine("- Lịch sử tham gia hoạt động: Chưa tham gia hoạt động nào");
            }

            // Analyze preferred activity types
            var preferredTypes = userRegistrations
                .Where(r => r.Activity != null && r.Status == "Attended")
                .GroupBy(r => r.Activity!.Type)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key.ToString())
                .ToList();
            
            if (preferredTypes.Any())
            {
                context.AppendLine($"- Loại hoạt động thường tham gia: {string.Join(", ", preferredTypes)}");
            }
        }
        else
        {
            context.AppendLine("- Lịch sử tham gia hoạt động: Chưa tham gia hoạt động nào");
        }

        _logger.LogDebug("Student context built successfully for StudentId: {StudentId}", studentId);
        return context.ToString();
    }

    public async Task<List<RecommendationDto>> GetClubRecommendationsAsync(
        int studentId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting club recommendations for StudentId: {StudentId}", studentId);

        try
        {
            // Get active clubs
            var activeClubs = await _clubRepository.SearchClubsAsync(null, null, true);
            
            if (!activeClubs.Any())
            {
                _logger.LogWarning("No active clubs found");
                return new List<RecommendationDto>();
            }

            // Build student context
            var studentContext = await BuildStudentContextAsync(studentId);

            // Build clubs list for prompt
            var clubsList = new StringBuilder();
            foreach (var club in activeClubs)
            {
                clubsList.AppendLine($"- ID: {club.Id}, Tên: {club.Name}, Mô tả: {club.Description ?? "Không có mô tả"}, Danh mục: {club.Category?.Name ?? "Không xác định"}");
            }

            // Create prompt template for club recommendation
            var prompt = $@"Bạn là trợ lý AI của hệ thống EduXtend, giúp sinh viên tìm câu lạc bộ phù hợp.

THÔNG TIN SINH VIÊN:
{studentContext}

DANH SÁCH CÂU LẠC BỘ HIỆN CÓ:
{clubsList}

YÊU CẦU CỦA SINH VIÊN:
{userMessage}

Hãy phân tích và đề xuất 3-5 câu lạc bộ phù hợp nhất với sinh viên này. Với mỗi câu lạc bộ, hãy trả về theo định dạng JSON như sau:

{{
  ""recommendations"": [
    {{
      ""id"": <club_id>,
      ""name"": ""<tên câu lạc bộ>"",
      ""reason"": ""<lý do phù hợp với sinh viên>"",
      ""confidence"": <điểm từ 0.0 đến 1.0>
    }}
  ]
}}

Chỉ trả về JSON, không thêm text nào khác. Lý do phải bằng tiếng Việt, thân thiện và nhiệt tình.";

            // Call Gemini API
            var response = await _geminiApiClient.GenerateContentAsync(prompt, cancellationToken);

            // Parse response to extract recommendations
            var recommendations = ParseClubRecommendations(response, activeClubs);

            _logger.LogInformation("Successfully generated {Count} club recommendations", recommendations.Count);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating club recommendations for StudentId: {StudentId}", studentId);
            throw;
        }
    }

    private List<RecommendationDto> ParseClubRecommendations(string geminiResponse, List<Club> availableClubs)
    {
        var recommendations = new List<RecommendationDto>();

        try
        {
            // Try to extract JSON from response (Gemini might add extra text)
            var jsonStart = geminiResponse.IndexOf('{');
            var jsonEnd = geminiResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = geminiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("recommendations", out var recsArray))
                {
                    foreach (var rec in recsArray.EnumerateArray())
                    {
                        if (rec.TryGetProperty("id", out var idProp) &&
                            rec.TryGetProperty("name", out var nameProp) &&
                            rec.TryGetProperty("reason", out var reasonProp) &&
                            rec.TryGetProperty("confidence", out var confidenceProp))
                        {
                            var clubId = idProp.GetInt32();
                            var club = availableClubs.FirstOrDefault(c => c.Id == clubId);
                            
                            if (club != null)
                            {
                                recommendations.Add(new RecommendationDto
                                {
                                    Type = "Club",
                                    Id = clubId,
                                    Name = club.Name,
                                    Description = club.Description,
                                    Reason = reasonProp.GetString() ?? "Phù hợp với bạn",
                                    ConfidenceScore = confidenceProp.GetDouble()
                                });
                            }
                        }
                    }
                }
            }
            
            // If parsing failed or no recommendations, return empty list
            if (!recommendations.Any())
            {
                _logger.LogWarning("Could not parse recommendations from Gemini response. Response: {Response}", geminiResponse);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from Gemini response: {Response}", geminiResponse);
        }

        return recommendations;
    }

    public async Task<List<RecommendationDto>> GetActivityRecommendationsAsync(
        int studentId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting activity recommendations for StudentId: {StudentId}", studentId);

        try
        {
            // Get student to access UserId
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentId}", studentId);
                return new List<RecommendationDto>();
            }

            // Get active activities (Status = "Approved", StartTime > now)
            var now = DateTime.UtcNow;
            var allActivities = await _activityRepository.SearchActivitiesAsync(null, null, "Approved", null, null);
            var activeActivities = allActivities
                .Where(a => a.StartTime > now)
                .OrderBy(a => a.StartTime)
                .ToList();
            
            if (!activeActivities.Any())
            {
                _logger.LogWarning("No active activities found");
                return new List<RecommendationDto>();
            }

            // Build student context
            var studentContext = await BuildStudentContextAsync(studentId);

            // Get clubs the student is a member of
            var clubMembers = await _clubMemberRepository.GetByStudentIdAsync(studentId);
            var studentClubIds = clubMembers
                .Where(cm => cm.IsActive)
                .Select(cm => cm.ClubId)
                .ToList();

            // Build activities list for prompt, prioritizing activities from student's clubs
            var activitiesList = new StringBuilder();
            foreach (var activity in activeActivities)
            {
                var isFromStudentClub = activity.ClubId.HasValue && studentClubIds.Contains(activity.ClubId.Value);
                var priority = isFromStudentClub ? "[ƯU TIÊN] " : "";
                var clubName = activity.Club?.Name ?? "Toàn trường";
                
                activitiesList.AppendLine($"{priority}ID: {activity.Id}, Tiêu đề: {activity.Title}, " +
                    $"Mô tả: {activity.Description ?? "Không có mô tả"}, " +
                    $"Loại: {activity.Type}, " +
                    $"Thời gian: {activity.StartTime:dd/MM/yyyy HH:mm}, " +
                    $"Địa điểm: {activity.Location ?? "Chưa xác định"}, " +
                    $"CLB: {clubName}");
            }

            // Create prompt template for activity recommendation
            var prompt = $@"Bạn là trợ lý AI của hệ thống EduXtend, giúp sinh viên tìm hoạt động phù hợp.

THÔNG TIN SINH VIÊN:
{studentContext}

DANH SÁCH HOẠT ĐỘNG SẮP DIỄN RA:
{activitiesList}

YÊU CẦU CỦA SINH VIÊN:
{userMessage}

Hãy đề xuất 3-5 hoạt động phù hợp nhất. Ưu tiên các hoạt động có đánh dấu [ƯU TIÊN] vì đó là hoạt động của CLB mà sinh viên đã tham gia. 
Với mỗi hoạt động, hãy trả về theo định dạng JSON như sau:

{{
  ""recommendations"": [
    {{
      ""id"": <activity_id>,
      ""name"": ""<tiêu đề hoạt động>"",
      ""reason"": ""<lý do phù hợp và lợi ích khi tham gia>"",
      ""confidence"": <điểm từ 0.0 đến 1.0>
    }}
  ]
}}

Chỉ trả về JSON, không thêm text nào khác. Lý do phải bằng tiếng Việt, thân thiện và khuyến khích sinh viên tham gia.";

            // Call Gemini API
            var response = await _geminiApiClient.GenerateContentAsync(prompt, cancellationToken);

            // Parse response to extract recommendations
            var recommendations = ParseActivityRecommendations(response, activeActivities);

            // Limit to top 5 recommendations
            var topRecommendations = recommendations.Take(5).ToList();

            _logger.LogInformation("Successfully generated {Count} activity recommendations", topRecommendations.Count);
            return topRecommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating activity recommendations for StudentId: {StudentId}", studentId);
            throw;
        }
    }

    private List<RecommendationDto> ParseActivityRecommendations(string geminiResponse, List<Activity> availableActivities)
    {
        var recommendations = new List<RecommendationDto>();

        try
        {
            // Try to extract JSON from response (Gemini might add extra text)
            var jsonStart = geminiResponse.IndexOf('{');
            var jsonEnd = geminiResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = geminiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("recommendations", out var recsArray))
                {
                    foreach (var rec in recsArray.EnumerateArray())
                    {
                        if (rec.TryGetProperty("id", out var idProp) &&
                            rec.TryGetProperty("name", out var nameProp) &&
                            rec.TryGetProperty("reason", out var reasonProp) &&
                            rec.TryGetProperty("confidence", out var confidenceProp))
                        {
                            var activityId = idProp.GetInt32();
                            var activity = availableActivities.FirstOrDefault(a => a.Id == activityId);
                            
                            if (activity != null)
                            {
                                recommendations.Add(new RecommendationDto
                                {
                                    Type = "Activity",
                                    Id = activityId,
                                    Name = activity.Title,
                                    Description = activity.Description,
                                    Reason = reasonProp.GetString() ?? "Phù hợp với bạn",
                                    ConfidenceScore = confidenceProp.GetDouble()
                                });
                            }
                        }
                    }
                }
            }
            
            // If parsing failed or no recommendations, return empty list
            if (!recommendations.Any())
            {
                _logger.LogWarning("Could not parse recommendations from Gemini response. Response: {Response}", geminiResponse);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from Gemini response: {Response}", geminiResponse);
        }

        return recommendations;
    }
}
