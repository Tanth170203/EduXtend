# EduXtend AI Chatbot Module

## Overview
This module provides an AI-powered chatbot with personalized recommendations using Google's Gemini AI. It includes rate limiting, metrics tracking, and conversation history management.

## Features
- ✅ AI-powered conversations using Gemini AI
- ✅ Personalized recommendations based on student profile
- ✅ Conversation history and session management
- ✅ Rate limiting (15 requests per minute per user)
- ✅ Metrics and monitoring
- ✅ Intent detection (Club recommendations, Activity suggestions, General conversation)
- ✅ Responsive chat UI with modern design

## Architecture

### Backend Components

#### Services
- **GeminiApiClient**: Handles communication with Google Gemini AI API
- **ChatbotService**: Main service for processing chat requests and managing conversations
- **RecommendationEngine**: Generates personalized club and activity recommendations
- **ChatbotMetricsService**: Tracks usage metrics and performance

#### Repositories
- **ChatSessionRepository**: Manages chat sessions and message persistence

#### Controllers
- **ChatbotController**: API endpoints for chat interactions
- **ChatbotMetricsController**: Admin endpoints for monitoring

#### Middleware
- **ChatbotRateLimitAttribute**: Rate limiting for chatbot endpoints

### Frontend Components
- **chatbot.js**: Chat UI and API integration
- **chatbot.css**: Modern chat interface styling

### Models & DTOs
- **ChatSession**: Chat session entity
- **ChatMessage**: Individual message entity
- **ChatRequestDto**: Request payload
- **ChatResponseDto**: Response payload
- **RecommendationDto**: Recommendation data structure

## Installation

### 1. Backend Setup

#### Add NuGet Packages
```bash
# In your main project
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.Extensions.Http
```

#### Copy Files
1. Copy `Backend/Services/` to your `Services/` folder
2. Copy `Backend/Repositories/` to your `Repositories/` folder
3. Copy `Backend/Controllers/` to your `WebAPI/Controllers/` folder
4. Copy `Backend/Middleware/` to your `WebAPI/Middleware/` folder
5. Copy `Backend/Configuration/` to your `WebAPI/Configuration/` folder
6. Copy `Backend/Models/` to your `BusinessObject/Models/` folder
7. Copy `Backend/DTOs/` to your `BusinessObject/DTOs/` folder

#### Database Migration
Add these entities to your DbContext:

```csharp
public DbSet<ChatSession> ChatSessions { get; set; }
public DbSet<ChatMessage> ChatMessages { get; set; }
```

Run migration:
```bash
dotnet ef migrations add AddChatbotTables
dotnet ef database update
```

#### Configure Services in Program.cs

```csharp
using Services.Chatbot;
using Repositories.ChatSessions;

// Configure Gemini AI
builder.Services.Configure<GeminiAIOptions>(options =>
{
    builder.Configuration.GetSection("GeminiAI").Bind(options);
    
    // Override with environment variables if present
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (!string.IsNullOrEmpty(apiKey))
    {
        options.ApiKey = apiKey;
    }
});

// Configure Chatbot options
builder.Services.Configure<ChatbotOptions>(options =>
{
    builder.Configuration.GetSection("Chatbot").Bind(options);
});

// Register services
builder.Services.AddHttpClient<IGeminiApiClient, GeminiApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
builder.Services.AddSingleton<IChatbotMetricsService, ChatbotMetricsService>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
```

#### Add Configuration to appsettings.json

```json
{
  "GeminiAI": {
    "ApiKey": "your-gemini-api-key-here",
    "ApiBaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "ModelName": "gemini-2.0-flash-exp",
    "Temperature": 0.7,
    "MaxTokens": 2048
  },
  "Chatbot": {
    "RateLimitRequests": 15,
    "RateLimitWindowSeconds": 60,
    "RequestTimeoutSeconds": 30,
    "RetryMaxAttempts": 3
  }
}
```

#### Environment Variables (Recommended for Production)
```bash
GEMINI_API_KEY=your-api-key-here
GEMINI_MODEL_NAME=gemini-2.0-flash-exp
GEMINI_API_BASE_URL=https://generativelanguage.googleapis.com/v1beta
GEMINI_MAX_TOKENS=2048
GEMINI_TEMPERATURE=0.7
CHATBOT_RATE_LIMIT_REQUESTS=15
CHATBOT_RATE_LIMIT_WINDOW_SECONDS=60
CHATBOT_REQUEST_TIMEOUT_SECONDS=30
CHATBOT_RETRY_MAX_ATTEMPTS=3
```

### 2. Frontend Setup

#### Copy Files
1. Copy `Frontend/js/chatbot.js` to your `wwwroot/js/` folder
2. Copy `Frontend/css/chatbot.css` to your `wwwroot/css/` folder

#### Add to Your Layout or Page

```html
<!-- In your _Layout.cshtml or specific page -->
<link rel="stylesheet" href="~/css/chatbot.css" asp-append-version="true" />

<!-- Chatbot Button -->
<button id="chatbotToggle" class="chatbot-toggle" aria-label="Open Chatbot">
    <i class="bi bi-chat-dots"></i>
</button>

<!-- Chatbot Container -->
<div id="chatbotContainer" class="chatbot-container"></div>

<!-- Scripts -->
<script src="~/js/chatbot.js" asp-append-version="true"></script>
<script>
    // Initialize chatbot
    document.addEventListener('DOMContentLoaded', function() {
        if (typeof initChatbot === 'function') {
            initChatbot();
        }
    });
</script>
```

## API Endpoints

### Chat Endpoints

#### POST /api/chatbot/chat
Send a message to the chatbot

**Request:**
```json
{
  "message": "What clubs should I join?",
  "sessionId": 123
}
```

**Response:**
```json
{
  "message": "Based on your profile...",
  "sessionId": 123,
  "intent": "ClubRecommendation",
  "recommendations": [
    {
      "id": 1,
      "name": "FPT Code Club",
      "type": "Club",
      "reason": "Matches your Software Engineering major",
      "relevanceScore": 0.95
    }
  ]
}
```

#### GET /api/chatbot/sessions
Get user's chat sessions

**Response:**
```json
[
  {
    "id": 123,
    "title": "Club Recommendations",
    "lastMessageAt": "2025-12-01T10:30:00Z",
    "messageCount": 5
  }
]
```

#### GET /api/chatbot/sessions/{sessionId}/history
Get chat history for a session

**Response:**
```json
{
  "sessionId": 123,
  "messages": [
    {
      "role": "user",
      "content": "What clubs should I join?",
      "createdAt": "2025-12-01T10:30:00Z"
    },
    {
      "role": "assistant",
      "content": "Based on your profile...",
      "createdAt": "2025-12-01T10:30:05Z"
    }
  ]
}
```

#### DELETE /api/chatbot/sessions/{sessionId}
Delete a chat session

### Admin Endpoints

#### GET /api/chatbotmetrics
Get chatbot metrics (Admin only)

**Response:**
```json
{
  "totalRequests": 1250,
  "successfulRequests": 1200,
  "failedRequests": 50,
  "rateLimitHits": 25,
  "averageResponseTime": 1.5,
  "uptime": "5.12:30:45"
}
```

#### POST /api/chatbotmetrics/reset
Reset metrics (Admin only)

## Usage Examples

### Basic Chat
```javascript
// Send a message
const response = await fetch('https://localhost:5001/api/chatbot/chat', {
    method: 'POST',
    credentials: 'include',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        message: 'Hello!',
        sessionId: null // null for new session
    })
});

const data = await response.json();
console.log(data.message); // AI response
console.log(data.sessionId); // Session ID for follow-up messages
```

### Get Recommendations
```javascript
const response = await fetch('https://localhost:5001/api/chatbot/chat', {
    method: 'POST',
    credentials: 'include',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        message: 'What clubs should I join?',
        sessionId: 123
    })
});

const data = await response.json();
if (data.recommendations && data.recommendations.length > 0) {
    data.recommendations.forEach(rec => {
        console.log(`${rec.name}: ${rec.reason}`);
    });
}
```

## Customization

### Modify System Prompt
Edit `Services/Chatbot/GeminiApiClient.cs` - `BuildSystemPrompt()` method to customize the AI's behavior and personality.

### Adjust Rate Limits
Modify `Middleware/ChatbotRateLimitAttribute.cs`:
```csharp
private const int MaxRequests = 15; // Change this
private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1); // Change this
```

### Change Recommendation Logic
Edit `Services/Recommendations/RecommendationEngine.cs` to customize how recommendations are generated.

### Customize UI
Edit `Frontend/css/chatbot.css` to match your application's design system.

## Dependencies

### Backend
- ASP.NET Core 8.0+
- Entity Framework Core
- System.IdentityModel.Tokens.Jwt
- Microsoft.Extensions.Http

### Frontend
- Bootstrap Icons (for chat icon)
- Modern browser with ES6+ support

## Security Considerations

1. **API Key Protection**: Never commit API keys to source control. Use environment variables or Azure Key Vault.
2. **Rate Limiting**: Adjust rate limits based on your Gemini AI quota.
3. **Authentication**: All endpoints require authentication except metrics (Admin only).
4. **Input Validation**: User messages are validated and sanitized.
5. **CORS**: Configure CORS appropriately for your frontend domain.

## Troubleshooting

### Chatbot not responding
- Check Gemini API key is valid
- Verify API quota hasn't been exceeded
- Check network connectivity
- Review logs for error messages

### Rate limit errors
- Increase rate limit in configuration
- Check if multiple users are sharing the same session
- Review metrics to identify usage patterns

### Recommendations not showing
- Verify student profile data is complete
- Check if clubs/activities exist in database
- Review recommendation engine logic

## Performance Tips

1. **Caching**: Consider caching frequently requested recommendations
2. **Database Indexing**: Add indexes on ChatSession.UserId and ChatMessage.SessionId
3. **Connection Pooling**: Configure HTTP client with appropriate timeout and retry policies
4. **Monitoring**: Use ChatbotMetricsService to track performance

## License
This module is part of the EduXtend project.

## Support
For issues or questions, please contact the development team.
