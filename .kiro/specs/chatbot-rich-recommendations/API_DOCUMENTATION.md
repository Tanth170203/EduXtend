# Chatbot Rich Recommendations API Documentation

## Overview

The Chatbot Rich Recommendations API extends the existing chatbot functionality to support structured recommendation responses. This allows the AI to return visually rich, card-based recommendations for clubs and activities instead of plain text responses.

## API Endpoints

### POST /api/chatbot/message

Send a message to the chatbot and receive a response with optional structured recommendations.

#### Request

**Headers:**
```
Content-Type: application/json
Cookie: .AspNetCore.Identity.Application (authentication required)
```

**Body:**
```json
{
  "message": "Tìm câu lạc bộ về công nghệ",
  "sessionId": 12345
}
```

**Parameters:**
- `message` (string, required): The user's message to the chatbot
- `sessionId` (integer, optional): The chat session ID for conversation continuity

#### Response - Structured Recommendations

When the AI detects a recommendation request, it returns structured data:

```json
{
  "message": "Dựa trên chuyên ngành Công nghệ thông tin của bạn, tôi tìm thấy các câu lạc bộ phù hợp sau:",
  "sessionId": 12345,
  "timestamp": "2024-12-02T10:30:00Z",
  "success": true,
  "hasRecommendations": true,
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
      "description": "Nghiên cứu và ứng dụng trí tuệ nhân tạo",
      "reason": "Xu hướng công nghệ mới, phù hợp với sinh viên IT muốn học về AI",
      "relevanceScore": 88
    },
    {
      "id": 203,
      "name": "Hackathon 2024",
      "type": "activity",
      "description": "Cuộc thi lập trình 48 giờ",
      "reason": "Cơ hội thực hành kỹ năng lập trình và làm việc nhóm",
      "relevanceScore": 82
    }
  ],
  "errorMessage": null
}
```

#### Response - Plain Text

When the AI returns a plain text response (non-recommendation queries):

```json
{
  "message": "Xin chào! Tôi có thể giúp bạn tìm câu lạc bộ hoặc hoạt động phù hợp. Bạn quan tâm đến lĩnh vực nào?",
  "sessionId": 12345,
  "timestamp": "2024-12-02T10:30:00Z",
  "success": true,
  "hasRecommendations": false,
  "recommendations": null,
  "errorMessage": null
}
```

#### Response - Error

When an error occurs:

```json
{
  "message": "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại.",
  "sessionId": 12345,
  "timestamp": "2024-12-02T10:30:00Z",
  "success": false,
  "hasRecommendations": false,
  "recommendations": null,
  "errorMessage": "Internal server error: Connection timeout"
}
```

## Data Models

### ChatResponseDto

The main response object returned by the chatbot API.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| message | string | Yes | The main text message from the AI |
| sessionId | integer | Yes | The chat session identifier |
| timestamp | DateTime | Yes | ISO 8601 timestamp of the response |
| success | boolean | Yes | Indicates if the request was successful |
| hasRecommendations | boolean | Yes | True if structured recommendations are included |
| recommendations | RecommendationDto[] | No | Array of recommendation objects (null if hasRecommendations is false) |
| errorMessage | string | No | Error details if success is false |

### RecommendationDto

Individual recommendation object within the recommendations array.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| id | integer | Yes | Unique identifier of the club or activity |
| name | string | Yes | Display name of the club/activity |
| type | string | Yes | Either "club" or "activity" |
| description | string | Yes | Brief description of the club/activity |
| reason | string | Yes | Explanation of why this is recommended for the student |
| relevanceScore | integer | Yes | Relevance percentage (0-100) based on student profile matching |

## Structured Response Format

### JSON Schema for AI Responses

The backend instructs Gemini AI to return responses in the following JSON format:

```json
{
  "message": "Introductory text explaining the recommendations",
  "recommendations": [
    {
      "id": 123,
      "name": "Club or Activity Name",
      "type": "club",
      "description": "Brief description",
      "reason": "Why this matches the student's profile",
      "relevanceScore": 95
    }
  ]
}
```

### Field Specifications

**message** (string)
- Brief introductory text in Vietnamese
- Should acknowledge the student's request
- Example: "Dựa trên sở thích của bạn, tôi tìm thấy các câu lạc bộ sau:"

**recommendations** (array)
- Must contain at least 1 recommendation
- Maximum 5 recommendations per response (for optimal UX)
- Sorted by relevanceScore in descending order

**id** (integer)
- Must match an existing club or activity ID in the database
- Used for navigation to detail pages

**name** (string)
- Official name of the club or activity
- In Vietnamese
- Maximum 100 characters

**type** (string)
- Must be either "club" or "activity"
- Lowercase only
- Used to determine icon and navigation path

**description** (string)
- Brief description (1-2 sentences)
- In Vietnamese
- Maximum 200 characters
- Should highlight key features or focus areas

**reason** (string)
- Personalized explanation of why this recommendation fits the student
- Should reference student's major, interests, or profile
- In Vietnamese
- Maximum 300 characters
- Example: "Phù hợp với chuyên ngành Công nghệ thông tin của bạn"

**relevanceScore** (integer)
- Range: 0-100
- Represents percentage match with student profile
- Used for color-coded visualization
- Scoring guidelines:
  - 90-100: Excellent match (major alignment, strong interest match)
  - 70-89: Good match (related field, some interest overlap)
  - 50-69: Fair match (general interest, skill development)
  - 0-49: Low match (exploratory, broadening horizons)

## Backend Implementation

### Prompt Engineering

The backend builds enhanced prompts that instruct Gemini AI to return structured JSON:

```csharp
private string BuildStructuredPrompt(StudentContext context, string userMessage)
{
    var prompt = new StringBuilder();
    
    prompt.AppendLine("BẠN LÀ TRỢ LÝ AI HỖ TRỢ SINH VIÊN TÌM CÂU LẠC BỘ VÀ HOẠT ĐỘNG.");
    prompt.AppendLine();
    prompt.AppendLine("QUAN TRỌNG: Khi đề xuất câu lạc bộ, bạn PHẢI trả về JSON theo format:");
    prompt.AppendLine();
    prompt.AppendLine("```json");
    prompt.AppendLine("{");
    prompt.AppendLine("  \"message\": \"Văn bản giới thiệu\",");
    prompt.AppendLine("  \"recommendations\": [");
    prompt.AppendLine("    {");
    prompt.AppendLine("      \"id\": 123,");
    prompt.AppendLine("      \"name\": \"Tên câu lạc bộ\",");
    prompt.AppendLine("      \"type\": \"club\",");
    prompt.AppendLine("      \"description\": \"Mô tả ngắn\",");
    prompt.AppendLine("      \"reason\": \"Lý do phù hợp\",");
    prompt.AppendLine("      \"relevanceScore\": 95");
    prompt.AppendLine("    }");
    prompt.AppendLine("  ]");
    prompt.AppendLine("}");
    prompt.AppendLine("```");
    
    // Add student context and available clubs...
    
    return prompt.ToString();
}
```

### Response Parsing

The backend parses AI responses with fallback handling:

```csharp
private (bool isStructured, StructuredResponse? data, string plainText) 
    ParseStructuredResponse(string aiResponse)
{
    try
    {
        // Extract JSON from markdown code blocks
        var jsonMatch = Regex.Match(aiResponse, 
            @"```json\s*(\{.*?\})\s*```", 
            RegexOptions.Singleline);
        
        string jsonContent = jsonMatch.Success 
            ? jsonMatch.Groups[1].Value 
            : aiResponse;
        
        // Deserialize with case-insensitive options
        var structured = JsonSerializer.Deserialize<StructuredResponse>(
            jsonContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        
        // Validate structure
        if (structured?.Recommendations != null && 
            structured.Recommendations.Any())
        {
            return (true, structured, string.Empty);
        }
    }
    catch (JsonException ex)
    {
        _logger.LogWarning("Failed to parse structured response: {Error}", 
            ex.Message);
    }
    
    // Fallback to plain text
    return (false, null, aiResponse);
}
```

## Frontend Integration

### Detecting Response Type

```javascript
function detectMessageType(response) {
    if (response.hasRecommendations && 
        response.recommendations && 
        response.recommendations.length > 0) {
        return 'recommendations';
    }
    return 'text';
}
```

### Rendering Recommendations

```javascript
function displayMessage(role, content, response = null) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `chat-message ${role}-message`;
    
    if (role === 'assistant' && response) {
        const messageType = detectMessageType(response);
        
        if (messageType === 'recommendations') {
            // Display intro text
            if (response.message) {
                messageDiv.innerHTML += `
                    <div class="message-text">${response.message}</div>
                `;
            }
            
            // Display recommendation cards
            const cardsContainer = document.createElement('div');
            cardsContainer.className = 'recommendations-container';
            
            response.recommendations.forEach(rec => {
                cardsContainer.innerHTML += renderRecommendationCard(rec);
            });
            
            messageDiv.appendChild(cardsContainer);
        } else {
            // Plain text message
            messageDiv.innerHTML = `
                <div class="message-text">${content}</div>
            `;
        }
    }
    
    chatHistory.appendChild(messageDiv);
}
```

## Rate Limiting

The chatbot API maintains existing rate limits:
- **15 requests per minute** per student
- Returns HTTP 429 (Too Many Requests) when limit exceeded
- Rate limit applies to both structured and plain text responses

## Authentication

All chatbot endpoints require authentication:
- Student must be logged in with valid session cookie
- Unauthenticated requests return HTTP 401 (Unauthorized)
- Student context (major, cohort) is automatically loaded from authenticated session

## Error Handling

### Backend Errors

| Error Type | HTTP Status | Response |
|------------|-------------|----------|
| Unauthenticated | 401 | `{ "success": false, "errorMessage": "Unauthorized" }` |
| Rate limit exceeded | 429 | `{ "success": false, "errorMessage": "Too many requests" }` |
| Invalid JSON | 200 | Falls back to plain text response |
| Gemini API error | 200 | `{ "success": false, "errorMessage": "AI service unavailable" }` |
| Database error | 500 | `{ "success": false, "errorMessage": "Internal server error" }` |

### Frontend Error Handling

The frontend gracefully handles errors:
- Network errors: Display "Không thể kết nối đến server"
- Malformed responses: Fall back to plain text display
- Empty recommendations: Display plain text message
- No crashes or blank screens in error scenarios

## Example Usage

### Example 1: Technology Club Recommendation

**Request:**
```json
{
  "message": "Tìm câu lạc bộ về công nghệ cho sinh viên IT",
  "sessionId": 12345
}
```

**Response:**
```json
{
  "message": "Dựa trên chuyên ngành Công nghệ thông tin của bạn, đây là các câu lạc bộ phù hợp:",
  "hasRecommendations": true,
  "recommendations": [
    {
      "id": 101,
      "name": "Câu lạc bộ Lập trình",
      "type": "club",
      "description": "Phát triển kỹ năng lập trình qua các dự án thực tế",
      "reason": "Hoàn toàn phù hợp với chuyên ngành IT, giúp bạn nâng cao kỹ năng coding",
      "relevanceScore": 95
    }
  ],
  "success": true
}
```

### Example 2: General Question (Plain Text)

**Request:**
```json
{
  "message": "Câu lạc bộ là gì?",
  "sessionId": 12345
}
```

**Response:**
```json
{
  "message": "Câu lạc bộ là nhóm sinh viên có cùng sở thích hoặc mục tiêu, tổ chức các hoạt động để phát triển kỹ năng và kết nối với nhau.",
  "hasRecommendations": false,
  "recommendations": null,
  "success": true
}
```

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-12-02 | Initial release with structured recommendations support |

## Support

For API issues or questions, contact the development team or refer to the troubleshooting guide.
