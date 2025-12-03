# AI Chatbot Assistant - Configuration Guide

## Overview

The AI Chatbot Assistant is an intelligent feature that helps students discover clubs and activities that match their interests, major, and goals. It uses Google's Gemini AI to provide personalized recommendations through an interactive chat interface.

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server database (existing EduXtend database)
- Google Gemini API key
- Active internet connection for API calls

## Getting Started

### 1. Obtain Gemini API Key

To use the AI Chatbot Assistant, you need a Gemini API key from Google:

1. **Visit Google AI Studio**
   - Go to [https://aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey)
   - Sign in with your Google account

2. **Create API Key**
   - Click "Create API Key" button
   - Select an existing Google Cloud project or create a new one
   - Click "Create API key in existing project" or "Create API key in new project"

3. **Copy Your API Key**
   - Copy the generated API key (format: `AIza...`)
   - Store it securely - you'll need it for configuration

4. **Important Notes**
   - Keep your API key confidential - never commit it to version control
   - Free tier includes generous quotas for testing and development
   - Monitor your usage at [https://aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey)
   - Review pricing at [https://ai.google.dev/pricing](https://ai.google.dev/pricing)

### 2. Configure appsettings.json

Add the Gemini AI configuration to your `WebAPI/appsettings.json` file:

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

#### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApiKey` | string | *Required* | Your Gemini API key from Google AI Studio |
| `Model` | string | `gemini-1.5-flash` | Gemini model to use. Options: `gemini-1.5-flash` (faster, cheaper) or `gemini-1.5-pro` (more capable) |
| `Temperature` | double | `0.7` | Controls randomness (0.0-1.0). Lower = more focused, Higher = more creative |
| `MaxTokens` | int | `1024` | Maximum tokens in AI response. Typical responses use 200-500 tokens |
| `ApiEndpoint` | string | `https://generativelanguage.googleapis.com/v1beta/models` | Gemini API base URL (rarely needs changing) |

#### Model Selection Guide

- **gemini-1.5-flash** (Recommended for production)
  - Faster response times (1-3 seconds)
  - Lower cost per request
  - Suitable for most chatbot interactions
  - Best for high-volume usage

- **gemini-1.5-pro**
  - More sophisticated responses
  - Better at complex reasoning
  - Higher cost per request
  - Use for enhanced user experience

#### Temperature Guide

- **0.0-0.3**: Very focused and deterministic responses
- **0.4-0.7**: Balanced creativity and consistency (recommended)
- **0.8-1.0**: More creative and varied responses

### 3. Development Environment Setup

For local development, you can use `appsettings.Development.json`:

```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_DEV_API_KEY",
    "Model": "gemini-1.5-flash",
    "Temperature": 0.7,
    "MaxTokens": 1024
  },
  "Logging": {
    "LogLevel": {
      "Services.Chatbot": "Debug",
      "WebAPI.Controllers.ChatbotController": "Debug"
    }
  }
}
```

**Important**: Add `appsettings.Development.json` to `.gitignore` to prevent committing API keys.

### 4. Production Deployment

For production environments, **never store API keys in appsettings.json**. Use environment variables or secure key management services.

#### Option A: Environment Variables (Recommended)

Set environment variables on your production server:

**Windows (PowerShell)**
```powershell
$env:GeminiAI__ApiKey = "YOUR_PRODUCTION_API_KEY"
$env:GeminiAI__Model = "gemini-1.5-flash"
$env:GeminiAI__Temperature = "0.7"
$env:GeminiAI__MaxTokens = "1024"
```

**Linux/macOS (Bash)**
```bash
export GeminiAI__ApiKey="YOUR_PRODUCTION_API_KEY"
export GeminiAI__Model="gemini-1.5-flash"
export GeminiAI__Temperature="0.7"
export GeminiAI__MaxTokens="1024"
```

**Docker**
```dockerfile
ENV GeminiAI__ApiKey="YOUR_PRODUCTION_API_KEY"
ENV GeminiAI__Model="gemini-1.5-flash"
ENV GeminiAI__Temperature="0.7"
ENV GeminiAI__MaxTokens="1024"
```

**Azure App Service**
1. Go to Azure Portal → Your App Service
2. Navigate to Configuration → Application settings
3. Add new application settings:
   - Name: `GeminiAI__ApiKey`, Value: `YOUR_API_KEY`
   - Name: `GeminiAI__Model`, Value: `gemini-1.5-flash`
   - Name: `GeminiAI__Temperature`, Value: `0.7`
   - Name: `GeminiAI__MaxTokens`, Value: `1024`

#### Option B: Azure Key Vault (Most Secure)

1. **Create Azure Key Vault**
   ```bash
   az keyvault create --name eduxtend-keyvault --resource-group eduxtend-rg --location eastus
   ```

2. **Store API Key**
   ```bash
   az keyvault secret set --vault-name eduxtend-keyvault --name GeminiAI-ApiKey --value "YOUR_API_KEY"
   ```

3. **Configure App Service to Use Key Vault**
   - Enable Managed Identity for your App Service
   - Grant Key Vault access to the Managed Identity
   - Reference secrets in Application Settings:
     ```
     @Microsoft.KeyVault(SecretUri=https://eduxtend-keyvault.vault.azure.net/secrets/GeminiAI-ApiKey/)
     ```

#### Production appsettings.Production.json

```json
{
  "GeminiAI": {
    "Model": "gemini-1.5-flash",
    "Temperature": 0.7,
    "MaxTokens": 1024,
    "ApiEndpoint": "https://generativelanguage.googleapis.com/v1beta/models"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Services.Chatbot": "Warning",
      "WebAPI.Controllers.ChatbotController": "Warning"
    }
  }
}
```

Note: `ApiKey` is intentionally omitted - it will be loaded from environment variables or Key Vault.

### 5. Rate Limiting Configuration

The chatbot includes rate limiting to prevent abuse and control API costs. Configuration is in `WebAPI/Program.cs`.

#### Default Rate Limits

- **10 requests per minute** per authenticated user
- **100 requests per hour** per authenticated user
- Applies to all `/api/chatbot/*` endpoints

#### Customizing Rate Limits

To modify rate limits, update the configuration in `Program.cs`:

```csharp
// Configure rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/chatbot/*",
            Period = "1m",
            Limit = 10  // Change this value
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/chatbot/*",
            Period = "1h",
            Limit = 100  // Change this value
        }
    };
});
```

#### Rate Limit Response

When rate limit is exceeded, users receive:
- **HTTP Status**: 429 Too Many Requests
- **Error Message**: "AI Assistant tạm thời quá tải. Vui lòng thử lại sau ít phút."

#### Production Recommendations

- **Development**: 10 req/min, 100 req/hour (current default)
- **Production (Small)**: 20 req/min, 500 req/hour
- **Production (Large)**: 50 req/min, 2000 req/hour

Adjust based on:
- Number of concurrent users
- Gemini API quota limits
- Budget constraints
- Expected usage patterns

### 6. Monitoring and Logging

#### Enable Detailed Logging

Add to `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Services.Chatbot.GeminiAIService": "Debug",
      "Services.Chatbot.ChatbotService": "Debug",
      "WebAPI.Controllers.ChatbotController": "Debug"
    }
  }
}
```

#### Key Metrics to Monitor

1. **API Usage**
   - Total requests per day/month
   - Token consumption
   - Cost tracking

2. **Performance**
   - Average response time
   - 95th percentile response time
   - Timeout rate

3. **Errors**
   - API key errors
   - Rate limit hits
   - Quota exceeded events
   - Network timeouts

4. **User Engagement**
   - Active users
   - Messages per session
   - Most common queries

#### Logging Examples

The system logs important events:

```
[INFO] ChatbotController: Processing chat message for student 12345
[DEBUG] ChatbotService: Building student context for student 12345
[DEBUG] ChatbotService: Retrieved 5 relevant clubs and 3 upcoming activities
[DEBUG] GeminiAIService: Sending request to Gemini API (prompt length: 1250 chars)
[INFO] GeminiAIService: Received response from Gemini API (tokens: 342, duration: 2.3s)
[INFO] ChatbotController: Successfully returned AI response to client
```

### 7. Testing the Configuration

#### Verify Configuration

1. **Start the application**
   ```bash
   cd WebAPI
   dotnet run
   ```

2. **Check logs for configuration errors**
   - Look for "GeminiAI configuration loaded successfully"
   - Watch for "Invalid API key" or "Missing configuration" errors

3. **Test the chatbot endpoint**
   ```bash
   curl -X POST https://localhost:5001/api/chatbot/message \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -d '{"message": "Xin chào"}'
   ```

#### Common Configuration Issues

| Issue | Symptom | Solution |
|-------|---------|----------|
| Invalid API Key | 401 Unauthorized from Gemini | Verify API key is correct and active |
| Missing Configuration | Application fails to start | Ensure GeminiAI section exists in appsettings.json |
| Quota Exceeded | 429 from Gemini API | Check quota limits in Google AI Studio |
| Network Timeout | Requests take >30 seconds | Check internet connection and firewall settings |
| Rate Limit Hit | 429 from WebAPI | Wait 1 minute or adjust rate limits |

### 8. Cost Management

#### Estimating Costs

Gemini API pricing (as of 2024):
- **gemini-1.5-flash**: $0.075 per 1M input tokens, $0.30 per 1M output tokens
- **gemini-1.5-pro**: $1.25 per 1M input tokens, $5.00 per 1M output tokens

Typical chatbot interaction:
- Input: ~1,500 tokens (student context + message)
- Output: ~300 tokens (AI response)

**Cost per interaction**:
- Flash: ~$0.0002 per message
- Pro: ~$0.003 per message

**Monthly estimates** (1000 active students, 5 messages/student/month):
- Flash: ~$1.00/month
- Pro: ~$15.00/month

#### Cost Optimization Tips

1. **Use gemini-1.5-flash** for most interactions
2. **Implement caching** for student context and club data
3. **Limit conversation history** to last 10 messages
4. **Set appropriate MaxTokens** to avoid unnecessarily long responses
5. **Monitor usage** regularly in Google AI Studio
6. **Set up billing alerts** in Google Cloud Console

### 9. Security Best Practices

✅ **DO**
- Store API keys in environment variables or Key Vault
- Use HTTPS for all API communications
- Implement rate limiting
- Log API usage for audit trails
- Rotate API keys periodically (every 90 days)
- Use separate API keys for dev/staging/production

❌ **DON'T**
- Commit API keys to version control
- Expose API keys in client-side code
- Share API keys between environments
- Log API keys in application logs
- Use the same API key across multiple projects

### 10. Troubleshooting

#### Problem: "Invalid API key" error

**Solution**:
1. Verify API key is correctly copied (no extra spaces)
2. Check API key is enabled in Google AI Studio
3. Ensure billing is enabled for your Google Cloud project
4. Try generating a new API key

#### Problem: Slow response times (>10 seconds)

**Solution**:
1. Switch from `gemini-1.5-pro` to `gemini-1.5-flash`
2. Reduce `MaxTokens` to 512 or 768
3. Implement caching for student context
4. Check network latency to Google APIs

#### Problem: Rate limit errors

**Solution**:
1. Increase rate limits in `Program.cs`
2. Implement request queuing on frontend
3. Add user feedback about rate limits
4. Consider upgrading Gemini API quota

#### Problem: Chatbot not appearing in UI

**Solution**:
1. Verify user is authenticated
2. Check user has "Student" role
3. Ensure `chatbot.js` and `chatbot.css` are loaded
4. Check browser console for JavaScript errors

## Support

For issues or questions:
- Check application logs in `WebAPI/logs/`
- Review Gemini API status: [https://status.cloud.google.com/](https://status.cloud.google.com/)
- Contact development team

## Additional Resources

- [Gemini API Documentation](https://ai.google.dev/docs)
- [Gemini API Pricing](https://ai.google.dev/pricing)
- [Google AI Studio](https://aistudio.google.com/)
- [Rate Limiting Best Practices](https://cloud.google.com/architecture/rate-limiting-strategies-techniques)
