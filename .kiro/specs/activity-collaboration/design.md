# Design Document

## Overview

The Activity Collaboration feature extends the existing Activity system to support two types of collaboration:
1. **Club Collaboration** - Two clubs working together on an activity
2. **School Collaboration** - A club collaborating with school administration

The feature introduces collaboration partner selection, differentiated point allocation (Movement Points for organizers, Collaboration Points for partners), and modified registration rules based on collaboration type and visibility settings.

## Architecture

### System Components

The feature integrates into the existing layered architecture:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  - WebFE/Pages/Admin/Activities/Create.cshtml           │
│  - WebFE/Pages/Admin/Activities/Edit.cshtml             │
│  - WebFE/Pages/ClubManager/Activities/Create.cshtml     │
│  - WebFE/Pages/ClubManager/Activities/Edit.cshtml       │
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│                      API Layer                           │
│  - WebAPI/Controllers/ActivitiesController.cs           │
│  - WebAPI/Admin/AdminActivitiesController.cs            │
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│                    Business Layer                        │
│  - Services/Activities/ActivityService.cs               │
│  - Services/Activities/IActivityService.cs              │
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│                     Data Layer                           │
│  - Repositories/Activities/ActivityRepository.cs        │
│  - DataAccess/EduXtendDbContext.cs                      │
│  - BusinessObject/Models/Activity.cs                    │
└─────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Database Schema Changes

**Activity Model Extension:**

```csharp
// BusinessObject/Models/Activity.cs - New properties
public int? ClubCollaborationId { get; set; }
public Club? CollaboratingClub { get; set; }
public int? CollaborationPoint { get; set; }
```

**Database Migration:**
- Add nullable `ClubCollaborationId` column (INT, FK to Clubs table)
- Add nullable `CollaborationPoint` column (INT)
- Add foreign key constraint with ON DELETE SET NULL

### 2. DTO Extensions

**AdminCreateActivityDto:**
```csharp
public int? ClubCollaborationId { get; set; }
public int? CollaborationPoint { get; set; }
```

**AdminUpdateActivityDto:**
```csharp
public int? ClubCollaborationId { get; set; }
public int? CollaborationPoint { get; set; }
```

**ClubCreateActivityDto:**
```csharp
public int? ClubCollaborationId { get; set; }
public int? CollaborationPoint { get; set; }
```

**ActivityDetailDto:**
```csharp
public int? ClubCollaborationId { get; set; }
public string? CollaboratingClubName { get; set; }
public int? CollaborationPoint { get; set; }
```

**ActivityListItemDto:**
```csharp
public int? ClubCollaborationId { get; set; }
public string? CollaboratingClubName { get; set; }
```

### 3. Service Layer Changes

**IActivityService Interface Extensions:**
```csharp
Task<List<ClubListDto>> GetAvailableCollaboratingClubsAsync(int excludeClubId);
Task<bool> ValidateCollaborationSettingsAsync(ActivityType type, string userRole, 
    int? clubCollaborationId, int? collaborationPoint, double movementPoint);
```

**ActivityService Business Logic:**


**Validation Rules by Role and Type:**

| User Role     | Activity Type        | ClubCollaborationId | CollaborationPoint | MovementPoint |
|---------------|---------------------|---------------------|-------------------|---------------|
| Admin         | ClubCollaboration   | Required            | Required (1-3)    | N/A           |
| Admin         | SchoolCollaboration | Required            | Required (1-3)    | N/A           |
| Club Manager  | ClubCollaboration   | Required            | Required (1-3)    | Required (1-10)|
| Club Manager  | SchoolCollaboration | Null                | Null              | Required (1-10)|
| Any           | Other Types         | Null                | Null              | As configured |

**Registration Eligibility Logic:**

```csharp
public async Task<bool> CanUserRegisterAsync(int userId, int activityId)
{
    var activity = await _repo.GetByIdAsync(activityId);
    
    // Public activities - anyone can register
    if (activity.IsPublic) return true;
    
    // Non-public activities
    if (activity.Type == ActivityType.ClubCollaboration && activity.ClubCollaborationId.HasValue)
    {
        // Check if user is member of organizing club OR collaborating club
        var isOrganizerMember = activity.ClubId.HasValue && 
            await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
        var isCollaboratorMember = 
            await _repo.IsUserMemberOfClubAsync(userId, activity.ClubCollaborationId.Value);
        
        return isOrganizerMember || isCollaboratorMember;
    }
    
    // Default club-only check
    if (activity.ClubId.HasValue)
    {
        return await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value);
    }
    
    return false;
}
```

**Point Assignment Logic:**

```csharp
public async Task<int> GetParticipantPointsAsync(int userId, int activityId)
{
    var activity = await _repo.GetByIdAsync(activityId);
    
    // For collaboration activities
    if (activity.Type == ActivityType.ClubCollaboration && activity.ClubCollaborationId.HasValue)
    {
        // Check if user is from organizing club
        if (activity.ClubId.HasValue && 
            await _repo.IsUserMemberOfClubAsync(userId, activity.ClubId.Value))
        {
            return (int)activity.MovementPoint; // BTC gets Movement Points
        }
        
        // Check if user is from collaborating club
        if (await _repo.IsUserMemberOfClubAsync(userId, activity.ClubCollaborationId.Value))
        {
            return activity.CollaborationPoint ?? 0; // Collaborator gets Collaboration Points
        }
    }
    
    // Default point assignment
    return (int)activity.MovementPoint;
}
```

### 4. Repository Layer Changes

**IActivityRepository Interface Extensions:**
```csharp
Task<List<Club>> GetAvailableCollaboratingClubsAsync(int excludeClubId);
Task<bool> IsUserMemberOfClubAsync(int userId, int clubId);
Task<Club?> GetClubByIdAsync(int clubId);
```

### 5. UI Components

**Club Selection Modal (Shared Component):**

- Modal dialog with searchable club list
- Display club logo, name, and member count
- Single selection mode
- Exclude current user's club from list (for Club Manager)
- Return selected club ID to parent form

**Form Field Visibility Logic:**

```javascript
// Pseudo-code for UI behavior
function updateCollaborationFields(activityType, userRole) {
    const isClubCollaboration = activityType === 'ClubCollaboration';
    const isSchoolCollaboration = activityType === 'SchoolCollaboration';
    const isAdmin = userRole === 'Admin';
    const isClubManager = userRole === 'ClubManager';
    
    // Club selection button
    if (isClubCollaboration || (isSchoolCollaboration && isAdmin)) {
        showClubSelectionButton();
    } else {
        hideClubSelectionButton();
        clearClubCollaborationId();
    }
    
    // Collaboration Point field
    if ((isClubCollaboration && isClubManager) || 
        (isSchoolCollaboration && isAdmin)) {
        showCollaborationPointField();
        setCollaborationPointRange(1, 3);
    } else {
        hideCollaborationPointField();
        clearCollaborationPoint();
    }
    
    // Movement Point field
    if (isClubManager && (isClubCollaboration || isSchoolCollaboration)) {
        showMovementPointField();
        setMovementPointRange(1, 10);
    } else if (isAdmin && (isClubCollaboration || isSchoolCollaboration)) {
        hideMovementPointField();
        clearMovementPoint();
    }
}
```

## Data Models

### Activity Model (Extended)

```csharp
public class Activity
{
    // ... existing properties ...
    
    // Collaboration properties
    public int? ClubCollaborationId { get; set; }
    public Club? CollaboratingClub { get; set; }
    public int? CollaborationPoint { get; set; }
    
    // Navigation properties remain unchanged
}
```

### Club Model (Reference)

```csharp
public class Club
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? LogoUrl { get; set; }
    // ... other properties ...
}
```

### ActivityRegistration Model (No changes needed)

The existing model already supports tracking registrations. Point assignment will be determined at attendance/scoring time based on user's club membership.

## Error Handling

### Validation Errors

**Client-Side Validation:**
- Activity Type selection triggers field visibility changes
- Point range validation (1-3 for Collaboration, 1-10 for Movement)
- Required field validation based on activity type and role

**Server-Side Validation:**

```csharp
public class CollaborationValidationException : Exception
{
    public string ErrorCode { get; set; }
    public CollaborationValidationException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

// Error codes:
// - INVALID_CLUB_COLLABORATION_ID: Selected club doesn't exist
// - SAME_CLUB_COLLABORATION: Cannot collaborate with own club
// - INVALID_COLLABORATION_POINT: Point out of range (1-3)
// - INVALID_MOVEMENT_POINT: Point out of range (1-10)
// - MISSING_REQUIRED_FIELD: Required collaboration field is null
// - UNAUTHORIZED_COLLABORATION: User doesn't have permission
```

### Business Logic Errors


**Registration Errors:**
- User not member of either organizing or collaborating club (non-public activity)
- Activity full
- Activity not approved
- Activity ended

**Error Response Format:**
```json
{
    "success": false,
    "message": "User-friendly error message",
    "errorCode": "ERROR_CODE",
    "details": {
        "field": "fieldName",
        "value": "invalidValue"
    }
}
```

## Testing Strategy

### Unit Tests

**Service Layer Tests:**

1. **Validation Tests:**
   - Test collaboration settings validation for each role/type combination
   - Test point range validation (1-3, 1-10)
   - Test null value handling

2. **Registration Eligibility Tests:**
   - Test public activity registration (any user)
   - Test non-public Club Collaboration (organizer member)
   - Test non-public Club Collaboration (collaborator member)
   - Test non-public Club Collaboration (non-member rejection)
   - Test School Collaboration registration rules

3. **Point Assignment Tests:**
   - Test Movement Point assignment for organizing club members
   - Test Collaboration Point assignment for collaborating club members
   - Test default point assignment for non-collaboration activities

**Repository Layer Tests:**

1. **Data Access Tests:**
   - Test GetAvailableCollaboratingClubsAsync (excludes specified club)
   - Test IsUserMemberOfClubAsync (various membership scenarios)
   - Test club lookup by ID

### Integration Tests

1. **Admin Create Club Collaboration:**
   - Create activity with Type=ClubCollaboration
   - Set ClubCollaborationId and CollaborationPoint
   - Verify MovementPoint is null/default
   - Verify activity saved correctly

2. **Admin Create School Collaboration:**
   - Create activity with Type=SchoolCollaboration
   - Set ClubCollaborationId and CollaborationPoint
   - Verify activity saved correctly

3. **Club Manager Create Club Collaboration:**
   - Create activity with Type=ClubCollaboration
   - Set ClubCollaborationId, CollaborationPoint, and MovementPoint
   - Verify all fields saved correctly
   - Verify cannot select own club as collaborator

4. **Club Manager Create School Collaboration:**
   - Create activity with Type=SchoolCollaboration
   - Set MovementPoint only
   - Verify ClubCollaborationId and CollaborationPoint are null

5. **Registration Flow:**
   - Register organizing club member (should succeed)
   - Register collaborating club member (should succeed)
   - Register non-member for non-public activity (should fail)
   - Register any user for public activity (should succeed)

6. **Edit Activity:**
   - Change activity type from non-collaboration to collaboration
   - Verify collaboration fields appear and validate
   - Change from collaboration to non-collaboration
   - Verify collaboration fields cleared

### UI Tests

1. **Field Visibility Tests:**
   - Select ClubCollaboration type → verify club selection button appears
   - Select SchoolCollaboration as Admin → verify club selection and collaboration point appear
   - Select SchoolCollaboration as Club Manager → verify only movement point appears
   - Change activity type → verify fields update correctly

2. **Club Selection Modal:**
   - Open modal → verify clubs listed (excluding own club for managers)
   - Search clubs → verify filtering works
   - Select club → verify selection reflected in form

3. **Validation Tests:**
   - Enter collaboration point < 1 or > 3 → verify error message
   - Enter movement point < 1 or > 10 → verify error message
   - Submit without required collaboration fields → verify error message

## Migration Strategy

### Database Migration

```csharp
public partial class AddActivityCollaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ClubCollaborationId",
            table: "Activities",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CollaborationPoint",
            table: "Activities",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Activities_ClubCollaborationId",
            table: "Activities",
            column: "ClubCollaborationId");

        migrationBuilder.AddForeignKey(
            name: "FK_Activities_Clubs_ClubCollaborationId",
            table: "Activities",
            column: "ClubCollaborationId",
            principalTable: "Clubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Activities_Clubs_ClubCollaborationId",
            table: "Activities");

        migrationBuilder.DropIndex(
            name: "IX_Activities_ClubCollaborationId",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "ClubCollaborationId",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "CollaborationPoint",
            table: "Activities");
    }
}
```

### Backward Compatibility

- Existing activities will have null values for new fields (no impact)
- Existing registration logic continues to work for non-collaboration activities
- New validation only applies when ActivityType is ClubCollaboration or SchoolCollaboration

### Deployment Steps

1. Deploy database migration (add columns)
2. Deploy backend code (service, repository, DTOs)
3. Deploy API controllers
4. Deploy frontend UI changes
5. Test collaboration features in staging
6. Deploy to production

## Performance Considerations

### Database Queries

- Add index on `ClubCollaborationId` for efficient lookups
- Use eager loading for `CollaboratingClub` navigation property when needed
- Cache club list for selection modal (refresh periodically)

### Caching Strategy

```csharp
// Cache available clubs for 5 minutes
private async Task<List<Club>> GetAvailableClubsCachedAsync(int excludeClubId)
{
    var cacheKey = $"available_clubs_{excludeClubId}";
    if (_cache.TryGetValue(cacheKey, out List<Club> clubs))
    {
        return clubs;
    }
    
    clubs = await _repo.GetAvailableCollaboratingClubsAsync(excludeClubId);
    _cache.Set(cacheKey, clubs, TimeSpan.FromMinutes(5));
    return clubs;
}
```

## API Endpoints

### Get Available Collaborating Clubs

**Endpoint:** `GET /api/activity/available-clubs`

**Query Parameters:**
- `excludeClubId` (optional): Club ID to exclude from results (typically the current user's club)

**Response:**
```json
[
  {
    "id": 1,
    "name": "Photography Club",
    "logoUrl": "https://...",
    "memberCount": 45
  },
  {
    "id": 2,
    "name": "Music Club",
    "logoUrl": "https://...",
    "memberCount": 30
  }
]
```

**Authorization:** Requires authenticated user (Admin or Club Manager)

**Implementation Notes:**
- For Club Managers: Automatically excludes their own managed club
- Returns all active clubs except the excluded one
- Includes basic club information needed for selection UI

## Security Considerations

### Authorization

- Verify user role before allowing collaboration settings
- Validate club ownership for Club Manager operations
- Prevent Club Manager from selecting their own club as collaborator
- Verify club membership for non-public activity registration

### Input Validation

- Sanitize all user inputs
- Validate point ranges server-side (don't trust client validation)
- Verify ClubCollaborationId references valid club
- Check for SQL injection in club search queries

### Data Integrity

- Use foreign key constraints to maintain referential integrity
- Set ON DELETE SET NULL for ClubCollaborationId (preserve activity if club deleted)
- Validate collaboration settings before saving
- Use transactions for multi-step operations

## UI/UX Design

### Create/Edit Activity Form Layout

```
┌─────────────────────────────────────────────────────────┐
│ Activity Type: [Dropdown ▼]                             │
├─────────────────────────────────────────────────────────┤
│ [If ClubCollaboration or SchoolCollaboration selected]  │
│                                                          │
│ Collaborating Club:                                     │
│ ┌─────────────────────────────────┐                    │
│ │ [Selected Club Name]            │ [Select Club]      │
│ └─────────────────────────────────┘                    │
│                                                          │
│ [If showing Collaboration Point]                        │
│ Collaboration Point (1-3): [___]                        │
│                                                          │
│ [If showing Movement Point]                             │
│ Movement Point (1-10): [___]                            │
└─────────────────────────────────────────────────────────┘
```

### Club Selection Modal

```
┌─────────────────────────────────────────────────────────┐
│ Select Collaborating Club                          [X]   │
├─────────────────────────────────────────────────────────┤
│ Search: [_____________________] 🔍                      │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐ │
│ │ [Logo] Club Name 1              (50 members)    ( ) │ │
│ │ [Logo] Club Name 2              (30 members)    ( ) │ │
│ │ [Logo] Club Name 3              (45 members)    ( ) │ │
│ └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│                              [Cancel]  [Select]          │
└─────────────────────────────────────────────────────────┘
```

## Future Enhancements

1. **Multiple Collaborators:** Support more than one collaborating club
2. **Collaboration Invitations:** Send invitations to clubs before finalizing collaboration
3. **Collaboration History:** Track and display past collaborations between clubs
4. **Collaboration Analytics:** Report on collaboration frequency and success metrics
5. **Flexible Point Allocation:** Allow different points for different roles within collaboration
