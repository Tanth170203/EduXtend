# Requirements Document

## Introduction

This feature enables collaboration functionality for Activities in the EduXtend system. It allows clubs to collaborate with each other (Club Collaboration) or with the school administration (School Collaboration). The feature includes collaboration partner selection, point allocation for organizers and collaborators, and registration rules based on collaboration type.

## Glossary

- **Activity System**: The system component that manages club activities and events
- **Club Collaboration**: A collaboration type where two clubs work together on an activity (ActivityType = 10)
- **School Collaboration**: A collaboration type where a club collaborates with school administration (ActivityType = 11)
- **BTC (Ban Tổ Chức)**: Organizing Committee - the role of the activity creator/organizer
- **Movement Point**: Points awarded to organizing committee members (1-10 points)
- **Collaboration Point**: Points awarded to collaborating club members (1-3 points)
- **Admin**: School administrator with full system access
- **Club Manager**: User with management rights for a specific club
- **Activity Creator**: The user (Admin or Club Manager) who creates an activity

## Requirements

### Requirement 1: Database Schema for Collaboration

**User Story:** As a system architect, I want to extend the Activities table with collaboration fields, so that the system can store collaboration relationships and point allocations.

#### Acceptance Criteria

1. THE Activity System SHALL add a nullable ClubCollaborationId field to the Activities table
2. THE Activity System SHALL add a nullable CollaborationPoint field to the Activities table with integer type
3. THE Activity System SHALL allow ClubCollaborationId to reference the Clubs table as a foreign key
4. THE Activity System SHALL allow CollaborationPoint values between 1 and 3 when set
5. THE Activity System SHALL allow both fields to be null when no club collaboration exists

### Requirement 2: Club Collaboration Selection UI

**User Story:** As a Club Manager, I want to select a collaborating club when creating a Club Collaboration activity, so that I can invite another club to work together.

#### Acceptance Criteria

1. WHEN ActivityType is set to ClubCollaboration (10), THE Activity System SHALL display a club selection button
2. WHEN the club selection button is clicked, THE Activity System SHALL display a list of available clubs excluding the creator's club
3. WHEN a club is selected from the list, THE Activity System SHALL set the ClubCollaborationId field to the selected club's ID
4. WHEN ActivityType is not ClubCollaboration, THE Activity System SHALL hide the club selection button
5. THE Activity System SHALL display the selected collaborating club name in the form after selection

### Requirement 3: Point Allocation for Club Collaboration by Club Manager

**User Story:** As a Club Manager creating a Club Collaboration activity, I want to set Movement Points for my organizing team and Collaboration Points for the partner club, so that participants receive appropriate recognition.

#### Acceptance Criteria

1. WHEN a Club Manager creates an activity with ActivityType ClubCollaboration, THE Activity System SHALL display a Movement Point input field with range 1-10
2. WHEN a Club Manager creates an activity with ActivityType ClubCollaboration, THE Activity System SHALL display a Collaboration Point input field with range 1-3
3. WHEN a Club Manager enters a Movement Point value outside 1-10 range, THE Activity System SHALL display a validation error
4. WHEN a Club Manager enters a Collaboration Point value outside 1-3 range, THE Activity System SHALL display a validation error
5. THE Activity System SHALL require both point fields to be filled when ClubCollaborationId is set

### Requirement 4: Point Allocation for School Collaboration by Admin

**User Story:** As an Admin creating a School Collaboration activity, I want to invite one club and set their Collaboration Points, so that the collaborating club receives appropriate recognition.

#### Acceptance Criteria

1. WHEN an Admin creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL display a club selection button
2. WHEN an Admin creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL display a Collaboration Point input field with range 1-3
3. WHEN an Admin creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL hide the Movement Point input field
4. WHEN an Admin creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL set ClubCollaborationId to the selected club
5. WHEN an Admin creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL leave Movement Point as null or default

### Requirement 5: Point Allocation for School Collaboration by Club Manager

**User Story:** As a Club Manager creating a School Collaboration activity, I want to set Movement Points for my organizing team without selecting a collaborating club, so that my team receives recognition for working with the school.

#### Acceptance Criteria

1. WHEN a Club Manager creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL display a Movement Point input field with range 1-10
2. WHEN a Club Manager creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL hide the club selection button
3. WHEN a Club Manager creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL hide the Collaboration Point input field
4. WHEN a Club Manager creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL set ClubCollaborationId to null
5. WHEN a Club Manager creates an activity with ActivityType SchoolCollaboration, THE Activity System SHALL set CollaborationPoint to null

### Requirement 6: Registration Rules for Club Collaboration

**User Story:** As a student, I want to register for Club Collaboration activities based on my club membership, so that I can participate in collaborative events.

#### Acceptance Criteria

1. WHEN an activity has ActivityType ClubCollaboration AND IsPublic is false, THE Activity System SHALL allow registration only from members of the organizing club or collaborating club
2. WHEN an activity has ActivityType ClubCollaboration AND IsPublic is true, THE Activity System SHALL allow registration from any student
3. WHEN a student from the organizing club registers, THE Activity System SHALL assign Movement Point to their participation record
4. WHEN a student from the collaborating club registers, THE Activity System SHALL assign Collaboration Point to their participation record
5. WHEN a student attempts to register for a non-public Club Collaboration activity without membership in either club, THE Activity System SHALL reject the registration with an error message

### Requirement 7: Edit Activity Collaboration Settings

**User Story:** As an Admin or Club Manager, I want to edit collaboration settings for an existing activity, so that I can update collaboration partners or point allocations.

#### Acceptance Criteria

1. WHEN editing an activity, THE Activity System SHALL display current collaboration settings based on ActivityType
2. WHEN ActivityType is changed from non-collaboration to collaboration type, THE Activity System SHALL display appropriate collaboration fields
3. WHEN ActivityType is changed from collaboration to non-collaboration type, THE Activity System SHALL clear ClubCollaborationId and CollaborationPoint fields
4. WHEN collaboration settings are updated, THE Activity System SHALL validate all point ranges according to the role and ActivityType
5. THE Activity System SHALL preserve existing participant point allocations when collaboration settings are modified

### Requirement 8: Validation and Business Rules

**User Story:** As a system administrator, I want the system to enforce collaboration business rules, so that data integrity is maintained.

#### Acceptance Criteria

1. WHEN ClubCollaborationId is set, THE Activity System SHALL verify the referenced club exists in the database
2. WHEN ActivityType is ClubCollaboration, THE Activity System SHALL prevent selecting the same club as both organizer and collaborator
3. WHEN CollaborationPoint is set, THE Activity System SHALL require ClubCollaborationId to be set for Club Collaboration activities
4. WHEN an activity is saved with invalid collaboration data, THE Activity System SHALL return specific validation error messages
5. THE Activity System SHALL enforce that Movement Point and Collaboration Point are positive integers within their respective ranges
