# Migration Guide - Integrating Chatbot Module into Your Project

## Prerequisites
- ASP.NET Core 8.0+ project
- Entity Framework Core configured
- SQL Server or compatible database
- Google Gemini API key

## Step-by-Step Integration

### Step 1: Copy Files

#### Backend Files
```bash
# From ChatbotModule/Backend/ to your project:

# Services
Copy Services/Chatbot/* → YourProject/Services/Chatbot/
Copy Services/Recommendations/* → YourProject/Services/Recommendations/

# Repositories
Copy Repositories/ChatSessions/* → YourProject/Repositories/ChatSessions/

# Controllers
Copy Controllers/ChatbotController.cs → YourProject/WebAPI/Controllers/
Copy Controllers/ChatbotMetricsController.cs → YourProject/WebAPI/Controllers/

# Middleware
Copy Middleware/ChatbotRateLimitAttribute.cs → YourProject/WebAPI/Middleware/

# Configuration
Copy Configuration/ChatbotOptions.cs → YourProject/WebAPI/Configuration/

# Models
Copy Models/ChatSession.cs → YourProject/BusinessObject/Models/
Copy Models/ChatMessage.cs → YourProject/BusinessObject/Models/

# DTOs
Copy DTOs/Chatbot/* → YourProject/BusinessObject/DTOs/Chatbot/
```

#### Frontend Files
```bash
# From ChatbotModule/Frontend/ to your project:

Copy js/chatbot.js → YourProject/WebFE/wwwroot/js/
Copy css/chatbot.css → YourProject/WebFE/wwwroot/css/
```

### Step 2: Update DbContext

Add these DbSets to your ApplicationDbContext:

```csharp
public class ApplicationDbContext : DbContext
{
    // ... existing DbSets ...
    
    // Chatbot tables
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // ... existing configurations ...
        
        // ChatSession configuration
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
        
        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
```

### Step 3: Create Database Migration

```bash
# Navigate to your project directory
cd YourProject

# Create migration
dotnet ef migrations add AddChatbotTables --project DataAccess --startup-project WebAPI

# Apply migration
dotnet ef database update --project DataAccess --startup-project WebAPI
```

### Step 4: Configure Services in Program.cs

Add these configurations to your `Program.cs`:

```csharp
using Services.Chatbot;
using Services.Recommendations;
using Repositories.ChatSessions;
using WebAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ... existing configurations ...

// ============================================
// CHATBOT MODULE CONFIGURATION
// ============================================

// Configure Gemini AI Options
builder.Services.Configure<GeminiAIOptions>(options =>
{
    builder.Configuration.GetSection("GeminiAI").Bind(options);
    
    // Override with environment variables (recommended for production)
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (!string.IsNullOrEmpty(apiKey))
    {
        options.ApiKey = apiKey;
    }
    
    var modelName = Environment.GetEnvironmentVariable("GEMINI_MODEL_NAME");
    if (!string.IsNullOrEmpty(modelName))
    {
        options.ModelName = modelName;
    }
    
    var apiBaseUrl = Environment.GetEnvironmentVariable("GEMINI_API_BASE_URL");
    if (!string.IsNullOrEmpty(apiBaseUrl))
    {
        options.ApiBaseUrl = apiBaseUrl;
    }
    
    var maxTokens = Environment.GetEnvironmentVariable("GEMINI_MAX_TOKENS");
    if (!string.IsNullOrEmpty(maxTokens) && int.TryParse(maxTokens, out var maxTokensValue))
    {
        options.MaxTokens = maxTokensValue;
    }
    
    var temperature = Environment.GetEnvironmentVariable("GEMINI_TEMPERATURE");
    if (!string.IsNullOrEmpty(temperature) && double.TryParse(temperature, out var temperatureValue))
    {
        options.Temperature = temperatureValue;
    }
});

// Configure Chatbot Options
builder.Services.Configure<ChatbotOptions>(options =>
{
    builder.Configuration.GetSection("Chatbot").Bind(options);
    
    // Override with environment variables
    var rateLimitRequests = Environment.GetEnvironmentVariable("CHATBOT_RATE_LIMIT_REQUESTS");
    if (!string.IsNullOrEmpty(rateLimitRequests) && int.TryParse(rateLimitRequests, out var rateLimitRequestsValue))
    {
        options.RateLimitRequests = rateLimitRequestsValue;
    }
    
    var rateLimitWindowSeconds = Environment.GetEnvironmentVariable("CHATBOT_RATE_LIMIT_WINDOW_SECONDS");
    if (!string.IsNullOrEmpty(rateLimitWindowSeconds) && int.TryParse(rateLimitWindowSeconds, out var rateLimitWindowSecondsValue))
    {
        options.RateLimitWindowSeconds = rateLimitWindowSecondsValue;
    }
    
    var requestTimeoutSeconds = Environment.GetEnvironmentVariable("CHATBOT_REQUEST_TIMEOUT_SECONDS");
    if (!string.IsNullOrEmpty(requestTimeoutSeconds) && int.TryParse(requestTimeoutSeconds, out var requestTimeoutSecondsValue))
    {
        options.RequestTimeoutSeconds = requestTimeoutSecondsValue;
    }
    
    var retryMaxAttempts = Environment.GetEnvironmentVariable("CHATBOT_RETRY_MAX_ATTEMPTS");
    if (!string.IsNullOrEmpty(retryMaxAttempts) && int.TryParse(retryMaxAttempts, out var retryMaxAttemptsValue))
    {
        options.RetryMaxAttempts = retryMaxAttemptsValue;
    }
});

// Register Chatbot Services
builder.Services.AddHttpClient<IGeminiApiClient, GeminiApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddSingleton<IChatbotMetricsService, ChatbotMetricsService>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();

// ============================================
// END CHATBOT MODULE CONFIGURATION
// ============================================

var app = builder.Build();

// ... rest of your configuration ...
```

### Step 5: Add Configuration to appsettings.json

Add these sections to your `appsettings.json`:

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

### Step 6: Set Environment Variables (Production)

For production, use environment variables instead of hardcoding API keys:

```bash
# Windows
set GEMINI_API_KEY=your-api-key-here
set GEMINI_MODEL_NAME=gemini-2.0-flash-exp

# Linux/Mac
export GEMINI_API_KEY=your-api-key-here
export GEMINI_MODEL_NAME=gemini-2.0-flash-exp
```

Or in Azure App Service:
- Go to Configuration → Application settings
- Add new settings for each environment variable

### Step 7: Add Frontend Integration

#### Option A: Add to Layout (Global)

In your `_Layout.cshtml`:

```html
<!DOCTYPE html>
<html>
<head>
    <!-- ... existing head content ... -->
    <link rel="stylesheet" href="~/css/chatbot.css" asp-append-version="true" />
</head>
<body>
    <!-- ... existing body content ... -->
    
    <!-- Chatbot Button (Fixed position) -->
    <button id="chatbotToggle" class="chatbot-toggle" aria-label="Open Chatbot">
        <i class="bi bi-chat-dots"></i>
    </button>
    
    <!-- Chatbot Container -->
    <div id="chatbotContainer" class="chatbot-container"></div>
    
    <!-- ... existing scripts ... -->
    <script src="~/js/chatbot.js" asp-append-version="true"></script>
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            if (typeof initChatbot === 'function') {
                initChatbot();
            }
        });
    </script>
</body>
</html>
```

#### Option B: Add to Specific Pages

In specific `.cshtml` pages:

```html
@section Styles {
    <link rel="stylesheet" href="~/css/chatbot.css" asp-append-version="true" />
}

<!-- Page content -->

@section Scripts {
    <script src="~/js/chatbot.js" asp-append-version="true"></script>
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            if (typeof initChatbot === 'function') {
                initChatbot();
            }
        });
    </script>
}
```

### Step 8: Verify Installation

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the project:**
   ```bash
   dotnet run
   ```

3. **Test the chatbot:**
   - Open your application in a browser
   - Click the chatbot button (bottom-right corner)
   - Send a test message
   - Verify you get a response

4. **Check metrics (Admin only):**
   ```bash
   curl -X GET https://localhost:5001/api/chatbotmetrics \
     -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
   ```

## Customization

### Customize System Prompt

Edit `Services/Chatbot/GeminiApiClient.cs`:

```csharp
private string BuildSystemPrompt(Student student)
{
    var prompt = new StringBuilder();
    prompt.AppendLine("=== YOUR CUSTOM SYSTEM PROMPT ===");
    prompt.AppendLine();
    prompt.AppendLine($"Student: {student.User?.FullName}");
    // ... customize as needed ...
    return prompt.ToString();
}
```

### Customize Recommendations

Edit `Services/Recommendations/RecommendationEngine.cs`:

```csharp
public async Task<List<RecommendationDto>> GenerateRecommendationsAsync(int studentId, string intent)
{
    // Customize recommendation logic here
}
```

### Customize UI

Edit `wwwroot/css/chatbot.css` to match your design system.

## Troubleshooting

### Issue: "Table 'ChatSessions' doesn't exist"
**Solution:** Run the database migration:
```bash
dotnet ef database update
```

### Issue: "Gemini API key is not configured"
**Solution:** Check your appsettings.json or environment variables

### Issue: "Rate limit exceeded"
**Solution:** Adjust rate limits in ChatbotOptions or wait for the window to reset

### Issue: Chatbot button not showing
**Solution:** 
- Verify chatbot.css is loaded
- Check browser console for errors
- Ensure Bootstrap Icons is loaded

### Issue: No recommendations returned
**Solution:**
- Verify student profile data exists
- Check if clubs/activities exist in database
- Review logs for errors

## Next Steps

1. **Customize the AI personality** by editing the system prompt
2. **Add more intents** in ChatbotService
3. **Enhance recommendations** in RecommendationEngine
4. **Monitor usage** via ChatbotMetricsController
5. **Adjust rate limits** based on your API quota

## Support

For issues or questions:
1. Check the main README.md
2. Review error logs
3. Contact the development team
