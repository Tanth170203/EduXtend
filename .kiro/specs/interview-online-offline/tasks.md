# Implementation Plan

- [ ] 1. Update database schema and models
  - [x] 1.1 Create database migration to add InterviewType column






    - Add `InterviewType` column (NVARCHAR(50), NOT NULL, DEFAULT 'Offline')
    - Update existing records to have 'Offline' as default type
    - _Requirements: 1.5_
  
  - [x] 1.2 Update Interview entity model


    - Add `InterviewType` property to Interview.cs
    - Update Entity Framework configuration
    - _Requirements: 1.5_
  
  - [x] 1.3 Update Interview DTOs


    - Add `InterviewType` to ScheduleInterviewDto with validation
    - Add `InterviewType` to UpdateInterviewDto
    - Add `InterviewType` to InterviewDto
    - Make Location optional in DTOs (required only for Offline)
    - _Requirements: 1.1, 1.4_

- [ ] 2. Implement Google Meet integration service
  - [x] 2.1 Create IGoogleMeetService interface and implementation


    - Define CreateMeetLinkAsync method
    - Implement GoogleMeetService using Google Calendar API
    - Add configuration for Google service account credentials
    - Add error handling for API failures
    - _Requirements: 2.1, 2.2, 2.3, 2.5_
  
  - [ ]* 2.2 Write property test for Google Meet link generation
    - **Property 3: Google Meet link generation for online interviews**
    - **Validates: Requirements 2.1, 2.2**
  


  - [ ] 2.3 Register GoogleMeetService in dependency injection
    - Add service registration in Program.cs
    - Add Google API NuGet packages
    - _Requirements: 2.5_



- [ ] 3. Update InterviewService to handle online/offline interviews
  - [ ] 3.1 Modify ScheduleInterviewAsync method
    - Add validation for interview type
    - Add logic to generate Google Meet link for online interviews
    - Add validation to require location for offline interviews
    - Update error handling for Google Meet API failures
    - _Requirements: 1.4, 2.1, 2.2, 2.3_
  
  - [ ]* 3.2 Write property test for offline interview location validation
    - **Property 1: Offline interview location validation**
    - **Validates: Requirements 1.4**
  


  - [ ]* 3.3 Write property test for interview type persistence
    - **Property 2: Interview type persistence**
    - **Validates: Requirements 1.5**
  
  - [ ] 3.4 Modify UpdateInterviewAsync method
    - Add logic to handle interview type changes
    - Generate new Google Meet link when changing from Offline to Online
    - Clear meet link and require location when changing from Online to Offline
    - _Requirements: 5.1, 5.2, 5.3_
  
  - [ ]* 3.5 Write property test for interview type updates
    - **Property 14: Interview type update support**
    - **Validates: Requirements 5.1**
  
  - [ ]* 3.6 Write property test for offline-to-online conversion
    - **Property 15: Offline-to-online conversion generates meet link**
    - **Validates: Requirements 5.2**

  
  - [ ]* 3.7 Write property test for online-to-offline conversion
    - **Property 16: Online-to-offline conversion clears meet link**
    - **Validates: Requirements 5.3**

- [ ] 4. Implement notification system for interviews
  - [ ] 4.1 Create system notification for new interviews
    - Update ScheduleInterviewAsync to create notification with interview details
    - Include interview type, date, time, and location/meet link in notification
    - Format online interview notifications with clickable meet link
    - Format offline interview notifications with physical address
    - Mark notifications as unread by default
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  
  - [ ]* 4.2 Write property test for system notification creation
    - **Property 5: System notification creation**
    - **Validates: Requirements 3.1**
  
  - [ ]* 4.3 Write property test for notification content completeness
    - **Property 6: Notification content completeness**
    - **Validates: Requirements 3.2**
  
  - [ ]* 4.4 Write property test for online notification link formatting
    - **Property 7: Online notification link formatting**
    - **Validates: Requirements 3.3**
  
  - [x]* 4.5 Write property test for offline notification address display

    - **Property 8: Offline notification address display**
    - **Validates: Requirements 3.4**
  
  - [ ]* 4.6 Write property test for notification unread status
    - **Property 9: Notification unread status**
    - **Validates: Requirements 3.5**
  
  - [ ] 4.7 Create notifications for interview updates
    - Update UpdateInterviewAsync to send notifications


    - Include updated interview details in notification
    - _Requirements: 5.4_
  
  - [ ]* 4.8 Write property test for update notifications
    - **Property 17: Update notifications sent**
    - **Validates: Requirements 5.4**

- [ ] 5. Implement email notification system for interviews
  - [ ] 5.1 Add SendInterviewNotificationEmailAsync method to EmailService
    - Create professional HTML email template for interview notifications
    - Include club name, date, time, interview type, and location/meet link
    - Format Google Meet link as clickable hyperlink for online interviews
    - Format physical address clearly for offline interviews
    - Add error handling with logging (don't fail interview creation if email fails)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_
  
  - [ ]* 5.2 Write property test for email notification sending
    - **Property 10: Email notification sending**
    - **Validates: Requirements 4.1**
  
  - [ ]* 5.3 Write property test for email content completeness
    - **Property 11: Email content completeness**

    - **Validates: Requirements 4.2**
  
  - [ ]* 5.4 Write property test for online email link formatting
    - **Property 12: Online email link formatting**



    - **Validates: Requirements 4.3**
  
  - [ ]* 5.5 Write property test for offline email address formatting
    - **Property 13: Offline email address formatting**



    - **Validates: Requirements 4.4**
  
  - [ ] 5.6 Add SendInterviewUpdateEmailAsync method to EmailService
    - Create email template for interview updates
    - Include updated interview details
    - _Requirements: 5.4_
  



  - [ ] 5.7 Integrate email sending into InterviewService
    - Call email service from ScheduleInterviewAsync
    - Call email service from UpdateInterviewAsync
    - Ensure email failures don't prevent interview creation
    - _Requirements: 4.1, 4.5, 5.4_

- [ ] 6. Update frontend UI for interview scheduling
  - [ ] 6.1 Update interview scheduling modal/form
    - Add radio buttons or dropdown for interview type selection (Online/Offline)
    - Show/hide location input field based on interview type selection
    - Add client-side validation for offline location requirement
    - Update form submission to include interview type
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [ ] 6.2 Update interview details display page
    - Display interview type prominently
    - Show Google Meet link as clickable button/link for online interviews
    - Show physical address for offline interviews
    - Ensure meet links open in new tab


    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [ ] 6.3 Update interview list/table views
    - Add interview type column
    - Display appropriate icon for online/offline interviews
    - _Requirements: 6.1_


  
  - [ ] 6.4 Update interview edit form
    - Allow changing interview type
    - Handle location field visibility when type changes

    - Show warning when changing from online to offline (meet link will be lost)
    - _Requirements: 5.1, 5.2, 5.3_

- [ ] 7. Update API controller
  - [x] 7.1 Update InterviewController endpoints


    - Update POST /api/interviews endpoint to accept interview type
    - Update PUT /api/interviews/{id} endpoint to handle type changes
    - Add proper error responses for validation failures
    - Add error responses for Google Meet API failures
    - _Requirements: 1.1, 1.4, 2.3, 5.1_
  
  - [ ] 7.2 Update API documentation/Swagger
    - Document new InterviewType field
    - Document validation rules
    - Document error responses
    - _Requirements: 1.1_

- [ ] 8. Configuration and deployment setup
  - [ ] 8.1 Add Google Meet configuration to appsettings
    - Add GoogleMeet section with service account credentials
    - Add calendar ID and default duration settings
    - Document configuration requirements
    - _Requirements: 2.5_
  
  - [ ] 8.2 Update deployment documentation
    - Document Google Cloud setup requirements
    - Document required API scopes
    - Document environment variables
    - _Requirements: 2.5_
  
  - [ ] 8.3 Run database migration
    - Apply migration to development database
    - Verify existing interviews have default 'Offline' type
    - Test migration rollback
    - _Requirements: 1.5_

- [ ] 9. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 10. Integration testing
  - [ ]* 10.1 Write integration test for end-to-end online interview creation
    - Test creating online interview with Google Meet link generation
    - Verify database persistence
    - Verify notification creation
    - Verify email sending
    - _Requirements: 2.1, 2.2, 3.1, 4.1_
  
  - [ ]* 10.2 Write integration test for end-to-end offline interview creation
    - Test creating offline interview with physical location
    - Verify database persistence
    - Verify notification creation
    - Verify email sending
    - _Requirements: 1.4, 3.1, 4.1_
  
  - [ ]* 10.3 Write integration test for interview type updates
    - Test updating from offline to online
    - Test updating from online to offline
    - Verify notifications and emails are sent
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [ ]* 10.4 Write integration test for Google Meet API failure handling
    - Mock Google API failure
    - Verify interview creation is prevented
    - Verify appropriate error message is returned
    - _Requirements: 2.3_

- [ ] 11. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
