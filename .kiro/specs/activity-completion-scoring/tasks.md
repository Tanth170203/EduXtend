# Implementation Plan

- [x] 1. Create ClubMovementRecordService with point calculation logic





  - Create `Services/ClubMovementRecords/IClubMovementRecordService.cs` interface with `AwardActivityPointsAsync` method
  - Implement `Services/ClubMovementRecords/ClubMovementRecordService.cs` with point calculation, weekly limit checking, and score category mapping
  - Add helper methods for week calculation and score category determination
  - Register service in dependency injection container
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

- [x] 2. Add repository method for weekly point calculation





  - Add `GetDetailsByClubAndWeekAsync` method to `IClubMovementRecordRepository` interface
  - Implement method in `ClubMovementRecordRepository` to query details within a date range
  - _Requirements: 4.1, 4.5_

- [x] 3. Add CompleteActivity method to ActivityService





  - Add `CompleteActivityAsync` method signature to `IActivityService` interface
  - Implement validation logic in `ActivityService.CompleteActivityAsync` (check status, end time, not already completed)
  - Update Activity.Status to "Completed"
  - Call `ClubMovementRecordService.AwardActivityPointsAsync` to calculate and award points
  - Return success response with point details
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 8.1, 8.2, 8.5, 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 4. Add API endpoint for completing activities (Admin)





  - Add `CompleteActivity` POST endpoint to `AdminActivitiesController`
  - Extract current user ID from authentication context
  - Call `ActivityService.CompleteActivityAsync`
  - Return appropriate HTTP status codes and response messages
  - _Requirements: 2.1, 8.1, 8.4_

- [x] 5. Add API endpoint for completing activities (ClubManager)





  - Add `CompleteActivity` POST endpoint to `ClubManagerActivitiesController`
  - Verify user is manager of the activity's club
  - Call `ActivityService.CompleteActivityAsync`
  - Return appropriate HTTP status codes and response messages
  - _Requirements: 2.1, 8.1, 8.4_

- [x] 6. Add Complete Activity button to Admin Details page





  - Modify `WebFE/Pages/Activities/Details.cshtml` to add button in Quick Actions section
  - Implement visibility logic: show when EndTime < now AND Status == "Approved"
  - Hide button when Status == "Completed"
  - Add JavaScript function `completeActivity(activityId)` to call API endpoint
  - Display success toast notification with point details
  - Refresh page after successful completion
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 8.2, 8.3, 8.4, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 7. Add Complete Activity button to ClubManager Details page





  - Modify `WebFE/Pages/ClubManager/Activities/Details.cshtml` to add button in Quick Actions section
  - Implement same visibility logic as Admin page
  - Only show for activities owned by manager's club (not collaborated activities)
  - Add JavaScript function to call ClubManager API endpoint
  - Display success toast notification with point details
  - Refresh page after successful completion
  - _Requirements: 1.1, 1.2, 1.3, 1.5, 2.1, 8.2, 8.3, 8.4, 10.1, 10.2, 10.3, 10.4, 10.5_

- [x] 8. Add error handling and logging





  - Add try-catch blocks in service methods with appropriate error messages
  - Log errors with activity ID, club ID, and error details
  - Implement transaction rollback on database errors
  - Add concurrency handling to prevent duplicate completions
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [x] 9. Write integration tests for complete activity flow






  - Test successful activity completion with point calculation
  - Test weekly limit enforcement for club activities
  - Test semester limit (100 points cap)
  - Test collaboration point awards
  - Test validation errors (not approved, not ended, already completed)
  - Test activity with no club (no points awarded)
  - Test criterion not found scenario
  - _Requirements: All requirements_
