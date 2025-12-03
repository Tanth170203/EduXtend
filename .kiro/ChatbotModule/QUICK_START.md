# Quick Start Guide

## 5-Minute Setup

### 1. Get Gemini API Key
Visit [Google AI Studio](https://makersuite.google.com/app/apikey) and create an API key.

### 2. Copy Files
```bash
# Copy all files from ChatbotModule to your project
cp -r ChatbotModule/Backend/* YourProject/
cp -r ChatbotModule/Frontend/js/* YourProject/WebFE/wwwroot/js/
cp -r ChatbotModule/Frontend/css/* YourProject/WebFE/wwwroot/css/
```

### 3. Add to DbContext
```csharp
public DbSet<ChatSession> ChatSessions { get; set; }
public DbSet<ChatMessage> ChatMessages { get; set; }
```

### 4. Run Migration
```bash
dotnet ef migrations add AddChatbot
dotnet ef database update
```

### 5. Configure in Program.cs
```csharp
// Add these lines
builder.Services.Configure<GeminiAIOptions>(builder.Configuration.GetSection("GeminiAI"));
builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));
builder.Services.AddHttpClient<IGeminiApiClient, GeminiApiClient>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
builder.Services.AddSingleton<IChatbotMetricsService, ChatbotMetricsService>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
```

### 6. Add to appsettings.json
```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "ModelName": "gemini-2.0-flash-exp"
  },
  "Chatbot": {
    "RateLimitRequests": 15
  }
}
```

### 7. Add to Layout
```html
<link rel="stylesheet" href="~/css/chatbot.css" />
<button id="chatbotToggle" class="chatbot-toggle">💬</button>
<div id="chatbotContainer" class="chatbot-container"></div>
<script src="~/js/chatbot.js"></script>
<script>initChatbot();</script>
```

### 8. Run & Test
```bash
dotnet run
```

Open your app and click the chat button!

## That's it! 🎉

For detailed instructions, see [MIGRATION_GUIDE.md](Documentation/MIGRATION_GUIDE.md)
