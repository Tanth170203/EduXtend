# Implementation Plan - AI Chatbot Assistant

- [x] 1. Set up Gemini AI configuration and service infrastructure





  - Add GeminiAI configuration section to WebAPI/appsettings.json with ApiKey, Model, Temperature, MaxTokens, and ApiEndpoint
  - Create GeminiAIOptions class in Services/Chatbot/GeminiAIOptions.cs for configuration binding
  - Register GeminiAIOptions in WebAPI/Program.cs using Configure<GeminiAIOptions>
  - Register HttpClient for Gemini API in WebAPI/Program.cs with IHttpClientFactory
  - _Requirements: 6.1, 6.2, 6.3, 6.4_
- [x] 2. Implement GeminiAIService for API integration




- [ ] 2. Implement GeminiAIService for API integration

  - Create IGeminiAIService interface in Services/Chatbot/IGeminiAIService.cs with GenerateResponseAsync method
  - Create GeminiAIService class in Services/Chatbot/GeminiAIService.cs implementing IGeminiAIService
  - Implement GenerateResponseAsync method to send HTTP POST requests to Gemini API endpoint
  - Create Gemini API request/response models (GeminiRequest, GeminiResponse, GeminiContent, GeminiPart, GeminiGenerationConfig, GeminiCandidate) in Services/Chatbot/Models/
  - Implement BuildRequest method to construct Gemini API request with prompt and generation config
  - Implement ParseResponse method to extract text from Gemini JSON response
  - Implement SendRequestWithRetryAsync with exponential backoff for transient failures (max 3 retries)
  - Add error handling for invalid API key, rate limiting, quota exceeded, and network timeouts
  - Add logging for all API calls, responses, and errors with correlation IDs
  - Register IGeminiAIService as scoped service in WebAPI/Program.cs
  - _Requirements: 6.3, 6.4, 6.5, 10.2, 10.3_

- [x] 3. Create DTOs for chatbot requests and responses




  - Create ChatMessageRequestDto in BusinessObject/DTOs/Chatbot/ChatMessageRequestDto.cs with Message and ConversationHistory properties
  - Create ChatMessageResponseDto in BusinessObject/DTOs/Chatbot/ChatMessageResponseDto.cs with Message, Timestamp, Success, and ErrorMessage properties
  - Create ChatMessageDto in BusinessObject/DTOs/Chatbot/ChatMessageDto.cs with Role, Content, and Timestamp properties
  - Add validation attributes (Required, MaxLength) to DTO properties
  - _Requirements: 3.1, 3.4, 5.4_
-

- [x] 4. Implement ChatbotService for business logic




  - Create IChatbotService interface in Services/Chatbot/IChatbotService.cs with ProcessChatMessageAsync method
  - Create ChatbotService class in Services/Chatbot/ChatbotService.cs implementing IChatbotService
  - Implement BuildStudentContextAsync method to retrieve student profile, major, and current club memberships from database using IStudentRepository
  - Create StudentContext model in Services/Chatbot/Models/StudentContext.cs with StudentId, FullName, MajorName, Cohort, CurrentClubs, and Interests properties
  - Implement GetRelevantClubsAsync method to query active clubs with open recruitment, filtered by student's interests and major
  - Create ClubRecommendation model in Services/Chatbot/Models/ClubRecommendation.cs with ClubId, Name, SubName, Description, CategoryName, and IsRecruitmentOpen properties
  - Implement GetUpcomingActivitiesAsync method to query approved activities with StartTime > DateTime.Now, filtered by student's club memberships and interests
  - Create ActivityRecommendation model in Services/Chatbot/Models/ActivityRecommendation.cs with ActivityId, Title, Description, Location, StartTime, ClubName, ActivityType, and IsPublic properties
  - Implement BuildAIPrompt method to format system prompt with student context, club list, activity list, conversation history, and user message
  - Implement FormatConversationHistory method to format chat history as text for AI context
  - Implement ProcessChatMessageAsync method to orchestrate: build context → get clubs/activities → build prompt → call GeminiAIService → return response
  - Add error handling for database errors, AI service errors, and unexpected exceptions
  - Add logging for all service operations with student ID and correlation IDs
  - Register IChatbotService as scoped service in WebAPI/Program.cs
  - _Requirements: 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 10.1, 10.4_
- [x] 5. Create ChatbotController API endpoint




- [ ] 5. Create ChatbotController API endpoint

  - Create ChatbotController in WebAPI/Controllers/ChatbotController.cs with [ApiController], [Route("api/[controller]")], and [Authorize] attributes
  - Inject IChatbotService, ILogger<ChatbotController>, and IHttpContextAccessor in constructor
  - Implement POST /api/chatbot/message endpoint with [HttpPost("message")] attribute
  - Extract student ID from User.Claims in SendMessage method
  - Validate ChatMessageRequestDto using ModelState
  - Call IChatbotService.ProcessChatMessageAsync with student ID, message, and conversation history
  - Return ChatMessageResponseDto with AI response, timestamp, and success status
  - Add error handling for authentication errors (401), validation errors (400), service errors (500), and Gemini API errors (502/503)
  - Create ChatbotErrorMessages static class in WebAPI/Constants/ChatbotErrorMessages.cs with Vietnamese error messages
  - Return appropriate HTTP status codes and error messages for each error type
  - Add logging for all controller actions with correlation IDs
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.4_
-

- [x] 6. Implement frontend chat UI component




  - Create chatbot.css in WebFE/wwwroot/css/chatbot.css with styles for floating button, modal window, message bubbles, quick action buttons, and typing indicator
  - Create chatbot.js in WebFE/wwwroot/js/chatbot.js with chat UI logic
  - Implement initChatbot function to initialize chatbot on page load and attach event listeners
  - Implement toggleChatModal function to show/hide chat modal window
  - Implement displayWelcomeMessage function to show welcome message and quick action buttons on first open
  - Implement handleQuickAction function to send predefined messages for "Tìm CLB phù hợp", "Xem hoạt động", and "Tìm hiểu thêm"
  - Implement sendMessage function to send user message to /api/chatbot/message endpoint with fetch API
  - Implement displayMessage function to render user and AI messages in chat history with appropriate styling
  - Implement showTypingIndicator function to display "AI đang suy nghĩ..." animation
  - Implement hideTypingIndicator function to remove typing indicator
  - Implement loadChatHistory function to restore chat messages from sessionStorage
  - Implement saveChatHistory function to persist chat messages to sessionStorage (max 50 messages)
  - Implement clearChatHistory function to remove chat history from sessionStorage on logout
  - Add error handling to display user-friendly error messages in chat UI
  - Disable send button while AI is processing to prevent multiple simultaneous requests
  - Add 30-second timeout for API requests with timeout error message
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.5, 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5, 9.2, 9.3_
-

- [x] 7. Integrate chat UI into WebFE layout




  - Add chatbot.css reference to WebFE/Pages/Shared/_Layout.cshtml in <head> section
  - Add chatbot.js reference to WebFE/Pages/Shared/_Layout.cshtml before </body> tag
  - Add floating chat button HTML to _Layout.cshtml with onclick="toggleChatModal()"
  - Add chat modal HTML structure to _Layout.cshtml with welcome message, quick actions, message container, and input field
  - Add conditional rendering to only show chat button for authenticated users with Student role
  - Test chat button appears on all pages (Index, Clubs, Activities, Profile, etc.)
  - _Requirements: 1.1, 1.2, 1.3, 1.4_
-

- [x] 8. Add authentication check and logout handler




  - Modify chatbot.js to check authentication status before sending messages
  - Implement handleAuthError function to redirect to login page on 401 Unauthorized
  - Add event listener for logout button to call clearChatHistory before logout
  - Test unauthenticated users are redirected to login when trying to use chatbot
  - Test chat history is cleared on logout
  - _Requirements: 3.2, 7.4, 9.3_
-

- [x] 9. Implement caching for performance optimization




  - Add IMemoryCache to ChatbotService constructor
  - Implement caching for BuildStudentContextAsync with 5-minute expiration using cache key "student_context_{studentId}"
  - Implement caching for GetRelevantClubsAsync with 10-minute expiration using cache key "active_clubs"
  - Implement caching for GetUpcomingActivitiesAsync with 5-minute expiration using cache key "upcoming_activities"
  - Add cache invalidation logic when student profile, clubs, or activities are updated
  - _Requirements: 4.1, 5.1, 5.2_
-

- [x] 10. Add rate limiting to prevent abuse




  - Install AspNetCoreRateLimit NuGet package in WebAPI project
  - Configure rate limiting in WebAPI/Program.cs with 10 requests per minute per user for /api/chatbot/* endpoints
  - Add rate limiting middleware before authentication middleware
  - Return 429 Too Many Requests with Vietnamese error message when limit exceeded
  - Test rate limiting works correctly by sending multiple rapid requests
  - _Requirements: 9.4_
-

- [x] 11. Create configuration documentation




  - Create README.md in .kiro/specs/ai-chatbot-assistant/ with setup instructions
  - Document how to obtain Gemini API key from Google AI Studio
  - Document appsettings.json configuration options (ApiKey, Model, Temperature, MaxTokens)
  - Document environment variable setup for production deployment
  - Document rate limiting configuration
  - _Requirements: 6.1, 6.2_
-

- [x] 12. Write unit tests for core services








  - [x] 12.1 Create GeminiAIServiceTests in WebAPI.Tests/Services/Chatbot/GeminiAIServiceTests.cs


    - Test GenerateResponseAsync with valid prompt returns expected response
    - Test GenerateResponseAsync with invalid API key throws appropriate exception
    - Test retry logic on transient failures (network errors, 500 errors)
    - Test timeout handling after 30 seconds
    - Test response parsing with valid Gemini JSON response
    - Test response parsing with malformed JSON throws exception
    - _Requirements: 6.3, 6.4, 6.5_

  - [x] 12.2 Create ChatbotServiceTests in WebAPI.Tests/Services/Chatbot/ChatbotServiceTests.cs



    - Test BuildStudentContextAsync with valid student ID returns correct context
    - Test BuildStudentContextAsync with invalid student ID throws exception
    - Test GetRelevantClubsAsync returns clubs matching student interests
    - Test GetUpcomingActivitiesAsync filters by date and status correctly
    - Test BuildAIPrompt formats context correctly with all required sections
    - Test ProcessChatMessageAsync handles empty message gracefully
    - Test ProcessChatMessageAsync includes conversation history in prompt
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3_

  - [x] 12.3 Create ChatbotControllerTests in WebAPI.Tests/Controllers/ChatbotControllerTests.cs




    - Test SendMessage with authenticated user returns 200 OK with AI response
    - Test SendMessage with unauthenticated user returns 401 Unauthorized
    - Test SendMessage with invalid request (empty message) returns 400 Bad Request
    - Test SendMessage with service error returns 500 Internal Server Error
    - Test SendMessage with Gemini API error returns 502 Bad Gateway
    - _Requirements: 3.1, 3.2, 9.1, 9.2, 9.3, 9.4_

-

- [x] 13. Perform integration and manual testing






  - [x] 13.1 Test end-to-end chat flow with real Gemini API


    - Send message "Tôi muốn tìm CLB phù hợp với chuyên ngành của mình" and verify AI responds with relevant clubs
    - Send message "Có hoạt động nào sắp tới không?" and verify AI responds with upcoming activities
    - Test conversation history is maintained across multiple messages
    - Test quick action buttons trigger appropriate AI responses
    - _Requirements: 3.1, 3.3, 3.4, 3.5, 4.4, 4.5, 5.4, 5.5_

  - [x] 13.2 Test UI/UX on different devices and browsers

    - Test floating chat button appears on all pages (Chrome, Firefox, Edge)
    - Test chat modal opens and closes correctly
    - Test welcome message displays on first open
    - Test user messages display on right side with correct styling
    - Test AI messages display on left side with correct styling
    - Test typing indicator shows while waiting for AI response
    - Test error messages display correctly in chat UI
    - Test chat history persists when modal is closed and reopened
    - Test chat history clears on logout
    - Test mobile responsive design on iPhone and Android devices
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.3, 9.5_

  - [x] 13.3 Test error handling scenarios


    - Test with invalid Gemini API key and verify error message displays
    - Test with network disconnected and verify timeout error displays
    - Test with unauthenticated user and verify redirect to login
    - Test with rate limit exceeded and verify 429 error message displays
    - Test with database connection error and verify generic error message displays
    - _Requirements: 6.5, 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 13.4 Test performance and scalability


    - Measure API response time for typical requests (should be < 5 seconds)
    - Test chat UI remains responsive during API calls
    - Test session storage doesn't grow unbounded (50 message limit enforced)
    - Test caching reduces database queries for repeated requests
    - Test rate limiting prevents abuse with rapid requests
    - _Requirements: 7.5, 8.5_
