# Design Document

## Overview

Tính năng này mở rộng hệ thống phỏng vấn hiện tại để hỗ trợ hai hình thức: phỏng vấn trực tuyến (online) và phỏng vấn trực tiếp (offline). Hệ thống sẽ tích hợp với Google Meet API để tự động tạo link họp cho phỏng vấn online, đồng thời gửi thông báo qua cả hệ thống nội bộ và email cho ứng viên.

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   Web UI        │
│  (Razor Pages)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  API Controller │
│ (Interview)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────────┐
│ Interview       │─────▶│ Google Meet      │
│ Service         │      │ Service          │
└────────┬────────┘      └──────────────────┘
         │
         ├──────────────┐
         │              │
         ▼              ▼
┌─────────────────┐  ┌─────────────────┐
│ Notification    │  │ Email           │
│ Service         │  │ Service         │
└─────────────────┘  └─────────────────┘
         │              │
         ▼              ▼
┌─────────────────┐  ┌─────────────────┐
│ Database        │  │ SMTP Server     │
└─────────────────┘  └─────────────────┘
```

### Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server (Entity Framework Core)
- **Frontend**: Razor Pages with JavaScript
- **Google Integration**: Google.Apis.Calendar.v3 NuGet package
- **Email**: System.Net.Mail (SMTP)

## Components and Interfaces

### 1. Data Models

#### Interview Model (Updated)

```csharp
public class Interview
{
    public int Id { get; set; }
    public int JoinRequestId { get; set; }
    public DateTime ScheduledDate { get; set; }
    
    // NEW: Interview type
    public string InterviewType { get; set; } // "Online" or "Offline"
    
    // Location: Physical address for Offline, Google Meet link for Online
    public string Location { get; set; }
    
    public string? Notes { get; set; }
    public string? Evaluation { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CreatedById { get; set; }
    
    // Navigation properties
    public virtual JoinRequest JoinRequest { get; set; }
    public virtual User CreatedBy { get; set; }
}
```

#### ScheduleInterviewDto (Updated)

```csharp
public class ScheduleInterviewDto
{
    [Required]
    public int JoinRequestId { get; set; }
    
    [Required]
    public DateTime ScheduledDate { get; set; }
    
    // NEW: Interview type
    [Required]
    [RegularExpression("^(Online|Offline)$")]
    public string InterviewType { get; set; }
    
    // Location: Required only for Offline interviews
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
```

### 2. Services

#### IGoogleMeetService (New)

```csharp
public interface IGoogleMeetService
{
    /// <summary>
    /// Creates a Google Meet link for an interview
    /// </summary>
    /// <param name="summary">Meeting title</param>
    /// <param name="description">Meeting description</param>
    /// <param name="startTime">Meeting start time</param>
    /// <param name="durationMinutes">Meeting duration in minutes</param>
    /// <returns>Google Meet link URL</returns>
    Task<string> CreateMeetLinkAsync(
        string summary, 
        string description, 
        DateTime startTime, 
        int durationMinutes = 60);
}
```

#### IInterviewService (Updated)

```csharp
public interface IInterviewService
{
    Task<InterviewDto?> GetByIdAsync(int id);
    Task<InterviewDto?> GetByJoinRequestIdAsync(int joinRequestId);
    Task<List<InterviewDto>> GetMyInterviewsAsync(int userId);
    
    // Updated to handle online/offline interviews
    Task<InterviewDto> ScheduleInterviewAsync(
        ScheduleInterviewDto dto, 
        int createdById);
    
    Task<InterviewDto> UpdateInterviewAsync(
        int id, 
        UpdateInterviewDto dto);
    
    Task<InterviewDto> UpdateEvaluationAsync(
        int id, 
        UpdateEvaluationDto dto);
    
    Task<bool> DeleteAsync(int id);
}
```

#### IEmailService (Updated)

```csharp
public interface IEmailService
{
    // Existing methods...
    
    /// <summary>
    /// Sends interview notification email to applicant
    /// </summary>
    Task SendInterviewNotificationEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes);
    
    /// <summary>
    /// Sends interview update notification email
    /// </summary>
    Task SendInterviewUpdateEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes);
}
```

### 3. Google Meet Integration

#### GoogleMeetService Implementation

```csharp
public class GoogleMeetService : IGoogleMeetService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleMeetService> _logger;
    
    public async Task<string> CreateMeetLinkAsync(
        string summary, 
        string description, 
        DateTime startTime, 
        int durationMinutes = 60)
    {
        // Use Google Calendar API to create event with conferencing
        // Returns the Google Meet link from the event
    }
}
```

**Configuration Requirements:**
- Google Cloud Project with Calendar API enabled
- Service Account credentials or OAuth 2.0 credentials
- Scopes: `https://www.googleapis.com/auth/calendar.events`

## Data Models

### Database Schema Changes

#### Migration: AddInterviewTypeColumn

```sql
ALTER TABLE Interviews
ADD InterviewType NVARCHAR(50) NOT NULL DEFAULT 'Offline';

-- Update existing records
UPDATE Interviews
SET InterviewType = 'Offline'
WHERE InterviewType IS NULL;
```

### Entity Framework Configuration

```csharp
modelBuilder.Entity<Interview>(entity =>
{
    entity.Property(e => e.InterviewType)
        .IsRequired()
        .HasMaxLength(50)
        .HasDefaultValue("Offline");
});
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Offline interview location validation
*For any* interview submission with "Offline" type, if the location field is empty or contains only whitespace, the system should reject the submission with a validation error.
**Validates: Requirements 1.4**

### Property 2: Interview type persistence
*For any* interview created with a specific type (Online or Offline), querying that interview from the database should return the same interview type.
**Validates: Requirements 1.5**

### Property 3: Google Meet link generation for online interviews
*For any* interview created with "Online" type, the system should automatically generate a Google Meet link and store it in the location field.
**Validates: Requirements 2.1, 2.2**

### Property 4: Online interview notifications contain meet link
*For any* online interview created, all notifications (system and email) should contain the Google Meet link.
**Validates: Requirements 2.4**

### Property 5: System notification creation
*For any* interview successfully created, the system should create exactly one system notification for the applicant.
**Validates: Requirements 3.1**

### Property 6: Notification content completeness
*For any* interview created, the system notification should contain the interview date, time, type, and location/meeting link.
**Validates: Requirements 3.2**

### Property 7: Online notification link formatting
*For any* online interview notification, the Google Meet link should be present and properly formatted as a clickable element.
**Validates: Requirements 3.3**

### Property 8: Offline notification address display
*For any* offline interview notification, the physical address should be present in the notification message.
**Validates: Requirements 3.4**

### Property 9: Notification unread status
*For any* interview created, the generated notification should be marked as unread (IsRead = false).
**Validates: Requirements 3.5**

### Property 10: Email notification sending
*For any* interview successfully created, the system should send exactly one email notification to the applicant's registered email address.
**Validates: Requirements 4.1**

### Property 11: Email content completeness
*For any* interview created, the email notification should contain the club name, interview date and time, interview type, and location/meeting link.
**Validates: Requirements 4.2**

### Property 12: Online email link formatting
*For any* online interview email, the Google Meet link should be present as a clickable hyperlink (HTML anchor tag).
**Validates: Requirements 4.3**

### Property 13: Offline email address formatting
*For any* offline interview email, the physical address should be present with clear formatting.
**Validates: Requirements 4.4**

### Property 14: Interview type update support
*For any* existing interview, updating it with a different interview type (Online to Offline or vice versa) should be accepted and persisted.
**Validates: Requirements 5.1**

### Property 15: Offline-to-online conversion generates meet link
*For any* interview with "Offline" type, when updated to "Online" type, the system should generate a new Google Meet link.
**Validates: Requirements 5.2**

### Property 16: Online-to-offline conversion clears meet link
*For any* interview with "Online" type, when updated to "Offline" type with a physical location, the system should replace the meet link with the provided location.
**Validates: Requirements 5.3**

### Property 17: Update notifications sent
*For any* interview update, the system should send both a system notification and an email notification to the applicant.
**Validates: Requirements 5.4**

## Error Handling

### Error Scenarios

1. **Google Meet API Failure**
   - Scenario: Google API is unavailable or returns an error
   - Handling: Return clear error message, prevent interview creation, log error details
   - User Message: "Không thể tạo link Google Meet. Vui lòng thử lại sau hoặc chọn phỏng vấn trực tiếp."

2. **Invalid Interview Type**
   - Scenario: Interview type is not "Online" or "Offline"
   - Handling: Return validation error
   - User Message: "Hình thức phỏng vấn không hợp lệ. Vui lòng chọn Online hoặc Offline."

3. **Missing Location for Offline Interview**
   - Scenario: Offline interview submitted without location
   - Handling: Return validation error
   - User Message: "Vui lòng nhập địa chỉ phỏng vấn."

4. **Email Sending Failure**
   - Scenario: SMTP server is unavailable
   - Handling: Log error, continue with interview creation, notification still created
   - User Message: (No user-facing error, interview created successfully)

5. **Notification Creation Failure**
   - Scenario: Database error when creating notification
   - Handling: Log warning, continue with interview creation
   - User Message: (No user-facing error, interview created successfully)

### Validation Rules

```csharp
public class ScheduleInterviewDtoValidator : AbstractValidator<ScheduleInterviewDto>
{
    public ScheduleInterviewDtoValidator()
    {
        RuleFor(x => x.InterviewType)
            .NotEmpty()
            .Must(type => type == "Online" || type == "Offline")
            .WithMessage("Interview type must be 'Online' or 'Offline'");
        
        RuleFor(x => x.Location)
            .NotEmpty()
            .When(x => x.InterviewType == "Offline")
            .WithMessage("Location is required for offline interviews");
        
        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.Now)
            .WithMessage("Interview date must be in the future");
    }
}
```

## Testing Strategy

### Unit Testing

Unit tests will cover:
- DTO validation logic (offline requires location, online doesn't)
- Service method behavior with mocked dependencies
- Email template generation
- Notification message formatting
- Error handling paths

**Test Framework**: xUnit
**Mocking**: Moq

### Property-Based Testing

Property-based tests will verify universal properties across all inputs using FsCheck library.

**Property Testing Library**: FsCheck (for C#/.NET)
**Configuration**: Minimum 100 iterations per property test

Each property-based test will:
- Generate random interview data (dates, types, locations, notes)
- Execute the system operation
- Verify the correctness property holds

**Test Tagging Format**: Each property test must include a comment:
```csharp
// Feature: interview-online-offline, Property 1: Offline interview location validation
```

### Integration Testing

Integration tests will cover:
- End-to-end interview creation flow
- Database persistence and retrieval
- Google Meet API integration (with test credentials)
- Email sending (with test SMTP server)
- Notification system integration

### Manual Testing Checklist

- [ ] Create online interview and verify Google Meet link is generated
- [ ] Create offline interview and verify location is stored
- [ ] Verify system notification appears in user's notification list
- [ ] Verify email is received with correct content
- [ ] Update interview from offline to online
- [ ] Update interview from online to offline
- [ ] Click Google Meet link and verify it opens correctly
- [ ] Test with various date/time formats
- [ ] Test with special characters in location/notes

## Security Considerations

1. **Google API Credentials**
   - Store credentials securely in Azure Key Vault or appsettings (encrypted)
   - Use service account with minimal required permissions
   - Rotate credentials periodically

2. **Email Content**
   - Sanitize user input before including in emails
   - Prevent email injection attacks
   - Use HTML encoding for email templates

3. **Authorization**
   - Only club managers can create/update interviews
   - Users can only view their own interviews
   - Validate join request ownership before creating interview

4. **Data Validation**
   - Validate all input fields
   - Prevent SQL injection through parameterized queries
   - Validate Google Meet URLs before storing

## Performance Considerations

1. **Google API Calls**
   - Implement retry logic with exponential backoff
   - Cache API responses where appropriate
   - Set reasonable timeouts (5-10 seconds)

2. **Email Sending**
   - Send emails asynchronously (fire-and-forget)
   - Use background job queue for high volume
   - Implement rate limiting to prevent SMTP throttling

3. **Database Queries**
   - Add index on `InterviewType` column for filtering
   - Use eager loading for related entities
   - Implement pagination for interview lists

## Deployment Considerations

### Configuration

```json
{
  "GoogleMeet": {
    "ServiceAccountEmail": "service-account@project.iam.gserviceaccount.com",
    "PrivateKey": "-----BEGIN PRIVATE KEY-----\n...",
    "CalendarId": "primary",
    "DefaultDurationMinutes": 60
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "noreply@eduxtend.com",
    "SmtpPassword": "***",
    "FromEmail": "noreply@eduxtend.com",
    "FromName": "EduXtend System"
  }
}
```

### Database Migration

```bash
# Create migration
dotnet ef migrations add AddInterviewTypeColumn --project DataAccess

# Apply migration
dotnet ef database update --project DataAccess
```

### Dependencies

```xml
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.68.0.3421" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
```

## Future Enhancements

1. **Calendar Integration**
   - Add to applicant's Google Calendar automatically
   - Send calendar invites (.ics files)

2. **Reminder System**
   - Send reminder notifications 24 hours before interview
   - Send reminder emails 1 hour before interview

3. **Video Recording**
   - Option to record Google Meet sessions
   - Store recordings in cloud storage

4. **Interview Feedback**
   - Allow applicants to provide feedback after interview
   - Rating system for interview experience

5. **Multiple Interviewers**
   - Support multiple club managers in one interview
   - Add all interviewers to Google Meet event

6. **Rescheduling**
   - Allow applicants to request reschedule
   - Approval workflow for reschedule requests
