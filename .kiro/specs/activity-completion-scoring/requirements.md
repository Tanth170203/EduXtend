# Requirements Document

## Introduction

This feature enables Admin and ClubManager users to mark activities as "Completed" after the activity's end time has passed. Upon completion, the system automatically awards movement points to the organizing club and any collaborating clubs based on the activity type and predefined scoring criteria. Points are recorded in ClubMovementRecords and ClubMovementRecordDetails tables, with automatic calculation and enforcement of weekly and semester limits.

## Glossary

- **Activity**: An event, competition, meeting, or collaboration organized by a club or admin
- **ActivityType**: The classification of an activity (ClubMeeting, LargeEvent, SchoolCompetition, etc.)
- **ClubMovementRecord**: A record tracking a club's movement scores for a specific month and semester
- **ClubMovementRecordDetail**: Individual scoring entries that make up a ClubMovementRecord
- **MovementCriterion**: Scoring rules that define how many points each activity type earns
- **Organizing Club**: The club that created and hosts the activity (referenced by Activity.ClubId)
- **Collaborating Club**: A partner club invited to collaborate on an activity (referenced by Activity.ClubCollaborationId)
- **MovementPoint**: Points awarded to the organizing club based on activity type
- **CollaborationPoint**: Points awarded to the collaborating club for participating
- **Complete Button**: A UI button that appears in the Quick Actions section after EndTime has passed
- **Score Categories**: ClubMeetingScore (Club Activities), EventScore (Events), CompetitionScore (Competitions), CollaborationScore (Collaboration)
- **Weekly Limit**: Club Activities can only earn 5 points per week maximum
- **Semester Limit**: Total movement score per semester is capped at 100 points

## Requirements

### Requirement 1

**User Story:** As an Admin or ClubManager, I want to see a "Complete Activity" button in the Quick Actions section after an activity's end time has passed, so that I can mark the activity as completed and trigger automatic point calculation.

#### Acceptance Criteria

1. WHEN the current time is after the Activity EndTime AND the Activity Status is "Approved", THE System SHALL display a "Complete Activity" button in the Quick Actions section
2. WHILE the current time is before or equal to the Activity EndTime, THE System SHALL hide the "Complete Activity" button
3. WHEN the Activity Status is "Completed", THE System SHALL hide the "Complete Activity" button
4. WHERE the user role is Admin, THE System SHALL display the "Complete Activity" button on the Admin Activities Details page
5. WHERE the user role is ClubManager AND the user's club matches the Activity ClubId, THE System SHALL display the "Complete Activity" button on the ClubManager Activities Details page

### Requirement 2

**User Story:** As an Admin or ClubManager, I want to click the "Complete Activity" button to mark an activity as completed, so that the system automatically calculates and awards movement points to the relevant clubs.

#### Acceptance Criteria

1. WHEN the user clicks the "Complete Activity" button, THE System SHALL send a POST request to the API endpoint to complete the activity
2. WHEN the API receives the complete activity request, THE System SHALL validate that the Activity Status is "Approved"
3. WHEN the API receives the complete activity request, THE System SHALL validate that the current time is after the Activity EndTime
4. IF the validation fails, THEN THE System SHALL return an error message and SHALL NOT change the Activity Status
5. WHEN the validation succeeds, THE System SHALL update the Activity Status to "Completed"

### Requirement 3

**User Story:** As the system, I want to automatically calculate and award movement points to the organizing club when an activity is completed, so that clubs receive proper recognition for their activities based on the activity type.

#### Acceptance Criteria

1. WHEN an Activity is marked as "Completed" AND the Activity has a ClubId, THE System SHALL retrieve the MovementCriterion record matching the Activity Type
2. WHEN the Activity Type is ClubMeeting, ClubTraining, or ClubWorkshop, THE System SHALL add the MovementPoint to the ClubMeetingScore category
3. WHEN the Activity Type is LargeEvent, MediumEvent, or SmallEvent, THE System SHALL add the MovementPoint to the EventScore category
4. WHEN the Activity Type is SchoolCompetition, ProvincialCompetition, or NationalCompetition, THE System SHALL add the MovementPoint to the CompetitionScore category
5. WHEN the Activity Type is ClubCollaboration or SchoolCollaboration, THE System SHALL add the MovementPoint to the CollaborationScore category

### Requirement 4

**User Story:** As the system, I want to enforce the weekly 5-point limit for Club Activities, so that clubs cannot earn more than 5 points per week from club meetings, trainings, and workshops.

#### Acceptance Criteria

1. WHEN calculating points for an Activity with Type ClubMeeting, ClubTraining, or ClubWorkshop, THE System SHALL retrieve all ClubMovementRecordDetails for the same club in the current week
2. WHEN the total ClubMeetingScore for the current week is less than 5 points, THE System SHALL award the full MovementPoint amount up to the 5-point weekly limit
3. WHEN the total ClubMeetingScore for the current week equals or exceeds 5 points, THE System SHALL NOT award any additional points for the activity
4. WHEN points are not awarded due to the weekly limit, THE System SHALL create a ClubMovementRecordDetail record with Score set to 0 and a Note explaining the weekly limit was reached
5. WHEN calculating the week, THE System SHALL use the Activity EndTime to determine which week the activity belongs to

### Requirement 5

**User Story:** As the system, I want to automatically award collaboration points to the collaborating club when a collaboration activity is completed, so that partner clubs receive recognition for their participation.

#### Acceptance Criteria

1. WHEN an Activity is marked as "Completed" AND the Activity has a ClubCollaborationId AND CollaborationPoint is not null, THE System SHALL award CollaborationPoint to the collaborating club
2. WHEN awarding collaboration points, THE System SHALL add the CollaborationPoint to the CollaborationScore category of the collaborating club's ClubMovementRecord
3. WHEN awarding collaboration points, THE System SHALL create a ClubMovementRecordDetail record for the collaborating club with ScoreType set to "Auto"
4. WHEN creating the ClubMovementRecordDetail for collaboration, THE System SHALL set the ActivityId to link the detail to the completed activity
5. WHEN creating the ClubMovementRecordDetail for collaboration, THE System SHALL set the Note to describe the collaboration with the organizing club

### Requirement 6

**User Story:** As the system, I want to create or update ClubMovementRecord entries for the appropriate month and semester, so that club scores are properly tracked over time.

#### Acceptance Criteria

1. WHEN awarding points for a completed activity, THE System SHALL determine the month and semester based on the Activity EndTime
2. WHEN a ClubMovementRecord exists for the club, semester, and month, THE System SHALL update the existing record by adding the new points to the appropriate score category
3. WHEN no ClubMovementRecord exists for the club, semester, and month, THE System SHALL create a new ClubMovementRecord with the awarded points
4. WHEN updating or creating a ClubMovementRecord, THE System SHALL recalculate the TotalScore by summing ClubMeetingScore, EventScore, CompetitionScore, PlanScore, and CollaborationScore
5. WHEN the calculated TotalScore exceeds 100 points, THE System SHALL set the TotalScore to 100

### Requirement 7

**User Story:** As the system, I want to create ClubMovementRecordDetail entries for each point award, so that there is a detailed audit trail of how clubs earned their movement points.

#### Acceptance Criteria

1. WHEN awarding points for a completed activity, THE System SHALL create a ClubMovementRecordDetail record linked to the ClubMovementRecord
2. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the CriterionId to the matching MovementCriterion for the activity type
3. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the ActivityId to link the detail to the completed activity
4. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the Score to the actual points awarded (which may be 0 if limits are reached)
5. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the ScoreType to "Auto" to indicate automatic scoring
6. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the AwardedAt timestamp to the current date and time
7. WHEN creating a ClubMovementRecordDetail, THE System SHALL set the Note to describe the activity and any relevant information (e.g., weekly limit reached, collaboration details)

### Requirement 8

**User Story:** As an Admin or ClubManager, I want to see a success message after completing an activity, so that I know the operation was successful and points were awarded correctly.

#### Acceptance Criteria

1. WHEN the activity completion is successful, THE System SHALL return a success response with details of points awarded
2. WHEN the activity completion is successful, THE System SHALL display a success toast notification to the user
3. WHEN the activity completion is successful, THE System SHALL refresh the activity details page to show the updated "Completed" status
4. IF the activity completion fails, THEN THE System SHALL display an error message explaining why the operation failed
5. WHEN displaying the success message, THE System SHALL include information about points awarded to the organizing club and collaborating club (if applicable)

### Requirement 9

**User Story:** As a developer, I want the system to handle edge cases and errors gracefully, so that the application remains stable and provides clear feedback to users.

#### Acceptance Criteria

1. WHEN an Activity does not have a ClubId, THE System SHALL complete the activity without awarding any movement points
2. WHEN a MovementCriterion cannot be found for an Activity Type, THE System SHALL log an error and complete the activity without awarding points
3. WHEN a database error occurs during point calculation, THE System SHALL rollback the transaction and return an error to the user
4. WHEN multiple users attempt to complete the same activity simultaneously, THE System SHALL ensure only one completion is processed successfully
5. IF an Activity is already "Completed", THEN THE System SHALL return an error message indicating the activity is already completed

### Requirement 10

**User Story:** As an Admin or ClubManager, I want the Complete Activity button to be visually distinct and clearly indicate its purpose, so that I can easily identify when an activity is ready to be completed.

#### Acceptance Criteria

1. WHEN the Complete Activity button is displayed, THE System SHALL use a success color scheme (green) to indicate a positive action
2. WHEN the Complete Activity button is displayed, THE System SHALL include an icon that represents completion or checkmark
3. WHEN the Complete Activity button is displayed, THE System SHALL include descriptive text "Complete Activity" or similar
4. WHEN the user hovers over the Complete Activity button, THE System SHALL display a tooltip explaining that this will mark the activity as completed and award points
5. WHEN the Complete Activity button is clicked, THE System SHALL disable the button and show a loading indicator until the operation completes
