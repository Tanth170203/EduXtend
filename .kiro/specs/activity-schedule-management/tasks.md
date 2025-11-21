# Implementation Plan

- [x] 1. Create DTOs for Schedule Management





  - Create `CreateActivityScheduleDto` and `CreateActivityScheduleAssignmentDto` in `BusinessObject/DTOs/Activity/`
  - Create `UpdateActivityScheduleDto` and `UpdateActivityScheduleAssignmentDto` in `BusinessObject/DTOs/Activity/`
  - Create `ActivityScheduleDto` and `ActivityScheduleAssignmentDto` for response data
  - Add `Schedules` property to `ActivityDetailDto`
  - _Requirements: 1.3, 2.2, 3.2_

- [x] 2. Create Repository Interfaces and Implementations




  - [x] 2.1 Create ActivitySchedule Repository


    - Create `IActivityScheduleRepository` interface in `Repositories/ActivitySchedules/`
    - Implement `ActivityScheduleRepository` with methods: `GetByIdAsync`, `GetByActivityIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
    - Register repository in DI container
    - _Requirements: 1.4, 3.3_
  
  - [x] 2.2 Create ActivityScheduleAssignment Repository


    - Create `IActivityScheduleAssignmentRepository` interface in `Repositories/ActivityScheduleAssignments/`
    - Implement `ActivityScheduleAssignmentRepository` with methods: `GetByIdAsync`, `GetByScheduleIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
    - Register repository in DI container
    - _Requirements: 2.4, 3.5_

- [x] 3. Implement Service Layer Methods





  - [x] 3.1 Add helper method to check activity type


    - Add `IsComplexActivity(ActivityType type)` method to `ActivityService`
    - Method returns true for Events, Competitions, Collaborations
    - Method returns false for ClubMeeting, ClubTraining, ClubWorkshop
    - _Requirements: 1.1, 1.2_
  
  - [x] 3.2 Implement schedule creation logic


    - Add `AddSchedulesToActivityAsync(int activityId, List<CreateActivityScheduleDto> schedules)` method
    - Validate activity type using `IsComplexActivity()`
    - Validate schedule times are within activity time range
    - Sort schedules by StartTime and assign Order
    - Create ActivitySchedule entities and save to database
    - Create ActivityScheduleAssignment entities for each schedule
    - _Requirements: 1.3, 1.4, 1.5, 2.1, 2.4, 5.1, 5.2, 5.4_
  
  - [x] 3.3 Implement schedule update logic


    - Add `UpdateActivitySchedulesAsync(int activityId, List<UpdateActivityScheduleDto> schedules)` method
    - Validate activity type using `IsComplexActivity()`
    - Determine which schedules to delete, update, or add
    - Delete removed schedules (cascade delete assignments)
    - Update existing schedules and their assignments
    - Add new schedules with assignments
    - Re-sort and update Order for all schedules
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.4_
  
  - [x] 3.4 Implement validation methods


    - Add `ValidateScheduleTime()` method to validate EndTime > StartTime
    - Add `ValidateAssignment()` method to validate UserId or ResponsibleName exists
    - Add validation for Title, Description, Notes length limits
    - _Requirements: 5.1, 5.2, 5.3, 5.5_
  
  - [x] 3.5 Update existing activity creation/update methods


    - Modify `CreateActivityAsync` to accept optional schedules parameter
    - Modify `UpdateActivityAsync` to handle schedule updates
    - Add `GetActivitySchedulesAsync(int activityId)` method to retrieve schedules with assignments
    - _Requirements: 1.4, 3.4, 4.1, 4.2_

- [x] 4. Update API Endpoints





  - [x] 4.1 Update Club Manager Create endpoint


    - Modify `POST /api/activity/club-manager` to accept schedules in request body
    - Create request DTO with Activity and Schedules properties
    - Call service method to create activity with schedules
    - Return created activity with schedules
    - _Requirements: 1.4_
  
  - [x] 4.2 Update Club Manager Edit endpoint


    - Modify `PUT /api/activity/club-manager/{id}` to accept schedules in request body
    - Create request DTO with Activity and Schedules properties
    - Call service methods to update activity and schedules
    - Return updated activity with schedules
    - _Requirements: 3.4_
  
  - [x] 4.3 Update Activity Detail endpoint


    - Modify `GET /api/activity/{id}` to include schedules for complex activities
    - Check activity type using `IsComplexActivity()`
    - Load schedules with assignments if complex activity
    - Return activity detail with schedules
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 5. Implement Frontend Schedule Management UI




  - [x] 5.1 Create Schedule Management Section for Create Page


    - Add schedule management section HTML to `WebFE/Pages/ClubManager/Activities/Create.cshtml`
    - Add JavaScript to show/hide section based on activity type
    - Implement `addScheduleItem()` function to dynamically add schedule forms
    - Implement `removeScheduleItem()` function to remove schedule forms
    - Add validation for schedule times (EndTime > StartTime)
    - _Requirements: 1.1, 1.2, 1.3, 5.1, 5.2_
  
  - [x] 5.2 Create Assignment Management UI

    - Add assignment section HTML within schedule item template
    - Implement `addAssignment()` function to add assignment forms
    - Implement `removeAssignment()` function to remove assignment forms
    - Add validation for assignment fields (name or user required)
    - _Requirements: 2.1, 2.2, 2.3, 5.3_
  
  - [x] 5.3 Implement Form Submission Logic for Create


    - Collect all schedule data from form
    - Collect all assignment data for each schedule
    - Build JSON payload with activity and schedules
    - Submit to API endpoint
    - Handle success/error responses
    - _Requirements: 1.4, 2.4_
  
  - [x] 5.4 Create Schedule Management Section for Edit Page


    - Add schedule management section HTML to `WebFE/Pages/ClubManager/Activities/Edit.cshtml`
    - Load existing schedules and assignments on page load
    - Populate schedule forms with existing data
    - Implement add/edit/delete functionality
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  
  - [x] 5.5 Implement Form Submission Logic for Edit

    - Collect all schedule data (including IDs for existing schedules)
    - Collect all assignment data (including IDs for existing assignments)
    - Build JSON payload with activity and schedules
    - Submit to API endpoint
    - Handle success/error responses
    - _Requirements: 3.4, 3.5_

- [x] 6. Implement Frontend Timeline Display





  - [x] 6.1 Create Timeline Display Section


    - Add timeline section HTML to `WebFE/Pages/ClubManager/Activities/Details.cshtml`
    - Add CSS styles for timeline visualization
    - Implement JavaScript to show/hide section based on activity type
    - _Requirements: 4.1, 4.5_
  
  - [x] 6.2 Render Timeline Items


    - Fetch activity detail with schedules from API
    - Render timeline items sorted by time
    - Display StartTime, EndTime, Title, Description, Notes
    - Display assignments with ResponsibleName and Role
    - _Requirements: 4.2, 4.3, 4.4_

- [x] 7. Update Admin Pages





  - [x] 7.1 Update Admin Create Page


    - Apply same schedule management UI to `WebFE/Pages/Admin/Activities/Create.cshtml`
    - Update form submission to use Admin API endpoint
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [x] 7.2 Update Admin Edit Page


    - Apply same schedule management UI to `WebFE/Pages/Admin/Activities/Edit.cshtml`
    - Update form submission to use Admin API endpoint
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  
  - [x] 7.3 Update Admin Details Page


    - Apply same timeline display to `WebFE/Pages/Admin/Activities/Details.cshtml`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 8. Add Unit Tests






  - [x] 8.1 Test Service Layer

    - Test `IsComplexActivity()` method with all activity types
    - Test `AddSchedulesToActivityAsync()` with valid data
    - Test schedule validation (time ranges, required fields)
    - Test assignment validation
    - Test `UpdateActivitySchedulesAsync()` (add, update, delete scenarios)
    - Test error cases (invalid activity type, invalid times)
    - _Requirements: All_
  
  - [x] 8.2 Test Repository Layer


    - Test ActivitySchedule CRUD operations
    - Test ActivityScheduleAssignment CRUD operations
    - Test cascade delete behavior
    - _Requirements: All_

- [x] 9. Add Integration Tests






  - [x] 9.1 Test API Endpoints


    - Test creating activity with schedules
    - Test updating activity with schedules
    - Test getting activity with schedules
    - Test validation errors are returned correctly
    - _Requirements: All_
  
  - [x] 9.2 Test Frontend Integration


    - Test schedule UI shows/hides based on activity type
    - Test adding/removing schedule items
    - Test adding/removing assignments
    - Test form validation
    - Test data submission and response handling
    - _Requirements: All_
