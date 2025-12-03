# Implementation Plan - Chatbot Rich Recommendations UI

- [x] 1. Create backend data models for structured responses





  - Create StructuredResponse class in Services/Chatbot/Models/StructuredResponse.cs with Message and Recommendations properties
  - Create RecommendationItem class in Services/Chatbot/Models/RecommendationItem.cs with Id, Name, Type, Description, Reason, and RelevanceScore properties
  - Add data validation attributes to RecommendationItem (Range for RelevanceScore 0-100)
  - _Requirements: 1.2, 1.3_
-

- [x] 2. Enhance ChatResponseDto to support structured recommendations




  - Add HasRecommendations boolean property to BusinessObject/DTOs/Chatbot/ChatResponseDto.cs
  - Add Recommendations list property of type List<RecommendationDto> to ChatResponseDto
  - Create RecommendationDto class in BusinessObject/DTOs/Chatbot/RecommendationDto.cs with Id, Name, Type, Description, Reason, RelevanceScore properties
  - Ensure backward compatibility with existing plain text responses
  - _Requirements: 1.2, 1.5_
-

- [x] 3. Implement enhanced prompt builder with JSON schema instructions




  - Create BuildStructuredPrompt method in Services/Chatbot/ChatbotService.cs
  - Add JSON schema template with example format to prompt
  - Include instructions for Gemini AI to return structured JSON responses
  - Add student context (name, major, cohort) to prompt
  - Add available clubs/activities list to prompt
  - Include instructions to calculate relevance scores (0-100%) based on profile matching
  - Specify Vietnamese language requirement for all text fields
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 4. Implement response parser for structured JSON





  - Create ParseStructuredResponse method in Services/Chatbot/ChatbotService.cs
  - Implement regex pattern to extract JSON from markdown code blocks (```json...```)
  - Add JSON deserialization with JsonSerializer using case-insensitive options
  - Implement validation logic to check if recommendations array exists and has items
  - Return tuple with (isStructured, structuredData, plainText) for flexible handling
  - Add error logging for JSON parsing failures with correlation IDs
  - _Requirements: 1.3, 1.4, 7.1, 7.2_
-

- [x] 5. Update ChatbotService to use structured prompts and parse responses




  - Modify ProcessChatMessageAsync method in Services/Chatbot/ChatbotService.cs
  - Detect if user message is requesting recommendations (clubs, activities)
  - Call BuildStructuredPrompt when recommendation request detected
  - Call ParseStructuredResponse to handle AI response
  - Map StructuredResponse to ChatResponseDto with recommendations
  - Implement fallback to plain text when structured parsing fails
  - Add logging for structured vs plain text response paths
  - _Requirements: 1.1, 1.4, 4.1, 7.3, 7.4_
-

- [x] 6. Create recommendation card CSS styles



  - Create recommendation-cards.css in WebFE/wwwroot/css/ with card component styles
  - Define CSS variables for color palette (card backgrounds, text colors, score colors)
  - Implement .recommendation-card class with gradient background and rounded corners
  - Add .card-header, .card-title, .card-description, .card-reason, .card-score styles
  - Implement hover animation with transform and shadow effects
  - Add left border accent with ::before pseudo-element
  - Create mobile responsive styles with @media query for screens < 768px
  - Define .recommendations-container class for card layout
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 6.1, 6.2, 6.3, 6.4_

- [x] 7. Implement frontend message type detector





  - Add detectMessageType function in WebFE/wwwroot/js/chatbot.js
  - Check if response has hasRecommendations property set to true
  - Check if recommendations array exists and has length > 0
  - Return 'recommendations' or 'text' message type
  - _Requirements: 4.1, 4.2_
-

- [x] 8. Implement recommendation card renderer




  - Add renderRecommendationCard function in WebFE/wwwroot/js/chatbot.js
  - Map recommendation type to icon (👥 for club, 🎯 for activity)
  - Generate card HTML with header, title, description, reason, and score sections
  - Add data attributes (data-id, data-type) for click handling
  - Implement getScoreColor function to return color based on relevance score ranges
  - Add onclick handler to card for navigation
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_
-

- [x] 9. Update message display handler to support recommendation cards




  - Modify displayMessage function in WebFE/wwwroot/js/chatbot.js
  - Call detectMessageType to determine response format
  - For 'recommendations' type: render intro text + recommendation cards container
  - For 'text' type: render standard message bubble
  - Support mixed responses with both text and recommendations
  - Append cards to recommendations-container div
  - Maintain existing functionality for user messages
  - _Requirements: 4.2, 4.3, 4.4, 4.5_


- [x] 10. Implement card click navigation




  - Add navigateToDetail function in WebFE/wwwroot/js/chatbot.js
  - Extract club/activity ID and type from card data attributes
  - Construct detail page URL based on type (/clubs/{id} or /activities/{id})
  - Navigate to detail page in same tab using window.location.href
  - Add analytics tracking for card clicks (optional)
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 11. Enhance typing indicator for recommendation searches





  - Update showTypingIndicator function in WebFE/wwwroot/js/chatbot.js
  - Change indicator text to "AI đang tìm kiếm câu lạc bộ phù hợp..." for recommendation requests
  - Keep existing "AI đang suy nghĩ..." for general messages
  - Ensure smooth transition from loading to card display
  - _Requirements: 9.1, 9.2, 9.3, 9.4_
-

- [x] 12. Add accessibility features to recommendation cards




  - Add ARIA labels to card elements in renderRecommendationCard function
  - Add role="button" and tabindex="0" to clickable cards
  - Implement keyboard navigation support (Enter/Space to activate)
  - Add aria-label with full recommendation context for screen readers
  - Ensure focus indicators are visible for keyboard navigation
  - Add sr-only text labels for relevance scores with context
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
-

- [x] 13. Add recommendation-cards.css to layout




  - Add <link> tag for recommendation-cards.css in WebFE/Pages/Shared/_Layout.cshtml
  - Place after existing chatbot.css reference
  - Add asp-append-version="true" for cache busting
  - _Requirements: 3.1, 6.1_
-

- [x] 14. Implement fallback handling for malformed responses




  - Add try-catch blocks around JSON parsing in ParseStructuredResponse
  - Log parsing errors with correlation IDs and original response content
  - Return plain text fallback when JSON parsing fails
  - Ensure frontend gracefully displays plain text when hasRecommendations is false
  - Test with intentionally malformed JSON responses
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
-

- [x] 15. Test structured response flow end-to-end




  - Send message "Tìm câu lạc bộ về công nghệ" and verify structured JSON response from backend
  - Verify ChatResponseDto contains hasRecommendations=true and populated recommendations array
  - Verify frontend detects recommendation type correctly
  - Verify recommendation cards render with correct data (name, reason, score)
  - Verify relevance scores display with correct color coding
  - Verify card hover animations work smoothly
  - Verify card clicks navigate to correct detail pages
  - _Requirements: 1.1, 1.2, 3.1, 4.1, 5.1, 8.1_

- [x] 16. Test mobile responsive design





  - Open chatbot on mobile device (iPhone, Android)
  - Verify recommendation cards stack vertically and fit screen width
  - Verify text remains readable with appropriate font sizes
  - Verify card spacing and padding are touch-friendly
  - Verify relevance scores visible without horizontal scrolling
  - Verify hover effects work as touch interactions on mobile
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
-

- [x] 17. Test error handling and fallback scenarios




  - Test with Gemini AI returning plain text instead of JSON
  - Verify system falls back to plain text display gracefully
  - Test with malformed JSON (missing fields, invalid structure)
  - Verify error logging captures parsing failures
  - Test with empty recommendations array
  - Verify no crashes or blank screens in error scenarios
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
-

- [x] 18. Test accessibility compliance









  - Test recommendation cards with screen reader (NVDA, JAWS, VoiceOver)
  - Verify ARIA labels are announced correctly
  - Verify relevance scores announced with context ("Độ phù hợp: 95 phần trăm")
  - Test keyboard navigation (Tab, Enter, Space keys)
  - Verify focus indicators are visible
  - Verify color is not the only indicator of relevance (text labels present)
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
-


-

- [x] 19. Performance optimization





  - Implement caching for club/activity data in ChatbotService (10-minute expiration)
  - Add debouncing to typing indicator to prevent flickering
  - Minify recommendation-cards.css for production
  - Test response time for recommendation requests (should be < 5 seconds)
  - Monitor memory usage with multiple recommendation cards
-

- [x] 20. Create documentation




  - Document new structured response format in API documentation
  - Add examples of JSON schema to developer guide
  - Document CSS classes for recommendation cards
  - Create troubleshooting guide for common issues
  - Add screenshots of recommendation cards to user guide
