# Requirements Document - Chatbot Rich Recommendations UI

## Introduction

This feature enhances the AI chatbot to provide visually rich, structured recommendations for clubs and activities. Instead of plain text responses, the chatbot will display beautiful recommendation cards with icons, relevance scores, and detailed explanations. The system will use structured prompts to get formatted data from Gemini AI and render them as interactive cards in the chat interface.

## Glossary

- **Chatbot System**: The AI-powered conversational interface that helps students find clubs and activities
- **Recommendation Card**: A visual UI component displaying club/activity information with icon, name, description, and relevance score
- **Structured Response**: JSON-formatted data returned by Gemini AI containing recommendation details
- **Relevance Score**: A percentage (0-100%) indicating how well a recommendation matches the student's profile
- **Rich UI**: Enhanced user interface with colors, icons, cards, and visual hierarchy
- **Gemini AI**: Google's generative AI model used for natural language processing
- **Frontend Client**: The JavaScript-based chat interface in the browser
- **Backend Service**: The C# service layer that processes chat requests and communicates with Gemini AI

## Requirements

### Requirement 1: Structured AI Response Format

**User Story:** As a student, I want to receive club recommendations in a visually appealing format with clear information about each club, so that I can quickly understand which clubs are most relevant to me.

#### Acceptance Criteria

1. WHEN THE Chatbot System receives a club recommendation request, THE Backend Service SHALL instruct Gemini AI to return responses in JSON format with structured recommendation data
2. THE Structured Response SHALL include an array of recommendations, where each recommendation contains: id, name, type, description, reason, and relevanceScore fields
3. THE Backend Service SHALL validate the Structured Response format before sending to Frontend Client
4. IF Gemini AI returns unstructured text instead of JSON, THEN THE Backend Service SHALL attempt to extract recommendation data using fallback parsing logic
5. THE Structured Response SHALL support both club recommendations and activity recommendations with the same data structure

### Requirement 2: Enhanced System Prompt Engineering

**User Story:** As a system administrator, I want the AI to consistently return well-formatted recommendation data, so that the frontend can reliably display rich UI components.

#### Acceptance Criteria

1. THE Backend Service SHALL include explicit JSON schema instructions in the system prompt sent to Gemini AI
2. THE system prompt SHALL specify the exact format for recommendation responses including all required fields
3. THE system prompt SHALL instruct Gemini AI to calculate relevance scores (0-100%) based on student profile matching
4. THE system prompt SHALL request Vietnamese language responses for all text fields
5. WHEN building the prompt, THE Backend Service SHALL include examples of the expected JSON format

### Requirement 3: Recommendation Card UI Component

**User Story:** As a student, I want to see each club recommendation as a beautiful card with an icon, name, description, and relevance score, so that I can easily compare options.

#### Acceptance Criteria

1. THE Frontend Client SHALL render each recommendation as a card component with distinct visual styling
2. THE Recommendation Card SHALL display a category icon (👥 for clubs, 🎯 for activities) at the top
3. THE Recommendation Card SHALL display the club/activity name in large, bold, blue text
4. THE Recommendation Card SHALL display the reason text with a 💡 icon prefix
5. THE Recommendation Card SHALL display the relevance score with a ✨ icon and green percentage text
6. THE Recommendation Card SHALL use a light blue/purple gradient background
7. THE Recommendation Card SHALL have rounded corners and subtle shadow for depth
8. WHEN a student hovers over a Recommendation Card, THE card SHALL display a subtle scale animation

### Requirement 4: Response Type Detection

**User Story:** As a student, I want the chatbot to automatically detect when to show rich recommendation cards versus plain text, so that I get the appropriate format for each type of question.

#### Acceptance Criteria

1. THE Frontend Client SHALL detect when a response contains structured recommendation data
2. WHEN the response contains recommendations array, THE Frontend Client SHALL render Recommendation Cards
3. WHEN the response contains plain text only, THE Frontend Client SHALL render standard message bubbles
4. THE Frontend Client SHALL support mixed responses with both text and recommendations in the same message
5. THE Frontend Client SHALL display a text introduction before the recommendation cards when both are present

### Requirement 5: Relevance Score Visualization

**User Story:** As a student, I want to see a visual indicator of how well each club matches my profile, so that I can prioritize which clubs to explore first.

#### Acceptance Criteria

1. THE Recommendation Card SHALL display the relevance score as a percentage with color coding
2. WHEN relevanceScore is >= 90%, THE score text SHALL be displayed in dark green color
3. WHEN relevanceScore is 70-89%, THE score text SHALL be displayed in medium green color
4. WHEN relevanceScore is 50-69%, THE score text SHALL be displayed in yellow color
5. WHEN relevanceScore is < 50%, THE score text SHALL be displayed in orange color

### Requirement 6: Mobile Responsive Design

**User Story:** As a student using a mobile device, I want recommendation cards to display properly on my phone screen, so that I can browse clubs on the go.

#### Acceptance Criteria

1. THE Recommendation Card SHALL adapt its layout for screens smaller than 768px width
2. ON mobile devices, THE Recommendation Card SHALL stack vertically with full width
3. THE card text SHALL remain readable with appropriate font sizes on mobile
4. THE card spacing and padding SHALL adjust for touch-friendly interaction on mobile
5. THE relevance score SHALL remain visible without horizontal scrolling on mobile

### Requirement 7: Error Handling for Malformed Responses

**User Story:** As a student, I want to still receive helpful information even if the AI response format is incorrect, so that my chat experience is not interrupted by technical errors.

#### Acceptance Criteria

1. WHEN Gemini AI returns invalid JSON format, THE Backend Service SHALL log the error with correlation ID
2. THE Backend Service SHALL attempt to extract recommendation data from malformed responses using regex patterns
3. IF extraction fails, THE Backend Service SHALL return the original text response to Frontend Client
4. THE Frontend Client SHALL gracefully fall back to plain text display when structured data is unavailable
5. THE Chatbot System SHALL not crash or show error messages to students when response parsing fails

### Requirement 8: Recommendation Card Interactions

**User Story:** As a student, I want to click on a recommendation card to view more details about the club, so that I can learn more before joining.

#### Acceptance Criteria

1. THE Recommendation Card SHALL be clickable and display a pointer cursor on hover
2. WHEN a student clicks a Recommendation Card, THE Frontend Client SHALL navigate to the club/activity detail page
3. THE Recommendation Card SHALL include the club/activity ID in a data attribute for navigation
4. THE card click SHALL open the detail page in the same browser tab
5. THE Frontend Client SHALL track card clicks for analytics purposes

### Requirement 9: Loading State for Recommendations

**User Story:** As a student, I want to see a loading indicator while the AI is generating recommendations, so that I know the system is working on my request.

#### Acceptance Criteria

1. WHEN the student sends a recommendation request, THE Frontend Client SHALL display a typing indicator
2. THE typing indicator SHALL show animated dots with text "AI đang tìm kiếm câu lạc bộ phù hợp..."
3. THE typing indicator SHALL remain visible until the Backend Service returns the response
4. WHEN recommendations are received, THE Frontend Client SHALL smoothly transition from loading to displaying cards
5. THE loading state SHALL timeout after 30 seconds with an error message if no response is received

### Requirement 10: Accessibility Compliance

**User Story:** As a student with visual impairments, I want recommendation cards to be accessible with screen readers, so that I can use the chatbot effectively.

#### Acceptance Criteria

1. THE Recommendation Card SHALL include appropriate ARIA labels for screen readers
2. THE card SHALL have semantic HTML structure with proper heading hierarchy
3. THE relevance score SHALL be announced by screen readers with context (e.g., "Relevance: 95 percent")
4. THE card SHALL be keyboard navigable with visible focus indicators
5. THE color-coded relevance scores SHALL not be the only indicator of importance (text labels required)
