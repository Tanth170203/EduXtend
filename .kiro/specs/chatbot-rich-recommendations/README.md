# Chatbot Rich Recommendations - Documentation

## Overview

This directory contains comprehensive documentation for the Chatbot Rich Recommendations feature. This feature enhances the AI chatbot to provide visually rich, interactive recommendation cards for clubs and activities instead of plain text responses.

## Documentation Structure

### For Users

📘 **[User Guide](./USER_GUIDE.md)**
- How to use the chatbot to get recommendations
- Understanding recommendation cards
- Tips for best results
- Accessibility features
- FAQ and troubleshooting

### For Developers

📗 **[API Documentation](./API_DOCUMENTATION.md)**
- API endpoints and request/response formats
- Data models and JSON schemas
- Authentication and rate limiting
- Error handling
- Example usage

📗 **[Developer Guide](./DEVELOPER_GUIDE.md)**
- Architecture and component overview
- Backend implementation details
- Frontend implementation details
- JSON schema examples
- Best practices
- Extending the system
- Performance optimization

📗 **[CSS Reference](./CSS_REFERENCE.md)**
- Complete CSS class reference
- CSS variables and theming
- Responsive design
- Customization examples
- Browser compatibility

📗 **[Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md)**
- Common issues and solutions
- Debugging techniques
- Logging and diagnostics
- Performance issues
- Mobile and accessibility issues

### For Project Management

📙 **[Requirements Document](./requirements.md)**
- Feature requirements with EARS patterns
- User stories and acceptance criteria
- Glossary of terms

📙 **[Design Document](./design.md)**
- System architecture
- Component design
- Data models
- Error handling strategy
- Testing strategy

📙 **[Implementation Tasks](./tasks.md)**
- Task breakdown with status tracking
- Implementation order
- Requirements mapping

## Quick Start

### For End Users

1. Read the [User Guide](./USER_GUIDE.md)
2. Log in to your student account
3. Open the chatbot (💬 icon)
4. Ask: "Tìm câu lạc bộ về công nghệ"
5. Explore the recommendation cards!

### For Developers

1. Review the [Requirements](./requirements.md) and [Design](./design.md) documents
2. Read the [Developer Guide](./DEVELOPER_GUIDE.md) for implementation details
3. Check the [API Documentation](./API_DOCUMENTATION.md) for endpoint specifications
4. Refer to [CSS Reference](./CSS_REFERENCE.md) for styling
5. Use [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md) when issues arise

### For Testers

1. Review [Requirements](./requirements.md) for acceptance criteria
2. Follow test scenarios in [Implementation Tasks](./tasks.md)
3. Use [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md) for debugging
4. Verify accessibility features from [User Guide](./USER_GUIDE.md)

## Feature Highlights

### Visual Recommendation Cards

Instead of plain text, users see beautiful cards with:
- 👥 Type icons (clubs) or 🎯 (activities)
- Bold, prominent names
- Brief descriptions
- 💡 Personalized reasons
- ✨ Color-coded relevance scores (0-100%)

### Intelligent AI Responses

The AI:
- Detects recommendation requests automatically
- Returns structured JSON with recommendation data
- Calculates relevance scores based on student profiles
- Provides personalized explanations
- Falls back gracefully to plain text when needed

### Responsive Design

- Works on desktop, tablet, and mobile
- Touch-friendly on mobile devices
- Smooth hover animations on desktop
- Adaptive layout for different screen sizes

### Accessibility

- Full screen reader support with ARIA labels
- Keyboard navigation (Tab, Enter, Space)
- Visible focus indicators
- Color is not the only indicator
- Semantic HTML structure

## Technology Stack

### Backend
- **Language**: C# (.NET)
- **Framework**: ASP.NET Core
- **AI Service**: Google Gemini AI
- **Data Format**: JSON

### Frontend
- **Language**: JavaScript (ES6+)
- **Styling**: CSS3 with custom properties
- **Framework**: Vanilla JS (no framework dependencies)

### Key Files

**Backend:**
- `Services/Chatbot/ChatbotService.cs` - Main service logic
- `Services/Chatbot/GeminiAIService.cs` - AI integration
- `Services/Chatbot/Models/StructuredResponse.cs` - Data models
- `BusinessObject/DTOs/Chatbot/ChatResponseDto.cs` - API DTOs
- `WebAPI/Controllers/ChatbotController.cs` - API endpoint

**Frontend:**
- `WebFE/wwwroot/js/chatbot.js` - Chat interface logic
- `WebFE/wwwroot/css/recommendation-cards.css` - Card styling
- `WebFE/Pages/Shared/_Layout.cshtml` - Layout integration

## Implementation Status

All tasks completed ✅

- [x] Backend data models
- [x] Enhanced ChatResponseDto
- [x] Structured prompt builder
- [x] Response parser
- [x] ChatbotService integration
- [x] Recommendation card CSS
- [x] Frontend message detector
- [x] Card renderer
- [x] Message display handler
- [x] Card click navigation
- [x] Enhanced typing indicator
- [x] Accessibility features
- [x] Layout integration
- [x] Fallback handling
- [x] End-to-end testing
- [x] Mobile responsive testing
- [x] Error handling testing
- [x] Accessibility testing
- [x] Performance optimization
- [x] Documentation

## Key Concepts

### Structured Response

A JSON format returned by Gemini AI containing:
```json
{
  "message": "Introductory text",
  "recommendations": [
    {
      "id": 123,
      "name": "Club Name",
      "type": "club",
      "description": "Brief description",
      "reason": "Why it matches",
      "relevanceScore": 95
    }
  ]
}
```

### Relevance Score

A 0-100% score indicating how well a recommendation matches the student:
- **90-100%**: Excellent match (dark green)
- **70-89%**: Good match (green)
- **50-69%**: Fair match (yellow)
- **0-49%**: Low match (orange)

### Fallback Handling

When AI returns plain text instead of JSON:
1. Backend attempts to parse JSON
2. If parsing fails, logs warning
3. Returns plain text to frontend
4. Frontend displays as normal message
5. No errors shown to user

## Common Use Cases

### Use Case 1: Technology Student Seeking Clubs

**User**: "Tìm câu lạc bộ về công nghệ"

**System**:
1. Detects recommendation request
2. Builds structured prompt with student context (IT major)
3. Sends to Gemini AI
4. Receives JSON with 3-5 tech club recommendations
5. Displays cards sorted by relevance score
6. Student clicks card → navigates to club detail page

### Use Case 2: Business Student Seeking Activities

**User**: "Gợi ý hoạt động cho sinh viên kinh doanh"

**System**:
1. Detects recommendation request
2. Includes business-related activities in prompt
3. AI returns structured response
4. Displays activity cards with 🎯 icon
5. Shows relevance scores based on business major
6. Student explores activities

### Use Case 3: General Question

**User**: "Câu lạc bộ là gì?"

**System**:
1. Detects NOT a recommendation request
2. Builds regular prompt
3. AI returns plain text explanation
4. Displays as normal message bubble
5. No cards shown

## Performance Metrics

### Target Metrics

- **Response Time**: < 5 seconds for recommendations
- **Card Render Time**: < 100ms
- **Mobile Performance**: 60fps animations
- **Accessibility**: WCAG 2.1 AA compliant
- **Browser Support**: 95%+ of users

### Optimization Techniques

- Backend caching (10-minute expiration)
- Debounced typing indicator
- Minified CSS/JS for production
- GPU-accelerated animations
- Lazy loading for large result sets

## Security Considerations

### Input Validation
- All user input sanitized on backend
- HTML escaped on frontend
- XSS prevention measures

### Authentication
- All endpoints require authentication
- Student context loaded from session
- No anonymous access

### Rate Limiting
- 15 requests per minute per student
- Prevents abuse and excessive API calls
- Returns 429 status when exceeded

### Data Privacy
- No sensitive data in logs
- Student data protected
- Compliant with privacy policies

## Testing

### Test Coverage

- ✅ Unit tests for parsing logic
- ✅ Integration tests for end-to-end flow
- ✅ Manual testing for visual appearance
- ✅ Accessibility testing with screen readers
- ✅ Mobile testing on real devices
- ✅ Cross-browser testing
- ✅ Performance testing

### Test Scenarios

See [Implementation Tasks](./tasks.md) for detailed test scenarios:
- Task 15: End-to-end flow
- Task 16: Mobile responsive
- Task 17: Error handling
- Task 18: Accessibility
- Task 19: Performance

## Troubleshooting

For common issues and solutions, see the [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md).

**Quick fixes:**
- Cards not showing → Clear cache, hard refresh
- Plain text only → Check backend logs for JSON parsing
- Styling broken → Verify CSS file loaded
- Click not working → Check JavaScript console
- Slow response → Check AI response time in logs

## Contributing

### Making Changes

1. Review relevant documentation first
2. Follow existing code patterns
3. Test thoroughly before committing
4. Update documentation if needed
5. Follow accessibility guidelines

### Code Style

- **Backend**: Follow C# conventions, use async/await
- **Frontend**: Use ES6+, avoid jQuery
- **CSS**: Use CSS variables, mobile-first approach
- **Comments**: Explain why, not what

### Documentation Updates

When making changes:
1. Update relevant documentation files
2. Keep examples current
3. Add new troubleshooting entries if needed
4. Update version history

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-12-02 | Initial release with full feature set |

## Support

### Getting Help

1. **Check Documentation**: Start with relevant guide
2. **Search Issues**: Look for similar problems
3. **Enable Logging**: Turn on debug logging
4. **Collect Information**: Gather error messages, logs, screenshots
5. **Contact Support**: Provide all collected information

### Reporting Bugs

Include:
- Steps to reproduce
- Expected vs actual behavior
- Browser and version
- Screenshots or screen recordings
- Console errors
- Backend log excerpts

### Feature Requests

Submit feature requests with:
- Use case description
- Expected behavior
- Benefits to users
- Implementation suggestions (optional)

## License

This feature is part of the EduXtend system. All rights reserved.

## Contact

- **Development Team**: dev@eduxted.edu.vn
- **Support**: support@eduxted.edu.vn
- **Documentation Issues**: docs@eduxted.edu.vn

---

**Last Updated**: December 2, 2024  
**Documentation Version**: 1.0  
**Feature Status**: ✅ Complete and Deployed
