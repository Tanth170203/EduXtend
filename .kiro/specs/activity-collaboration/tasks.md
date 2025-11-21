# Implementation Plan

- [x] 1. Database schema and model updates





  - Add ClubCollaborationId and CollaborationPoint properties to Activity model
  - Create and apply database migration for new columns with foreign key constraint
  - Update EduXtendDbContext to include CollaboratingClub navigation property
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2. Update DTOs for collaboration support





  - [x] 2.1 Add collaboration fields to AdminCreateActivityDto and AdminUpdateActivityDto


    - Add ClubCollaborationId and CollaborationPoint properties
    - _Requirements: 2.1, 4.1, 4.2_
  
  - [x] 2.2 Add collaboration fields to ClubCreateActivityDto


    - Add ClubCollaborationId and CollaborationPoint properties
    - _Requirements: 2.1, 3.1, 3.2, 5.1_
  
  - [x] 2.3 Add collaboration fields to ActivityDetailDto and ActivityListItemDto


    - Add ClubCollaborationId, CollaboratingClubName, and CollaborationPoint properties
    - _Requirements: 2.5, 7.1_

- [x] 3. Implement repository layer methods





  - [x] 3.1 Add GetAvailableCollaboratingClubsAsync method


    - Query clubs excluding specified club ID
    - Return list of clubs with basic info (Id, Name, LogoUrl, member count)
    - _Requirements: 2.2, 8.1_
  
  - [x] 3.2 Add GetClubByIdAsync method if not exists

    - Retrieve club by ID for validation
    - _Requirements: 8.1_
  
  - [x] 3.3 Update existing queries to include CollaboratingClub navigation


    - Modify GetByIdWithDetailsAsync to eager load CollaboratingClub
    - Update mapping logic to include collaborating club name
    - _Requirements: 2.5, 7.1_

- [x] 4. Implement service layer validation and business logic





  - [x] 4.1 Create ValidateCollaborationSettingsAsync method


    - Validate collaboration fields based on ActivityType and user role
    - Check point ranges (1-3 for Collaboration, 1-10 for Movement)
    - Ensure ClubCollaborationId references valid club
    - Prevent selecting same club as organizer and collaborator
    - _Requirements: 3.1, 3.2, 3.3, 4.1, 4.2, 5.1, 8.1, 8.2, 8.3, 8.4, 8.5_
  
  - [x] 4.2 Update AdminCreateAsync method


    - Add collaboration field handling
    - Call validation method before creating activity
    - Set collaboration fields based on ActivityType
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 4.3 Update AdminUpdateAsync method


    - Add collaboration field handling
    - Call validation method before updating
    - Clear collaboration fields when ActivityType changes to non-collaboration
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [x] 4.4 Update ClubCreateAsync method


    - Add collaboration field handling for Club Manager
    - Call validation method with Club Manager role
    - Handle different rules for ClubCollaboration vs SchoolCollaboration
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 4.5 Update ClubUpdateAsync method


    - Add collaboration field handling
    - Call validation method
    - Clear collaboration fields appropriately when type changes
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 5. Implement registration eligibility logic





  - [x] 5.1 Update RegisterAsync method to check collaboration membership


    - For non-public ClubCollaboration activities, allow members of both organizing and collaborating clubs
    - Keep existing public activity logic (anyone can register)
    - Keep existing club-only logic for non-collaboration activities
    - _Requirements: 6.1, 6.2, 6.5_
  
  - [x] 5.2 Update CanUserRegisterAsync helper method


    - Add collaboration-specific eligibility checks
    - Check membership in either organizing or collaborating club
    - _Requirements: 6.1, 6.2, 6.5_

- [x] 6. Implement point assignment logic for attendance





  - [x] 6.1 Create GetParticipantPointsAsync helper method


    - Determine if user is from organizing club (gets Movement Points)
    - Determine if user is from collaborating club (gets Collaboration Points)
    - Return appropriate point value based on membership
    - _Requirements: 6.3, 6.4_
  
  - [x] 6.2 Update SetAttendanceAsync to use collaboration points


    - Call GetParticipantPointsAsync to determine correct points
    - Apply appropriate points when marking attendance
    - _Requirements: 6.3, 6.4_
  

  - [x] 6.3 Update SetClubAttendanceAsync to use collaboration points

    - Call GetParticipantPointsAsync to determine correct points
    - Apply appropriate points when marking attendance
    - _Requirements: 6.3, 6.4_

- [x] 7. Update API controllers





  - [x] 7.1 Update AdminActivitiesController Create endpoint


    - Accept new collaboration fields in request DTO
    - Pass collaboration fields to service layer
    - Return collaboration fields in response
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 7.2 Update AdminActivitiesController Update endpoint


    - Accept new collaboration fields in request DTO
    - Pass collaboration fields to service layer
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [x] 7.3 Add GetAvailableCollaboratingClubs endpoint


    - Create new endpoint to fetch available clubs for selection
    - Accept excludeClubId parameter for Club Manager context
    - Return list of clubs
    - _Requirements: 2.2_
  
  - [x] 7.4 Update ClubManager ActivitiesController Create endpoint


    - Accept new collaboration fields in request DTO
    - Pass collaboration fields to service layer
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 7.5 Update ClubManager ActivitiesController Update endpoint


    - Accept new collaboration fields in request DTO
    - Pass collaboration fields to service layer
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 8. Implement frontend UI for Admin Create/Edit





  - [x] 8.1 Create club selection modal component


    - Build reusable modal with club list
    - Add search/filter functionality
    - Display club logo, name, and member count
    - Handle single selection and return selected club ID
    - _Requirements: 2.1, 2.2, 2.3_
  
  - [x] 8.2 Update Admin Create Activity page


    - Add club selection button (shown when ActivityType is ClubCollaboration or SchoolCollaboration)
    - Add Collaboration Point input field (range 1-3, shown for collaboration types)
    - Hide Movement Point field for Admin collaboration activities
    - Implement field visibility logic based on ActivityType
    - Wire up club selection modal
    - _Requirements: 2.1, 2.4, 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 8.3 Update Admin Edit Activity page


    - Add same collaboration fields as Create page
    - Load existing collaboration data
    - Handle ActivityType changes (clear fields when switching types)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [x] 8.4 Add client-side validation

    - Validate Collaboration Point range (1-3)
    - Validate required fields based on ActivityType
    - Show validation error messages
    - _Requirements: 8.3, 8.4, 8.5_

- [x] 9. Implement frontend UI for Club Manager Create/Edit





  - [x] 9.1 Update Club Manager Create Activity page


    - Add club selection button (shown only for ClubCollaboration type)
    - Add Collaboration Point input field (range 1-3, shown for ClubCollaboration)
    - Add Movement Point input field (range 1-10, shown for both collaboration types)
    - Implement field visibility logic based on ActivityType
    - Wire up club selection modal (exclude current club)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5, 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 9.2 Update Club Manager Edit Activity page


    - Add same collaboration fields as Create page
    - Load existing collaboration data
    - Handle ActivityType changes appropriately
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  

  - [x] 9.3 Add client-side validation

    - Validate Collaboration Point range (1-3)
    - Validate Movement Point range (1-10)
    - Validate required fields based on ActivityType
    - Show validation error messages
    - _Requirements: 8.3, 8.4, 8.5_

- [x] 10. Update activity display pages





  - [x] 10.1 Update activity detail view to show collaboration info


    - Display collaborating club name and logo if present
    - Show collaboration point value
    - Indicate collaboration type in activity details
    - _Requirements: 2.5_
  

  - [x] 10.2 Update activity list view to show collaboration indicator

    - Add visual indicator for collaboration activities
    - Show collaborating club name in list item
    - _Requirements: 2.5_

- [x] 11. Testing and validation





  - [x] 11.1 Write unit tests for validation logic


    - Test ValidateCollaborationSettingsAsync with various role/type combinations
    - Test point range validation
    - Test club selection validation
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
  
  - [x] 11.2 Write integration tests for collaboration workflows


    - Test Admin creating ClubCollaboration activity
    - Test Admin creating SchoolCollaboration activity
    - Test Club Manager creating ClubCollaboration activity
    - Test Club Manager creating SchoolCollaboration activity
    - Test registration eligibility for collaboration activities
    - Test point assignment during attendance
    - _Requirements: All requirements_
  
  - [x] 11.3 Perform manual testing


    - Test all UI interactions and field visibility
    - Test club selection modal
    - Test form validation
    - Test end-to-end collaboration workflows
    - _Requirements: All requirements_
