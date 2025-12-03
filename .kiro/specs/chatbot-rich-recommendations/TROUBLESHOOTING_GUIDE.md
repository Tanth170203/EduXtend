# Troubleshooting Guide - Chatbot Rich Recommendations

## Overview

This guide helps you diagnose and fix common issues with the chatbot rich recommendations feature. Issues are organized by symptom with step-by-step solutions.

## Table of Contents

1. [Recommendations Not Displaying](#recommendations-not-displaying)
2. [Plain Text Instead of Cards](#plain-text-instead-of-cards)
3. [Malformed or Missing Data](#malformed-or-missing-data)
4. [Styling Issues](#styling-issues)
5. [Navigation Problems](#navigation-problems)
6. [Performance Issues](#performance-issues)
7. [Mobile Display Issues](#mobile-display-issues)
8. [Accessibility Issues](#accessibility-issues)
9. [Backend Errors](#backend-errors)
10. [Logging and Debugging](#logging-and-debugging)

---

## Recommendations Not Displaying

### Symptom
User sends a recommendation request but sees no cards or blank response.

### Possible Causes

#### 1. Frontend JavaScript Error

**Check:**
- Open browser DevTools (F12)
- Look for JavaScript errors in Console tab

**Solution:**
```javascript
// Verify chatbot.js is loaded
console.log(typeof detectMessageType); // Should be 'function'
console.log(typeof renderRecommendationCard); // Should be 'function'
```

**Fix:**
- Ensure `chatbot.js` is included in the page
- Check for syntax errors in JavaScript
- Verify no conflicting scripts

#### 2. CSS File Not Loaded

**Check:**
- Open DevTools Network tab
- Look for `recommendation-cards.css`
- Check if it returns 404 or 500

**Solution:**
```html
<!-- Verify in _Layout.cshtml -->
<link rel="stylesheet" href="~/css/recommendation-cards.css" asp-append-version="true" />
```

**Fix:**
- Ensure CSS file exists at `WebFE/wwwroot/css/recommendation-cards.css`
- Clear browser cache
- Rebuild and restart application

#### 3. API Response Not Received

**Check:**
- Open DevTools Network tab
- Find the `/api/chatbot/message` request
- Check response status and body

**Solution:**
```javascript
// Add logging in chatbot.js
async function sendMessage(message) {
    try {
        const response = await fetch('/api/chatbot/message', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ message, sessionId: currentSessionId })
        });
        
        console.log('Response status:', response.status);
        const data = await response.json();
        console.log('Response data:', data);
        
        // ... rest of code
    } catch (error) {
        console.error('API Error:', error);
    }
}
```

**Fix:**
- Check if API endpoint is accessible
- Verify authentication (user logged in)
- Check rate limiting (max 15 req/min)

---

## Plain Text Instead of Cards

### Symptom
AI returns plain text response instead of structured JSON with recommendations.

### Possible Causes

#### 1. AI Not Returning JSON Format

**Check Backend Logs:**
```
[Warning] Failed to parse structured response: Unexpected character...
```

**Solution:**
Check the prompt being sent to Gemini AI:

```csharp
// Add logging in ChatbotService.cs
_logger.LogInformation("Prompt sent to AI: {Prompt}", 
    prompt.Substring(0, Math.Min(500, prompt.Length)));
```

**Fix:**
- Verify `BuildStructuredPrompt` includes JSON schema instructions
- Ensure prompt clearly requests JSON format
- Check if Gemini AI model supports structured output

#### 2. JSON Parsing Failed

**Check Backend Logs:**
```
[Warning] Failed to parse structured response: ...
```

**Solution:**
Add detailed logging in `ParseStructuredResponse`:

```csharp
private (bool isStructured, StructuredResponse? data, string plainText) 
    ParseStructuredResponse(string aiResponse)
{
    _logger.LogDebug("AI Response: {Response}", aiResponse);
    
    try
    {
        var jsonMatch = Regex.Match(aiResponse, 
            @"```json\s*(\{.*?\})\s*```", 
            RegexOptions.Singleline);
        
        if (jsonMatch.Success)
        {
            _logger.LogDebug("JSON extracted: {Json}", jsonMatch.Groups[1].Value);
        }
        else
        {
            _logger.LogWarning("No JSON code block found in response");
        }
        
        // ... rest of parsing logic
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "JSON parsing failed. Response: {Response}", 
            aiResponse.Substring(0, Math.Min(200, aiResponse.Length)));
    }
    
    return (false, null, aiResponse);
}
```

**Fix:**
- Check if AI response contains ```json code blocks
- Verify JSON is valid (use online JSON validator)
- Check for missing or extra commas, brackets
- Ensure field names match expected format

#### 3. Recommendation Request Not Detected

**Check:**
The system might not recognize the message as a recommendation request.

**Solution:**
```csharp
// Add logging in DetectRecommendationRequest
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
    var isRecommendation = keywords.Any(keyword => lowerMessage.Contains(keyword));
    
    _logger.LogInformation("Message: '{Message}' - Is recommendation request: {IsRecommendation}", 
        message, isRecommendation);
    
    return isRecommendation;
}
```

**Fix:**
- Add more keywords to detection logic
- Use more flexible pattern matching
- Allow users to explicitly request structured format

---

## Malformed or Missing Data

### Symptom
Cards display but with missing information or incorrect data.

### Possible Causes

#### 1. Missing Required Fields

**Check Frontend Console:**
```
Uncaught TypeError: Cannot read property 'name' of undefined
```

**Solution:**
Add validation in `renderRecommendationCard`:

```javascript
function renderRecommendationCard(recommendation) {
    // Validate required fields
    if (!recommendation) {
        console.error('Recommendation is null or undefined');
        return '';
    }
    
    if (!recommendation.id || !recommendation.name || !recommendation.type) {
        console.error('Missing required fields:', recommendation);
        return '';
    }
    
    // ... rest of rendering logic
}
```

**Fix:**
- Ensure backend validates all required fields
- Add default values for optional fields
- Handle null/undefined gracefully

#### 2. Invalid Relevance Score

**Check:**
Score is outside 0-100 range or not a number.

**Solution:**
```csharp
// Add validation in ParseStructuredResponse
foreach (var rec in structured.Recommendations)
{
    if (rec.RelevanceScore < 0 || rec.RelevanceScore > 100)
    {
        _logger.LogWarning(
            "Invalid relevance score {Score} for {Name}, clamping to 0-100",
            rec.RelevanceScore, rec.Name
        );
        rec.RelevanceScore = Math.Clamp(rec.RelevanceScore, 0, 100);
    }
}
```

**Fix:**
- Clamp scores to valid range
- Validate in backend before sending to frontend
- Update prompt to emphasize 0-100 range

#### 3. HTML Injection / XSS

**Check:**
Recommendation text contains HTML tags that break layout.

**Solution:**
```javascript
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function renderRecommendationCard(recommendation) {
    return `
        <h3 class="card-title">${escapeHtml(recommendation.name)}</h3>
        <p class="card-description">${escapeHtml(recommendation.description)}</p>
        <p class="reason-text">${escapeHtml(recommendation.reason)}</p>
    `;
}
```

**Fix:**
- Always escape user-generated content
- Use `textContent` instead of `innerHTML` where possible
- Sanitize on backend as well

---

## Styling Issues

### Symptom
Cards display but look broken or unstyled.

### Possible Causes

#### 1. CSS Not Applied

**Check:**
- Open DevTools Elements tab
- Inspect a card element
- Check if CSS classes are applied

**Solution:**
```javascript
// Verify classes in console
const card = document.querySelector('.recommendation-card');
console.log('Card classes:', card?.className);
console.log('Computed styles:', window.getComputedStyle(card));
```

**Fix:**
- Clear browser cache (Ctrl+Shift+Delete)
- Hard refresh (Ctrl+F5)
- Check CSS file path in Network tab
- Verify `asp-append-version="true"` for cache busting

#### 2. CSS Conflicts

**Check:**
Other stylesheets might override card styles.

**Solution:**
```css
/* Increase specificity if needed */
.chat-message .recommendation-card {
    background: var(--card-bg-gradient);
    /* ... other styles */
}
```

**Fix:**
- Use browser DevTools to see which styles are applied
- Check for `!important` overrides
- Increase CSS specificity
- Load recommendation-cards.css after other stylesheets

#### 3. CSS Variables Not Supported

**Check:**
Older browsers might not support CSS custom properties.

**Solution:**
```css
.recommendation-card {
    /* Fallback for older browsers */
    background: #E8EAF6;
    /* Modern browsers */
    background: var(--card-bg-gradient);
}
```

**Fix:**
- Add fallback values for all CSS variables
- Consider using PostCSS for automatic fallbacks
- Check browser compatibility requirements

---

## Navigation Problems

### Symptom
Clicking cards doesn't navigate to detail pages.

### Possible Causes

#### 1. Click Handler Not Attached

**Check Console:**
```
Uncaught ReferenceError: navigateToDetail is not defined
```

**Solution:**
```javascript
// Verify function exists
console.log(typeof navigateToDetail); // Should be 'function'

// Test manually
navigateToDetail(101, 'club');
```

**Fix:**
- Ensure `navigateToDetail` function is defined in chatbot.js
- Check for JavaScript errors that prevent function definition
- Verify onclick attribute is properly set

#### 2. Incorrect URL Construction

**Check:**
Navigation goes to wrong page or 404.

**Solution:**
```javascript
function navigateToDetail(id, type) {
    console.log('Navigating to:', id, type);
    
    const url = type === 'club' 
        ? `/clubs/${id}` 
        : `/activities/${id}`;
    
    console.log('URL:', url);
    
    window.location.href = url;
}
```

**Fix:**
- Verify URL format matches your routing
- Check if detail pages exist
- Ensure IDs are valid

#### 3. Event Propagation Issues

**Check:**
Click event might be prevented or stopped.

**Solution:**
```javascript
function renderRecommendationCard(recommendation) {
    return `
        <div class="recommendation-card" 
             onclick="navigateToDetail(${recommendation.id}, '${recommendation.type}'); return false;">
            <!-- Card content -->
        </div>
    `;
}
```

**Fix:**
- Use `return false` to prevent default behavior
- Check for event.stopPropagation() calls
- Verify no overlapping elements blocking clicks

---

## Performance Issues

### Symptom
Chatbot is slow or unresponsive when displaying recommendations.

### Possible Causes

#### 1. Too Many Recommendations

**Check:**
Response contains many cards (>10).

**Solution:**
```csharp
// Limit recommendations in backend
structured.Recommendations = structured.Recommendations
    .OrderByDescending(r => r.RelevanceScore)
    .Take(5)
    .ToList();
```

**Fix:**
- Limit to 3-5 recommendations per response
- Implement pagination for more results
- Sort by relevance and show top matches

#### 2. Slow AI Response

**Check Backend Logs:**
```
[Information] AI response time: 8523ms
```

**Solution:**
```csharp
var stopwatch = Stopwatch.StartNew();
var aiResponse = await _geminiService.GenerateResponseAsync(prompt);
stopwatch.Stop();

_logger.LogInformation("AI response time: {ElapsedMs}ms", 
    stopwatch.ElapsedMilliseconds);
```

**Fix:**
- Optimize prompt length (remove unnecessary context)
- Use faster Gemini model if available
- Implement timeout (30 seconds)
- Show loading indicator to user

#### 3. Database Queries

**Check:**
Multiple database calls for club/activity data.

**Solution:**
```csharp
// Implement caching
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

**Fix:**
- Cache club/activity data (10-minute expiration)
- Use eager loading for related entities
- Optimize database queries

---

## Mobile Display Issues

### Symptom
Cards don't display properly on mobile devices.

### Possible Causes

#### 1. Missing Viewport Meta Tag

**Check:**
```html
<!-- Should be in <head> -->
<meta name="viewport" content="width=device-width, initial-scale=1.0">
```

**Fix:**
Add viewport meta tag to _Layout.cshtml if missing.

#### 2. Media Query Not Applied

**Check:**
Mobile styles not activating.

**Solution:**
```css
/* Verify media query syntax */
@media (max-width: 768px) {
    .recommendation-card {
        padding: 16px;
    }
}
```

**Fix:**
- Test on actual mobile device, not just browser resize
- Check media query breakpoint (768px)
- Verify no CSS syntax errors

#### 3. Text Overflow

**Check:**
Text extends beyond card boundaries.

**Solution:**
```css
.card-title,
.card-description,
.reason-text {
    word-wrap: break-word;
    overflow-wrap: break-word;
}
```

**Fix:**
- Add word wrapping to text elements
- Set max-width on cards
- Test with long Vietnamese text

---

## Accessibility Issues

### Symptom
Screen readers don't announce card content properly.

### Possible Causes

#### 1. Missing ARIA Labels

**Check:**
Use screen reader (NVDA, JAWS, VoiceOver) to test.

**Solution:**
```javascript
function renderRecommendationCard(recommendation) {
    return `
        <div class="recommendation-card" 
             role="button"
             tabindex="0"
             aria-label="${recommendation.name}. ${recommendation.description}. ${recommendation.reason}. Độ phù hợp ${recommendation.relevanceScore} phần trăm."
             onclick="navigateToDetail(${recommendation.id}, '${recommendation.type}')">
            <!-- Card content -->
        </div>
    `;
}
```

**Fix:**
- Add comprehensive aria-label
- Include all important information
- Test with actual screen readers

#### 2. Keyboard Navigation Not Working

**Check:**
Tab key doesn't focus cards, Enter doesn't activate.

**Solution:**
```javascript
function handleCardKeydown(event, id, type) {
    if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        navigateToDetail(id, type);
    }
}

// In card HTML
onkeydown="handleCardKeydown(event, ${recommendation.id}, '${recommendation.type}')"
```

**Fix:**
- Add `tabindex="0"` to cards
- Implement keyboard event handlers
- Ensure focus indicators are visible

#### 3. Color-Only Information

**Check:**
Relevance score uses only color to convey meaning.

**Solution:**
```html
<div class="card-score">
    <span class="score-icon" aria-hidden="true">✨</span>
    <span class="score-text" style="color: ${scoreColor}">
        Độ phù hợp: ${recommendation.relevanceScore}%
    </span>
    <span class="sr-only">
        Độ phù hợp: ${recommendation.relevanceScore} phần trăm
    </span>
</div>
```

**Fix:**
- Always include text labels with colors
- Add sr-only text for context
- Don't rely solely on color

---

## Backend Errors

### Symptom
API returns error responses or 500 status codes.

### Common Errors

#### 1. Gemini API Error

**Error Message:**
```
AI service unavailable
```

**Check Logs:**
```
[Error] Gemini API error: Request timeout
[Error] Gemini API error: NotFound - models/gemini-pro is not found
```

**Common Causes:**

**A. Model Not Found (404 Error)**

If you see: `models/gemini-pro is not found for API version v1beta`

**Solution:**
The model name has changed. Update `appsettings.json`:

```json
"GeminiAI": {
  "ApiKey": "YOUR_API_KEY",
  "ModelName": "gemini-1.5-flash",
  "ApiBaseUrl": "https://generativelanguage.googleapis.com/v1beta",
  "Temperature": 0.7,
  "MaxTokens": 2048
}
```

**Important:** The property must be `"ModelName"` (not `"Model"`) to match the `GeminiAIOptions` class.

**Valid model names:**
- `gemini-1.5-flash` (recommended, fast and efficient)
- `gemini-1.5-pro` (more capable, slower)
- `gemini-1.0-pro` (older version)

**B. Request Timeout**

**Solution:**
```csharp
try
{
    var aiResponse = await _geminiService.GenerateResponseAsync(prompt);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Gemini API request failed");
    return new ChatResponseDto
    {
        Message = "Xin lỗi, dịch vụ AI tạm thời không khả dụng. Vui lòng thử lại sau.",
        Success = false,
        ErrorMessage = "AI service unavailable"
    };
}
```

**C. Index Out of Range Error**

If you see: `Index was out of range. Must be non-negative and less than the size of the collection`

This usually means the Gemini API returned an empty or unexpected response structure.

**Solution:**

1. Check the logs for the actual response content (enable Debug logging)
2. Verify the response has candidates and content parts
3. Check if content was blocked by safety filters

The updated code now handles:
- Empty candidates array
- Missing content parts
- Safety filter blocks
- Different finish reasons

**Fix:**
- Check Gemini API key is valid
- Verify API quota not exceeded
- Check network connectivity
- Verify model name is correct (`gemini-1.5-flash` or `gemini-1.5-pro`)
- Enable Debug logging to see actual API responses
- Check if content is being blocked by safety filters
- Implement retry logic with exponential backoff

#### 2. Database Connection Error

**Error Message:**
```
Internal server error
```

**Check Logs:**
```
[Error] Database connection failed
```

**Solution:**
```csharp
try
{
    var clubs = await _clubRepository.GetActiveClubsAsync();
}
catch (SqlException ex)
{
    _logger.LogError(ex, "Database query failed");
    // Return cached data or error response
}
```

**Fix:**
- Check database connection string
- Verify database is running
- Check network connectivity
- Implement connection retry logic

#### 3. Rate Limit Exceeded

**Error Message:**
```
Too many requests
```

**HTTP Status:** 429

**Solution:**
```csharp
// Check rate limit before processing
if (await _rateLimiter.IsRateLimitExceededAsync(studentId))
{
    return new ChatResponseDto
    {
        Message = "Bạn đã gửi quá nhiều tin nhắn. Vui lòng đợi một chút.",
        Success = false,
        ErrorMessage = "Rate limit exceeded"
    };
}
```

**Fix:**
- Wait before sending more requests
- Increase rate limit if appropriate
- Implement client-side throttling

---

## Logging and Debugging

### Enable Detailed Logging

#### Backend (appsettings.Development.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Services.Chatbot": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Frontend (chatbot.js)

```javascript
// Add debug flag
const DEBUG = true;

function debugLog(...args) {
    if (DEBUG) {
        console.log('[Chatbot Debug]', ...args);
    }
}

// Use throughout code
debugLog('Sending message:', message);
debugLog('Response received:', data);
debugLog('Rendering cards:', data.recommendations);
```

### Common Log Messages

#### Success Case

```
[Information] Message: 'Tìm câu lạc bộ về công nghệ' - Is recommendation request: True
[Information] Building structured prompt for student 12345
[Information] AI response time: 2341ms
[Debug] AI Response: ```json{"message":"...","recommendations":[...]}```
[Debug] JSON extracted: {"message":"...","recommendations":[...]}
[Information] Returning structured response with 3 recommendations
```

#### Failure Case

```
[Information] Message: 'Tìm câu lạc bộ về công nghệ' - Is recommendation request: True
[Information] Building structured prompt for student 12345
[Information] AI response time: 3124ms
[Debug] AI Response: Dựa trên chuyên ngành của bạn, tôi gợi ý...
[Warning] No JSON code block found in response
[Warning] Failed to parse structured response: Unexpected character at position 0
[Information] Returning plain text response
```

### Debugging Checklist

- [ ] Check browser console for JavaScript errors
- [ ] Check Network tab for API request/response
- [ ] Check backend logs for errors or warnings
- [ ] Verify authentication (user logged in)
- [ ] Check rate limiting status
- [ ] Verify database connectivity
- [ ] Check Gemini API status
- [ ] Test with simple message first
- [ ] Clear browser cache and retry
- [ ] Test in incognito/private mode
- [ ] Test on different browser
- [ ] Test on mobile device

### Getting Help

If issues persist after troubleshooting:

1. **Collect Information:**
   - Browser and version
   - Error messages from console
   - Backend log excerpts
   - Steps to reproduce
   - Expected vs actual behavior

2. **Check Documentation:**
   - [API Documentation](./API_DOCUMENTATION.md)
   - [Developer Guide](./DEVELOPER_GUIDE.md)
   - [CSS Reference](./CSS_REFERENCE.md)

3. **Contact Support:**
   - Include all collected information
   - Provide screenshots if applicable
   - Share relevant code snippets

---

## Quick Reference

### Common Fixes

| Issue | Quick Fix |
|-------|-----------|
| Cards not showing | Clear cache, hard refresh (Ctrl+F5) |
| Plain text only | Check backend logs for JSON parsing errors |
| Styling broken | Verify CSS file loaded in Network tab |
| Click not working | Check console for JavaScript errors |
| Slow response | Check AI response time in logs |
| Mobile broken | Add viewport meta tag |
| Screen reader issues | Add ARIA labels and test |

### Useful Commands

```bash
# Clear browser cache (Chrome)
Ctrl+Shift+Delete

# Hard refresh
Ctrl+F5

# Open DevTools
F12

# Rebuild and restart
dotnet build
dotnet run
```

### Test Messages

Use these to test different scenarios:

```
✓ "Tìm câu lạc bộ về công nghệ" - Should return structured recommendations
✓ "Gợi ý hoạt động cho sinh viên IT" - Should return structured recommendations
✓ "Câu lạc bộ là gì?" - Should return plain text
✓ "Xin chào" - Should return plain text greeting
```

---

## Additional Resources

- [API Documentation](./API_DOCUMENTATION.md)
- [Developer Guide](./DEVELOPER_GUIDE.md)
- [CSS Reference](./CSS_REFERENCE.md)
- [Requirements Document](./requirements.md)
- [Design Document](./design.md)
