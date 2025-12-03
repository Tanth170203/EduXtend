# Chatbot Module - File List

## Backend Files

### Services
```
Backend/Services/Chatbot/
├── ChatbotMetrics.cs              # Metrics data model
├── ChatbotService.cs              # Main chatbot service
├── GeminiAIOptions.cs             # Gemini AI configuration options
├── GeminiApiClient.cs             # Gemini API client
├── IChatbotService.cs             # Chatbot service interface
└── IGeminiApiClient.cs            # Gemini API client interface

Backend/Services/Recommendations/
├── IRecommendationEngine.cs       # Recommendation engine interface
└── RecommendationEngine.cs        # Recommendation logic
```

### Repositories
```
Backend/Repositories/ChatSessions/
├── ChatSessionRepository.cs       # Chat session data access
└── IChatSessionRepository.cs      # Repository interface
```

### Controllers
```
Backend/Controllers/
├── ChatbotController.cs           # Chat API endpoints
└── ChatbotMetricsController.cs    # Metrics API endpoints (Admin)
```

### Middleware
```
Backend/Middleware/
└── ChatbotRateLimitAttribute.cs   # Rate limiting middleware
```

### Configuration
```
Backend/Configuration/
└── ChatbotOptions.cs              # Chatbot configuration options
```

### Models
```
Backend/Models/
├── ChatSession.cs                 # Chat session entity
└── ChatMessage.cs                 # Chat message entity
```

### DTOs
```
Backend/DTOs/Chatbot/
├── ChatHistoryDto.cs              # Chat history response
├── ChatMessageDto.cs              # Single message DTO
├── ChatRequestDto.cs              # Chat request payload
├── ChatResponseDto.cs             # Chat response payload
├── ChatSessionSummaryDto.cs       # Session summary
└── RecommendationDto.cs           # Recommendation data
```

## Frontend Files

```
Frontend/
├── js/
│   └── chatbot.js                 # Chat UI and API integration
└── css/
    └── chatbot.css                # Chat interface styling
```

## Documentation

```
Documentation/
├── MIGRATION_GUIDE.md             # Step-by-step integration guide
└── CONFIGURATION_EXAMPLE.md       # Configuration examples
```

## Root Files

```
ChatbotModule/
├── README.md                      # Main documentation
└── FILE_LIST.md                   # This file
```

## Total Files

- **Backend**: 18 files
  - Services: 8 files
  - Repositories: 2 files
  - Controllers: 2 files
  - Middleware: 1 file
  - Configuration: 1 file
  - Models: 2 files
  - DTOs: 6 files

- **Frontend**: 2 files
  - JavaScript: 1 file
  - CSS: 1 file

- **Documentation**: 3 files

**Total: 23 files**

## Dependencies

### Backend Dependencies
- ASP.NET Core 8.0+
- Entity Framework Core
- System.IdentityModel.Tokens.Jwt
- Microsoft.Extensions.Http
- Microsoft.Extensions.Caching.Memory

### Frontend Dependencies
- Bootstrap Icons (for chat icon)
- Modern browser with ES6+ support

## Database Tables

The module creates 2 database tables:

1. **ChatSessions**
   - Id (PK)
   - UserId (FK to Users)
   - StudentId (FK to Students, nullable)
   - Title
   - CreatedAt
   - LastMessageAt

2. **ChatMessages**
   - Id (PK)
   - SessionId (FK to ChatSessions)
   - Role (user/assistant)
   - Content
   - CreatedAt

## API Endpoints

### Public Endpoints (Authenticated Users)
- `POST /api/chatbot/chat` - Send message
- `GET /api/chatbot/sessions` - Get user sessions
- `GET /api/chatbot/sessions/{id}/history` - Get chat history
- `DELETE /api/chatbot/sessions/{id}` - Delete session

### Admin Endpoints
- `GET /api/chatbotmetrics` - Get metrics
- `POST /api/chatbotmetrics/reset` - Reset metrics

## File Sizes (Approximate)

- **ChatbotService.cs**: ~15 KB
- **GeminiApiClient.cs**: ~12 KB
- **RecommendationEngine.cs**: ~8 KB
- **ChatbotController.cs**: ~10 KB
- **chatbot.js**: ~20 KB
- **chatbot.css**: ~15 KB

**Total Module Size**: ~150 KB (excluding dependencies)

## Integration Checklist

- [ ] Copy all Backend files to your project
- [ ] Copy all Frontend files to your project
- [ ] Add DbSets to DbContext
- [ ] Create and run database migration
- [ ] Configure services in Program.cs
- [ ] Add configuration to appsettings.json
- [ ] Set environment variables (production)
- [ ] Add chatbot UI to layout or pages
- [ ] Test chatbot functionality
- [ ] Verify metrics endpoint (admin)

## Maintenance

### Regular Tasks
- Monitor API usage and costs
- Review and adjust rate limits
- Update Gemini model version
- Backup chat history data
- Review and improve recommendations

### Updates
- Check for Gemini API updates
- Update model name if new versions available
- Review and optimize system prompts
- Enhance recommendation algorithms
