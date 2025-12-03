# Configuration Examples

## appsettings.json

### Development Environment
```json
{
  "GeminiAI": {
    "ApiKey": "AIzaSy...",
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

### Production Environment (appsettings.Production.json)
```json
{
  "GeminiAI": {
    "ApiKey": "",
    "ApiBaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "ModelName": "gemini-2.0-flash-exp",
    "Temperature": 0.7,
    "MaxTokens": 2048
  },
  "Chatbot": {
    "RateLimitRequests": 10,
    "RateLimitWindowSeconds": 60,
    "RequestTimeoutSeconds": 30,
    "RetryMaxAttempts": 3
  }
}
```

**Note:** In production, use environment variables for sensitive data like API keys.

## Environment Variables

### Windows (PowerShell)
```powershell
$env:GEMINI_API_KEY="your-api-key-here"
$env:GEMINI_MODEL_NAME="gemini-2.0-flash-exp"
$env:GEMINI_API_BASE_URL="https://generativelanguage.googleapis.com/v1beta"
$env:GEMINI_MAX_TOKENS="2048"
$env:GEMINI_TEMPERATURE="0.7"
$env:CHATBOT_RATE_LIMIT_REQUESTS="15"
$env:CHATBOT_RATE_LIMIT_WINDOW_SECONDS="60"
$env:CHATBOT_REQUEST_TIMEOUT_SECONDS="30"
$env:CHATBOT_RETRY_MAX_ATTEMPTS="3"
```

### Linux/Mac (Bash)
```bash
export GEMINI_API_KEY="your-api-key-here"
export GEMINI_MODEL_NAME="gemini-2.0-flash-exp"
export GEMINI_API_BASE_URL="https://generativelanguage.googleapis.com/v1beta"
export GEMINI_MAX_TOKENS="2048"
export GEMINI_TEMPERATURE="0.7"
export CHATBOT_RATE_LIMIT_REQUESTS="15"
export CHATBOT_RATE_LIMIT_WINDOW_SECONDS="60"
export CHATBOT_REQUEST_TIMEOUT_SECONDS="30"
export CHATBOT_RETRY_MAX_ATTEMPTS="3"
```

### Docker (.env file)
```env
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

### Azure App Service
Go to: Configuration → Application settings

Add these settings:
- `GEMINI_API_KEY`: your-api-key-here
- `GEMINI_MODEL_NAME`: gemini-2.0-flash-exp
- `GEMINI_API_BASE_URL`: https://generativelanguage.googleapis.com/v1beta
- `GEMINI_MAX_TOKENS`: 2048
- `GEMINI_TEMPERATURE`: 0.7
- `CHATBOT_RATE_LIMIT_REQUESTS`: 15
- `CHATBOT_RATE_LIMIT_WINDOW_SECONDS`: 60
- `CHATBOT_REQUEST_TIMEOUT_SECONDS`: 30
- `CHATBOT_RETRY_MAX_ATTEMPTS`: 3

## Configuration Options Explained

### GeminiAI Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ApiKey | string | - | Your Google Gemini API key (required) |
| ApiBaseUrl | string | https://generativelanguage.googleapis.com/v1beta | Gemini API base URL |
| ModelName | string | gemini-2.0-flash-exp | Gemini model to use |
| Temperature | double | 0.7 | Controls randomness (0.0-1.0). Higher = more creative |
| MaxTokens | int | 2048 | Maximum tokens in response |

### Chatbot Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| RateLimitRequests | int | 15 | Max requests per window per user |
| RateLimitWindowSeconds | int | 60 | Rate limit window duration in seconds |
| RequestTimeoutSeconds | int | 30 | HTTP request timeout |
| RetryMaxAttempts | int | 3 | Max retry attempts on failure |

## Recommended Settings by Environment

### Development
- Temperature: 0.7 (balanced)
- MaxTokens: 2048 (generous)
- RateLimitRequests: 15 (lenient for testing)
- RequestTimeoutSeconds: 30

### Staging
- Temperature: 0.7
- MaxTokens: 2048
- RateLimitRequests: 12
- RequestTimeoutSeconds: 30

### Production
- Temperature: 0.7
- MaxTokens: 1024 (cost-effective)
- RateLimitRequests: 10 (protect API quota)
- RequestTimeoutSeconds: 20 (faster timeout)

## Getting a Gemini API Key

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the API key
5. Add it to your configuration

**Important:** 
- Never commit API keys to source control
- Use environment variables in production
- Rotate keys regularly
- Monitor usage in Google Cloud Console

## Testing Configuration

### Test API Key
```bash
curl -X POST "https://localhost:5001/api/chatbot/chat" \
  -H "Content-Type: application/json" \
  -H "Cookie: AccessToken=YOUR_TOKEN" \
  -d '{"message":"Hello","sessionId":null}'
```

### Test Rate Limiting
```bash
# Send 20 requests quickly
for i in {1..20}; do
  curl -X POST "https://localhost:5001/api/chatbot/chat" \
    -H "Content-Type: application/json" \
    -H "Cookie: AccessToken=YOUR_TOKEN" \
    -d '{"message":"Test '$i'","sessionId":null}'
done
```

### Check Metrics
```bash
curl -X GET "https://localhost:5001/api/chatbotmetrics" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```
