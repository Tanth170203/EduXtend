# Design Document - AI Chatbot Assistant

## Overview

Hệ thống AI Chatbot Assistant tích hợp Gemini AI vào nền tảng EduXtend để hỗ trợ sinh viên tìm kiếm và nhận đề xuất về các câu lạc bộ (CLB) và hoạt động phù hợp. Chatbot phân tích thông tin profile sinh viên (chuyên ngành, sở thích, CLB hiện tại) và dữ liệu hệ thống (danh sách CLB, hoạt động sắp tới) để đưa ra các gợi ý thông minh, cá nhân hóa thông qua giao diện chat tương tác.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        WebFE (Razor Pages)                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Chat UI Component (JavaScript + HTML)              │   │
│  │  - Floating chat button                             │   │
│  │  - Chat modal window                                │   │
│  │  - Message display & input                          │   │
│  │  - Session storage for chat history                 │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ HTTPS + Cookie Auth
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    WebAPI (.NET Core)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  ChatbotController                                   │   │
│  │  - POST /api/chatbot/message                        │   │
│  │  - Authentication via CustomJWT                     │   │
│  └──────────────────────────────────────────────────────┘   │
│                            │                                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  ChatbotService                                      │   │
│  │  - Build context from student profile               │   │
│  │  - Retrieve clubs & activities data                 │   │
│  │  - Format prompt for Gemini AI                      │   │
│  │  - Parse AI response                                │   │
│  └──────────────────────────────────────────────────────┘   │
│                            │                                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  GeminiAIService                                     │   │
│  │  - HTTP client to Gemini API                        │   │
│  │  - Request/response handling                        │   │
│  │  - Error handling & retry logic                     │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ HTTPS + API Key
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              Google Gemini AI API                            │
│              (generativelanguage.googleapis.com)             │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack

- **Frontend**: Razor Pages, JavaScript (Vanilla), HTML5, CSS3
- **Backend**: ASP.NET Core 8.0, C#
- **AI Service**: Google Gemini API (gemini-1.5-flash or gemini-1.5-pro)
- **Authentication**: HTTP Cookie with JWT token
- **Database**: SQL Server (existing EduXtendContext)
- **HTTP Client**: HttpClient with IHttpClientFactory

## Components and Interfaces

### 1. Frontend Components

#### 1.1 Chat UI Component (`wwwroot/js/chatbot.js`)

**Responsibilities:**
- Render floating chat button on all pages
- Display chat modal window with messages
- Handle user input and send messages to API
- Display AI responses with typing animation
- Manage chat history in session storage
- Handle errors and loading states

**Key Functions:**
```javascript
// Initialize chatbot on page load
function initChatbot()

// Toggle chat modal visibility
function toggleChatModal()

// Send message to API
async function sendMessage(message)

// Display message in chat UI
function displayMessage(message, isUser)

// Show typing indicator
function showTypingIndicator()

// Hide typing indicator
function hideTypingIndicator()

// Load chat history from session storage
function loadChatHistory()

// Save chat history to session storage
function saveChatHistory()

// Handle quick action buttons
function handleQuickAction(action)
```

#### 1.2 Chat UI Styles (`wwwroot/css/chatbot.css`)

**Responsibilities:**
- Style floating chat button
- Style chat modal window
- Style message bubbles (user vs AI)
- Style quick action buttons
- Responsive design for mobile devices

### 2. Backend Components

#### 2.1 ChatbotController

**Location**: `WebAPI/Controllers/ChatbotController.cs`

**Responsibilities:**
- Handle HTTP POST requests for chat messages
- Authenticate user via CustomJWT
- Extract student ID from claims
- Call ChatbotService to process message
- Return AI response to client
- Handle errors and return appropriate status codes

**Endpoints:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    // POST api/chatbot/message
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
}
```

**Request/Response Models:**
```csharp
public class ChatMessageRequest
{
    public string Message { get; set; }
    public List<ChatMessage>? ConversationHistory { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } // "user" or "assistant"
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ChatMessageResponse
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### 2.2 ChatbotService

**Location**: `Services/Chatbot/ChatbotService.cs`

**Interface**: `Services/Chatbot/IChatbotService.cs`

**Responsibilities:**
- Retrieve student profile from database
- Retrieve relevant clubs and activities
- Build context for AI prompt
- Format conversation history
- Call GeminiAIService
- Parse and format AI response
- Handle business logic errors

**Key Methods:**
```csharp
public interface IChatbotService
{
    Task<string> ProcessChatMessageAsync(
        int studentId, 
        string userMessage, 
        List<ChatMessage>? conversationHistory);
}

public class ChatbotService : IChatbotService
{
    private async Task<StudentContext> BuildStudentContextAsync(int studentId)
    private async Task<List<ClubRecommendation>> GetRelevantClubsAsync(StudentContext context)
    private async Task<List<ActivityRecommendation>> GetUpcomingActivitiesAsync(StudentContext context)
    private string BuildAIPrompt(StudentContext context, string userMessage, List<ChatMessage>? history)
    private string FormatConversationHistory(List<ChatMessage> history)
}
```

**Data Models:**
```csharp
public class StudentContext
{
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public string MajorName { get; set; }
    public string Cohort { get; set; }
    public List<string> CurrentClubs { get; set; }
    public List<string> Interests { get; set; } // Derived from club categories
}

public class ClubRecommendation
{
    public int ClubId { get; set; }
    public string Name { get; set; }
    public string SubName { get; set; }
    public string Description { get; set; }
    public string CategoryName { get; set; }
    public bool IsRecruitmentOpen { get; set; }
}

public class ActivityRecommendation
{
    public int ActivityId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public DateTime StartTime { get; set; }
    public string ClubName { get; set; }
    public string ActivityType { get; set; }
    public bool IsPublic { get; set; }
}
```

#### 2.3 GeminiAIService

**Location**: `Services/Chatbot/GeminiAIService.cs`

**Interface**: `Services/Chatbot/IGeminiAIService.cs`

**Responsibilities:**
- Configure HTTP client for Gemini API
- Send requests to Gemini API
- Handle API responses and errors
- Implement retry logic for transient failures
- Parse JSON responses from Gemini
- Handle rate limiting and quota errors

**Key Methods:**
```csharp
public interface IGeminiAIService
{
    Task<string> GenerateResponseAsync(string prompt);
}

public class GeminiAIService : IGeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiAIOptions _options;
    private readonly ILogger<GeminiAIService> _logger;
    
    public async Task<string> GenerateResponseAsync(string prompt)
    private async Task<string> SendRequestWithRetryAsync(string prompt, int maxRetries = 3)
    private GeminiRequest BuildRequest(string prompt)
    private string ParseResponse(string jsonResponse)
}
```

**Configuration Model:**
```csharp
public class GeminiAIOptions
{
    public string ApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-flash";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
    public string ApiEndpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
}
```

**Gemini API Request/Response Models:**
```csharp
public class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; }
    public GeminiGenerationConfig GenerationConfig { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart> Parts { get; set; }
}

public class GeminiPart
{
    public string Text { get; set; }
}

public class GeminiGenerationConfig
{
    public double Temperature { get; set; }
    public int MaxOutputTokens { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate> Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent Content { get; set; }
}
```

### 3. Configuration

#### 3.1 appsettings.json Configuration

**WebAPI/appsettings.json:**
```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
    "Model": "gemini-1.5-flash",
    "Temperature": 0.7,
    "MaxTokens": 1024,
    "ApiEndpoint": "https://generativelanguage.googleapis.com/v1beta/models"
  }
}
```

## Data Models

### Database Entities (Existing)

The chatbot leverages existing database entities:
- **Student**: Profile information (major, cohort, email)
- **Club**: Club details (name, description, category)
- **ClubMember**: Student's club memberships
- **Activity**: Activity details (title, description, time, location)
- **ClubCategory**: Club categories for interest matching
- **Major**: Student's major for relevance matching

### DTOs (New)

Located in `BusinessObject/DTOs/Chatbot/`:

```csharp
// Request DTO
public class ChatMessageRequestDto
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; }
    
    public List<ChatMessageDto>? ConversationHistory { get; set; }
}

// Response DTO
public class ChatMessageResponseDto
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// Chat message DTO
public class ChatMessageDto
{
    [Required]
    public string Role { get; set; } // "user" or "assistant"
    
    [Required]
    public string Content { get; set; }
    
    public DateTime Timestamp { get; set; }
}
```

## Error Handling

### Error Categories

1. **Authentication Errors (401)**
   - User not authenticated
   - Invalid or expired JWT token
   - Action: Redirect to login page

2. **Validation Errors (400)**
   - Empty message
   - Message too long (>2000 characters)
   - Invalid conversation history format
   - Action: Display validation error in chat

3. **Gemini API Errors (502/503)**
   - API key invalid or missing
   - Rate limit exceeded
   - Quota exceeded
   - Network timeout
   - Action: Display user-friendly error message

4. **Database Errors (500)**
   - Failed to retrieve student profile
   - Failed to retrieve clubs/activities
   - Action: Log error and display generic error message

5. **Unexpected Errors (500)**
   - Unhandled exceptions
   - Action: Log full stack trace and display generic error message

### Error Response Format

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public string? Details { get; set; } // Only in development
}
```

### User-Friendly Error Messages

```csharp
public static class ChatbotErrorMessages
{
    public const string NetworkError = "Không thể kết nối đến AI Assistant. Vui lòng thử lại sau.";
    public const string QuotaExceeded = "AI Assistant tạm thời quá tải. Vui lòng thử lại sau ít phút.";
    public const string InvalidApiKey = "Cấu hình AI Assistant không hợp lệ. Vui lòng liên hệ quản trị viên.";
    public const string Timeout = "Yêu cầu mất quá nhiều thời gian. Vui lòng thử lại.";
    public const string GenericError = "Đã xảy ra lỗi. Vui lòng thử lại sau.";
    public const string Unauthorized = "Bạn cần đăng nhập để sử dụng AI Assistant.";
}
```

## Testing Strategy

### Unit Tests

**ChatbotService Tests:**
- Test BuildStudentContextAsync with valid student ID
- Test BuildStudentContextAsync with invalid student ID
- Test GetRelevantClubsAsync returns clubs matching student interests
- Test GetUpcomingActivitiesAsync filters by date and status
- Test BuildAIPrompt formats context correctly
- Test ProcessChatMessageAsync handles empty message
- Test ProcessChatMessageAsync handles conversation history

**GeminiAIService Tests:**
- Test GenerateResponseAsync with valid prompt
- Test GenerateResponseAsync with invalid API key
- Test retry logic on transient failures
- Test timeout handling
- Test response parsing with valid JSON
- Test response parsing with malformed JSON

**ChatbotController Tests:**
- Test SendMessage with authenticated user
- Test SendMessage with unauthenticated user (401)
- Test SendMessage with invalid request (400)
- Test SendMessage returns AI response
- Test SendMessage handles service errors

### Integration Tests

**End-to-End Chat Flow:**
- User sends message → API processes → Gemini responds → User receives response
- Test with real Gemini API (using test API key)
- Test conversation history is maintained
- Test quick actions trigger appropriate responses

**Database Integration:**
- Test student context retrieval from database
- Test club recommendations query performance
- Test activity recommendations query performance

### Manual Testing Checklist

**UI/UX Testing:**
- [ ] Floating chat button appears on all pages
- [ ] Chat modal opens and closes correctly
- [ ] Welcome message displays on first open
- [ ] Quick action buttons work correctly
- [ ] User messages display on right side
- [ ] AI messages display on left side
- [ ] Typing indicator shows while waiting
- [ ] Error messages display correctly
- [ ] Chat history persists when modal is closed/reopened
- [ ] Chat history clears on logout
- [ ] Mobile responsive design works

**Functional Testing:**
- [ ] User can send text messages
- [ ] AI responds with relevant club recommendations
- [ ] AI responds with relevant activity recommendations
- [ ] AI understands Vietnamese language
- [ ] AI provides personalized responses based on student profile
- [ ] Conversation context is maintained across messages
- [ ] Authentication is enforced
- [ ] Errors are handled gracefully

**Performance Testing:**
- [ ] API response time < 5 seconds for typical requests
- [ ] Chat UI remains responsive during API calls
- [ ] Session storage doesn't grow unbounded (50 message limit)
- [ ] No memory leaks in JavaScript

**Security Testing:**
- [ ] API key is not exposed to client
- [ ] User can only access their own student data
- [ ] XSS protection in message display
- [ ] CSRF protection on API endpoint
- [ ] Rate limiting prevents abuse

## AI Prompt Engineering

### System Prompt Template

```
Bạn là AI Assistant của hệ thống EduXtend, một nền tảng quản lý câu lạc bộ sinh viên.
Nhiệm vụ của bạn là hỗ trợ sinh viên tìm kiếm và tham gia các câu lạc bộ (CLB) và hoạt động phù hợp.

THÔNG TIN SINH VIÊN:
- Họ tên: {FullName}
- Chuyên ngành: {MajorName}
- Khóa: {Cohort}
- CLB hiện tại: {CurrentClubs}

DANH SÁCH CLB ĐANG MỞ TUYỂN:
{ClubList}

HOẠT ĐỘNG SẮP TỚI:
{ActivityList}

HƯỚNG DẪN:
1. Trả lời bằng tiếng Việt, thân thiện và nhiệt tình
2. Đề xuất CLB và hoạt động phù hợp với chuyên ngành và sở thích của sinh viên
3. Giải thích lý do tại sao CLB/hoạt động phù hợp
4. Cung cấp thông tin cụ thể: tên CLB, mô tả, thời gian hoạt động
5. Khuyến khích sinh viên tham gia và phát triển kỹ năng
6. Nếu không có thông tin phù hợp, gợi ý sinh viên khám phá các lựa chọn khác
7. Giữ câu trả lời ngắn gọn (dưới 500 từ)

LỊCH SỬ HỘI THOẠI:
{ConversationHistory}

CÂU HỎI CỦA SINH VIÊN:
{UserMessage}
```

### Prompt Optimization Strategies

1. **Context Limitation**: Only include top 5 relevant clubs and top 5 upcoming activities to reduce token usage
2. **Conversation History**: Limit to last 10 messages to maintain context without exceeding token limits
3. **Structured Output**: Encourage AI to format responses with bullet points and clear sections
4. **Fallback Responses**: Provide default responses when no relevant data is available

## Security Considerations

### API Key Protection

- API key stored in appsettings.json (server-side only)
- Never expose API key to client-side JavaScript
- Use environment variables in production
- Rotate API keys periodically

### Authentication & Authorization

- All chatbot endpoints require authentication via CustomJWT
- User can only access their own student profile
- No privilege escalation possible through chatbot

### Input Validation

- Sanitize user messages to prevent injection attacks
- Limit message length to 2000 characters
- Validate conversation history structure
- Escape HTML in message display to prevent XSS

### Rate Limiting

- Implement rate limiting on chatbot endpoint (e.g., 10 requests per minute per user)
- Prevent abuse and excessive API costs
- Return 429 Too Many Requests when limit exceeded

### Data Privacy

- Do not log sensitive student information
- Do not send sensitive data to Gemini AI
- Comply with GDPR/data protection regulations
- Clear chat history on logout

## Performance Optimization

### Caching Strategies

1. **Student Context Caching**: Cache student profile for 5 minutes to reduce database queries
2. **Club List Caching**: Cache active clubs for 10 minutes
3. **Activity List Caching**: Cache upcoming activities for 5 minutes

### Database Query Optimization

- Use eager loading for related entities (Include)
- Index on frequently queried fields (StudentId, ClubId, ActivityId)
- Limit query results (top 10 clubs, top 10 activities)

### API Response Time

- Target: < 5 seconds for 95th percentile
- Implement timeout on Gemini API calls (30 seconds)
- Use async/await throughout the stack
- Consider background processing for complex queries

### Frontend Optimization

- Lazy load chatbot JavaScript only when needed
- Minimize DOM manipulations
- Use CSS animations for smooth UX
- Debounce user input if implementing typing indicators

## Deployment Considerations

### Configuration Management

- Use Azure Key Vault or similar for API key storage in production
- Separate appsettings for Development, Staging, Production
- Environment-specific Gemini API quotas

### Monitoring & Logging

- Log all chatbot requests with correlation IDs
- Track Gemini API usage and costs
- Monitor error rates and response times
- Set up alerts for quota limits and errors

### Scalability

- Stateless design allows horizontal scaling
- Consider Redis for distributed caching
- Monitor database connection pool usage
- Implement circuit breaker for Gemini API calls

### Rollback Plan

- Feature flag to enable/disable chatbot
- Graceful degradation if Gemini API is unavailable
- Fallback to static FAQ responses if needed
