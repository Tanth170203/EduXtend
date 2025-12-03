# Developer Guide - Chatbot Rich Recommendations

## Overview

This guide provides technical details for developers working with the chatbot rich recommendations feature. It covers the architecture, implementation patterns, and best practices for extending or maintaining the system.

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend Layer                           │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ chatbot.js                                             │ │
│  │ - detectMessageType()                                  │ │
│  │ - renderRecommendationCard()                           │ │
│  │ - displayMessage()                                     │ │
│  │ - navigateToDetail()                                   │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ recommendation-cards.css                               │ │
│  │ - Card styling                                         │ │
│  │ - Responsive design                                    │ │
│  │ - Animations                                           │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              ↕ HTTP/JSON
┌─────────────────────────────────────────────────────────────┐
│                     Backend Layer                            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ ChatbotController.cs                                   │ │
│  │ - POST /api/chatbot/message                            │ │
│  │ - Authentication & rate limiting                       │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ ChatbotService.cs                                      │ │
│  │ - BuildStructuredPrompt()                              │ │
│  │ - ParseStructuredResponse()                            │ │
│  │ - ProcessChatMessageAsync()                            │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ GeminiAIService.cs                                     │ │
│  │ - GenerateResponseAsync()                              │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              ↕ HTTP
┌─────────────────────────────────────────────────────────────┐
│                     Gemini AI API                            │
│              (Google Generative AI)                          │
└─────────────────────────────────────────────────────────────┘
```

## JSON Schema Examples

### Example 1: Technology Clubs

**AI Response:**
```json
{
  "message": "Dựa trên chuyên ngành Công nghệ thông tin của bạn, tôi tìm thấy các câu lạc bộ phù hợp sau:",
  "recommendations": [
    {
      "id": 101,
      "name": "Câu lạc bộ Lập trình",
      "type": "club",
      "description": "Câu lạc bộ dành cho sinh viên yêu thích lập trình và phát triển phần mềm",
      "reason": "Phù hợp với chuyên ngành Công nghệ thông tin của bạn và giúp phát triển kỹ năng lập trình thực tế",
      "relevanceScore": 95
    },
    {
      "id": 102,
      "name": "Câu lạc bộ AI & Machine Learning",
      "type": "club",
      "description": "Nghiên cứu và ứng dụng trí tuệ nhân tạo trong các dự án thực tế",
      "reason": "Xu hướng công nghệ mới, phù hợp với sinh viên IT muốn học về AI và data science",
      "relevanceScore": 88
    },
    {
      "id": 103,
      "name": "Câu lạc bộ Cyber Security",
      "type": "club",
      "description": "Tìm hiểu về bảo mật thông tin và an ninh mạng",
      "reason": "Kỹ năng quan trọng cho sinh viên IT, nhiều cơ hội việc làm trong lĩnh vực này",
      "relevanceScore": 82
    }
  ]
}
```

### Example 2: Business Activities

**AI Response:**
```json
{
  "message": "Với chuyên ngành Quản trị kinh doanh, bạn có thể tham gia các hoạt động sau:",
  "recommendations": [
    {
      "id": 201,
      "name": "Workshop Khởi nghiệp 2024",
      "type": "activity",
      "description": "Hội thảo về khởi nghiệp và phát triển ý tưởng kinh doanh",
      "reason": "Giúp bạn học cách xây dựng business plan và pitch ý tưởng cho nhà đầu tư",
      "relevanceScore": 92
    },
    {
      "id": 202,
      "name": "Cuộc thi Business Case Competition",
      "type": "activity",
      "description": "Giải quyết các tình huống kinh doanh thực tế",
      "reason": "Rèn luyện tư duy phân tích và kỹ năng làm việc nhóm trong môi trường kinh doanh",
      "relevanceScore": 87
    }
  ]
}
```

### Example 3: Mixed Clubs and Activities

**AI Response:**
```json
{
  "message": "Dựa trên sở thích về nghệ thuật của bạn, đây là các gợi ý:",
  "recommendations": [
    {
      "id": 301,
      "name": "Câu lạc bộ Nhiếp ảnh",
      "type": "club",
      "description": "Học và chia sẻ kỹ thuật chụp ảnh, tổ chức photo walk",
      "reason": "Phát triển kỹ năng nghệ thuật và có cơ hội tham gia các dự án nhiếp ảnh",
      "relevanceScore": 90
    },
    {
      "id": 401,
      "name": "Triển lãm Nghệ thuật Sinh viên 2024",
      "type": "activity",
      "description": "Triển lãm tranh và tác phẩm nghệ thuật của sinh viên",
      "reason": "Cơ hội trưng bày tác phẩm và kết nối với cộng đồng nghệ sĩ trẻ",
      "relevanceScore": 85
    }
  ]
}
```

## Backend Implementation Details

### Building Structured Prompts

The `BuildStructuredPrompt` method constructs prompts that guide Gemini AI to return structured JSON:

```csharp
private string BuildStructuredPrompt(
    StudentContext context, 
    List<ClubInfo> clubs,
    List<ActivityInfo> activities,
    string userMessage)
{
    var prompt = new StringBuilder();
    
    // System instructions
    prompt.AppendLine("BẠN LÀ TRỢ LÝ AI HỖ TRỢ SINH VIÊN TÌM CÂU LẠC BỘ VÀ HOẠT ĐỘNG.");
    prompt.AppendLine();
    
    // JSON format instructions
    prompt.AppendLine("QUAN TRỌNG: Khi đề xuất câu lạc bộ hoặc hoạt động, bạn PHẢI trả về JSON theo format sau:");
    prompt.AppendLine();
    prompt.AppendLine("```json");
    prompt.AppendLine("{");
    prompt.AppendLine("  \"message\": \"Văn bản giới thiệu ngắn gọn\",");
    prompt.AppendLine("  \"recommendations\": [");
    prompt.AppendLine("    {");
    prompt.AppendLine("      \"id\": 123,");
    prompt.AppendLine("      \"name\": \"Tên câu lạc bộ\",");
    prompt.AppendLine("      \"type\": \"club\" hoặc \"activity\",");
    prompt.AppendLine("      \"description\": \"Mô tả ngắn (1-2 câu)\",");
    prompt.AppendLine("      \"reason\": \"Lý do phù hợp với sinh viên này\",");
    prompt.AppendLine("      \"relevanceScore\": 95");
    prompt.AppendLine("    }");
    prompt.AppendLine("  ]");
    prompt.AppendLine("}");
    prompt.AppendLine("```");
    prompt.AppendLine();
    
    // Scoring guidelines
    prompt.AppendLine("HƯỚNG DẪN TÍNH ĐIỂM relevanceScore (0-100):");
    prompt.AppendLine("- 90-100: Rất phù hợp (chuyên ngành trùng khớp, sở thích rõ ràng)");
    prompt.AppendLine("- 70-89: Phù hợp (lĩnh vực liên quan, có điểm chung)");
    prompt.AppendLine("- 50-69: Tạm được (phát triển kỹ năng chung, mở rộng kiến thức)");
    prompt.AppendLine("- 0-49: Ít phù hợp (khám phá lĩnh vực mới)");
    prompt.AppendLine();
    
    // Student context
    prompt.AppendLine($"THÔNG TIN SINH VIÊN:");
    prompt.AppendLine($"- Họ tên: {context.FullName}");
    prompt.AppendLine($"- Chuyên ngành: {context.MajorName}");
    prompt.AppendLine($"- Khóa: {context.Cohort}");
    prompt.AppendLine();
    
    // Available clubs
    if (clubs.Any())
    {
        prompt.AppendLine("CÁC CÂU LẠC BỘ ĐANG MỞ TUYỂN:");
        foreach (var club in clubs)
        {
            prompt.AppendLine($"- ID: {club.ClubId}, Tên: {club.Name}, " +
                            $"Danh mục: {club.CategoryName}, " +
                            $"Mô tả: {club.Description}");
        }
        prompt.AppendLine();
    }
    
    // Available activities
    if (activities.Any())
    {
        prompt.AppendLine("CÁC HOẠT ĐỘNG SẮP DIỄN RA:");
        foreach (var activity in activities)
        {
            prompt.AppendLine($"- ID: {activity.ActivityId}, Tên: {activity.Name}, " +
                            $"Loại: {activity.Type}, " +
                            $"Thời gian: {activity.StartDate:dd/MM/yyyy}");
        }
        prompt.AppendLine();
    }
    
    // User message
    prompt.AppendLine($"CÂU HỎI CỦA SINH VIÊN: {userMessage}");
    prompt.AppendLine();
    prompt.AppendLine("Hãy phân tích câu hỏi và trả về JSON với các đề xuất phù hợp nhất.");
    
    return prompt.ToString();
}
```

### Parsing Structured Responses

The `ParseStructuredResponse` method extracts and validates JSON from AI responses:

```csharp
private (bool isStructured, StructuredResponse? data, string plainText) 
    ParseStructuredResponse(string aiResponse)
{
    try
    {
        // Try to extract JSON from markdown code blocks
        var jsonMatch = Regex.Match(
            aiResponse, 
            @"```json\s*(\{.*?\})\s*```", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        
        string jsonContent = jsonMatch.Success 
            ? jsonMatch.Groups[1].Value 
            : aiResponse.Trim();
        
        // Try to deserialize
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        
        var structured = JsonSerializer.Deserialize<StructuredResponse>(
            jsonContent, 
            options
        );
        
        // Validate structure
        if (structured?.Recommendations != null && 
            structured.Recommendations.Any())
        {
            // Validate each recommendation
            foreach (var rec in structured.Recommendations)
            {
                if (rec.RelevanceScore < 0 || rec.RelevanceScore > 100)
                {
                    _logger.LogWarning(
                        "Invalid relevance score {Score} for recommendation {Name}",
                        rec.RelevanceScore, rec.Name
                    );
                    rec.RelevanceScore = Math.Clamp(rec.RelevanceScore, 0, 100);
                }
            }
            
            // Sort by relevance score
            structured.Recommendations = structured.Recommendations
                .OrderByDescending(r => r.RelevanceScore)
                .ToList();
            
            return (true, structured, string.Empty);
        }
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, 
            "Failed to parse structured response. Response: {Response}", 
            aiResponse.Substring(0, Math.Min(200, aiResponse.Length))
        );
    }
    
    // Fallback to plain text
    return (false, null, aiResponse);
}
```

### Processing Chat Messages

The main `ProcessChatMessageAsync` method orchestrates the flow:

```csharp
public async Task<ChatResponseDto> ProcessChatMessageAsync(
    int studentId, 
    string message, 
    List<ChatMessageDto> conversationHistory)
{
    try
    {
        // Build student context
        var context = await BuildStudentContextAsync(studentId);
        
        // Detect if this is a recommendation request
        bool isRecommendationRequest = DetectRecommendationRequest(message);
        
        string prompt;
        if (isRecommendationRequest)
        {
            // Get relevant clubs and activities
            var clubs = await GetRelevantClubsAsync(context);
            var activities = await GetRelevantActivitiesAsync(context);
            
            // Build structured prompt
            prompt = BuildStructuredPrompt(context, clubs, activities, message);
        }
        else
        {
            // Build regular prompt
            prompt = BuildRegularPrompt(context, message, conversationHistory);
        }
        
        // Get AI response
        var aiResponse = await _geminiService.GenerateResponseAsync(prompt);
        
        // Try to parse as structured response
        var (isStructured, structuredData, plainText) = 
            ParseStructuredResponse(aiResponse);
        
        if (isStructured && structuredData != null)
        {
            _logger.LogInformation(
                "Returning structured response with {Count} recommendations",
                structuredData.Recommendations.Count
            );
            
            return new ChatResponseDto
            {
                Message = structuredData.Message,
                HasRecommendations = true,
                Recommendations = structuredData.Recommendations
                    .Select(r => new RecommendationDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Type = r.Type,
                        Description = r.Description,
                        Reason = r.Reason,
                        RelevanceScore = r.RelevanceScore
                    }).ToList(),
                Success = true,
                SessionId = await GetOrCreateSessionIdAsync(studentId),
                Timestamp = DateTime.UtcNow
            };
        }
        
        // Fallback to plain text
        _logger.LogInformation("Returning plain text response");
        
        return new ChatResponseDto
        {
            Message = plainText,
            HasRecommendations = false,
            Success = true,
            SessionId = await GetOrCreateSessionIdAsync(studentId),
            Timestamp = DateTime.UtcNow
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing chat message for student {StudentId}", 
            studentId);
        
        return new ChatResponseDto
        {
            Message = "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại.",
            Success = false,
            ErrorMessage = ex.Message,
            SessionId = 0,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

### Detecting Recommendation Requests

```csharp
private bool DetectRecommendationRequest(string message)
{
    var keywords = new[]
    {
        "tìm câu lạc bộ",
        "gợi ý câu lạc bộ",
        "đề xuất câu lạc bộ",
        "câu lạc bộ nào",
        "club nào",
        "tìm hoạt động",
        "gợi ý hoạt động",
        "hoạt động nào"
    };
    
    var lowerMessage = message.ToLower();
    return keywords.Any(keyword => lowerMessage.Contains(keyword));
}
```

## Frontend Implementation Details

### Detecting Message Type

```javascript
function detectMessageType(response) {
    // Check if response has structured recommendations
    if (response.hasRecommendations && 
        response.recommendations && 
        Array.isArray(response.recommendations) &&
        response.recommendations.length > 0) {
        return 'recommendations';
    }
    
    // Default to plain text
    return 'text';
}
```

### Rendering Recommendation Cards

```javascript
function renderRecommendationCard(recommendation) {
    // Map type to icon
    const typeIcon = recommendation.type === 'club' ? '👥' : '🎯';
    const typeLabel = recommendation.type === 'club' ? 'CÂU LẠC BỘ' : 'HOẠT ĐỘNG';
    
    // Get score color
    const scoreColor = getScoreColor(recommendation.relevanceScore);
    
    // Build card HTML
    return `
        <div class="recommendation-card" 
             data-id="${recommendation.id}" 
             data-type="${recommendation.type}"
             role="button"
             tabindex="0"
             aria-label="${recommendation.name}. ${recommendation.description}. ${recommendation.reason}. Độ phù hợp ${recommendation.relevanceScore} phần trăm."
             onclick="navigateToDetail(${recommendation.id}, '${recommendation.type}')"
             onkeydown="handleCardKeydown(event, ${recommendation.id}, '${recommendation.type}')">
            
            <div class="card-header">
                <span class="card-type-icon" aria-hidden="true">${typeIcon}</span>
                <span class="card-type-label">${typeLabel}</span>
            </div>
            
            <h3 class="card-title">${escapeHtml(recommendation.name)}</h3>
            
            ${recommendation.description ? `
                <p class="card-description">${escapeHtml(recommendation.description)}</p>
            ` : ''}
            
            <div class="card-reason">
                <span class="reason-icon" aria-hidden="true">💡</span>
                <p class="reason-text">${escapeHtml(recommendation.reason)}</p>
            </div>
            
            <div class="card-score">
                <span class="score-icon" aria-hidden="true">✨</span>
                <span class="score-text" style="color: ${scoreColor}">
                    Độ phù hợp: ${recommendation.relevanceScore}%
                </span>
                <span class="sr-only">Độ phù hợp: ${recommendation.relevanceScore} phần trăm</span>
            </div>
        </div>
    `;
}

function getScoreColor(score) {
    if (score >= 90) return '#00A86B'; // Dark green - Excellent
    if (score >= 70) return '#32CD32'; // Medium green - Good
    if (score >= 50) return '#FFD700'; // Yellow - Fair
    return '#FF8C00'; // Orange - Low
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
```

### Keyboard Navigation

```javascript
function handleCardKeydown(event, id, type) {
    // Handle Enter or Space key
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        navigateToDetail(id, type);
    }
}
```

### Navigation to Detail Pages

```javascript
function navigateToDetail(id, type) {
    // Construct URL based on type
    const url = type === 'club' 
        ? `/clubs/${id}` 
        : `/activities/${id}`;
    
    // Track analytics (optional)
    if (typeof gtag !== 'undefined') {
        gtag('event', 'recommendation_click', {
            'recommendation_type': type,
            'recommendation_id': id
        });
    }
    
    // Navigate to detail page
    window.location.href = url;
}
```

## Data Models

### Backend Models

```csharp
// Services/Chatbot/Models/StructuredResponse.cs
public class StructuredResponse
{
    public string Message { get; set; } = string.Empty;
    public List<RecommendationItem> Recommendations { get; set; } = new();
}

// Services/Chatbot/Models/RecommendationItem.cs
public class RecommendationItem
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [RegularExpression("^(club|activity)$")]
    public string Type { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
    
    [Range(0, 100)]
    public int RelevanceScore { get; set; }
}

// BusinessObject/DTOs/Chatbot/RecommendationDto.cs
public class RecommendationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RelevanceScore { get; set; }
}
```

## Best Practices

### Prompt Engineering

1. **Be Explicit**: Clearly specify the JSON format with examples
2. **Provide Context**: Include student profile and available options
3. **Set Guidelines**: Define scoring criteria and expectations
4. **Use Vietnamese**: Request responses in the target language
5. **Limit Results**: Ask for 3-5 recommendations for optimal UX

### Response Parsing

1. **Handle Markdown**: Extract JSON from code blocks (```json...```)
2. **Case Insensitive**: Use case-insensitive deserialization
3. **Validate Data**: Check for required fields and valid ranges
4. **Log Failures**: Log parsing errors with context for debugging
5. **Graceful Fallback**: Always fall back to plain text on errors

### Frontend Rendering

1. **Escape HTML**: Sanitize all user-generated content
2. **Accessibility**: Include ARIA labels and keyboard navigation
3. **Responsive Design**: Ensure cards work on mobile devices
4. **Performance**: Avoid unnecessary re-renders
5. **Error Handling**: Never show blank screens or crashes

### Testing

1. **Unit Tests**: Test parsing logic with various JSON formats
2. **Integration Tests**: Test end-to-end flow with real AI responses
3. **Manual Testing**: Verify visual appearance and interactions
4. **Accessibility Testing**: Test with screen readers and keyboard
5. **Mobile Testing**: Test on actual mobile devices

## Extending the System

### Adding New Recommendation Types

To add a new recommendation type (e.g., "event"):

1. Update the prompt to include event data
2. Add "event" to the type validation regex
3. Add event icon mapping in frontend
4. Update navigation logic for event detail pages

### Customizing Card Appearance

To customize card styles:

1. Edit `recommendation-cards.css`
2. Modify CSS variables for colors and spacing
3. Update card HTML structure in `renderRecommendationCard()`
4. Test responsive behavior on mobile

### Adding Analytics

To track recommendation interactions:

```javascript
function navigateToDetail(id, type) {
    // Track with Google Analytics
    if (typeof gtag !== 'undefined') {
        gtag('event', 'recommendation_click', {
            'recommendation_type': type,
            'recommendation_id': id,
            'student_major': getCurrentStudentMajor()
        });
    }
    
    // Track with custom analytics
    fetch('/api/analytics/track', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            event: 'recommendation_click',
            type: type,
            id: id,
            timestamp: new Date().toISOString()
        })
    });
    
    window.location.href = `/${type}s/${id}`;
}
```

## Performance Optimization

### Backend Caching

```csharp
private readonly IMemoryCache _cache;

private async Task<List<ClubInfo>> GetRelevantClubsAsync(StudentContext context)
{
    var cacheKey = $"clubs_{context.MajorId}";
    
    if (!_cache.TryGetValue(cacheKey, out List<ClubInfo> clubs))
    {
        clubs = await _clubRepository.GetActiveClubsAsync();
        
        _cache.Set(cacheKey, clubs, TimeSpan.FromMinutes(10));
    }
    
    return clubs;
}
```

### Frontend Optimization

```javascript
// Debounce typing indicator
let typingTimeout;
function showTypingIndicator() {
    clearTimeout(typingTimeout);
    const indicator = document.getElementById('typing-indicator');
    indicator.style.display = 'block';
}

function hideTypingIndicator() {
    typingTimeout = setTimeout(() => {
        const indicator = document.getElementById('typing-indicator');
        indicator.style.display = 'none';
    }, 300);
}
```

## Security Considerations

### Input Sanitization

```javascript
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Use when rendering
messageDiv.innerHTML = `<p>${escapeHtml(userInput)}</p>`;
```

### API Security

- All endpoints require authentication
- Rate limiting prevents abuse (15 req/min)
- Input validation on backend
- CSRF protection enabled
- No sensitive data in logs

## Troubleshooting

See [TROUBLESHOOTING_GUIDE.md](./TROUBLESHOOTING_GUIDE.md) for common issues and solutions.

## Additional Resources

- [API Documentation](./API_DOCUMENTATION.md)
- [CSS Reference](./CSS_REFERENCE.md)
- [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md)
- [Requirements Document](./requirements.md)
- [Design Document](./design.md)
