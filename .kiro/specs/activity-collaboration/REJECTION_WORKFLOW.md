# Collaboration Rejection & Re-invitation Workflow

## When Club B Rejects Invitation

### What Happens:
1. Activity Status changes to "CollaborationRejected"
2. CollaborationStatus = "Rejected"
3. CollaborationRejectionReason is saved
4. Activity does NOT go to Admin approval queue

### Club A (Organizer) Can:

#### Option 1: View Rejection Reason
- Go to Activities list
- See activity with "Collaboration Rejected" badge
- Click to view details
- See rejection reason from Club B

#### Option 2: Edit and Re-send
1. Click Edit on the rejected activity
2. View rejection reason
3. Make changes:
   - Change activity details (date, location, etc.)
   - OR choose a different club to collaborate with
4. Submit
5. New invitation is sent with Status = "PendingCollaboration"

#### Option 3: Delete Activity
- If collaboration is essential and no other club is suitable
- Delete the activity

## UI Changes Needed

### Activities List (Index.cshtml)
Add badge for CollaborationRejected status:

```csharp
var statusClass = activity.Status switch
{
    "Approved" => "bg-success",
    "PendingApproval" => "bg-warning text-dark",
    "PendingCollaboration" => "bg-info text-dark",
    "Rejected" => "bg-danger",
    "CollaborationRejected" => "bg-danger",
    "Completed" => "bg-secondary",
    _ => "bg-secondary"
};

var statusText = activity.Status switch
{
    "PendingCollaboration" => "Pending Collaboration",
    "CollaborationRejected" => "Collaboration Rejected",
    _ => activity.Status
};
```

Show rejection reason:
```html
@if (activity.Status == "CollaborationRejected" && !string.IsNullOrEmpty(activity.CollaborationRejectionReason))
{
    <div class="alert alert-warning mt-2">
        <strong>Rejection Reason:</strong> @activity.CollaborationRejectionReason
    </div>
}
```

### Edit Page
Show rejection info at top:

```html
@if (Model.Activity.Status == "CollaborationRejected")
{
    <div class="alert alert-danger">
        <h5>Collaboration Rejected</h5>
        <p><strong>Reason:</strong> @Model.Activity.CollaborationRejectionReason</p>
        <p>You can edit this activity and choose a different club, or make changes and re-send to the same club.</p>
    </div>
}
```

## Backend Logic

### When Editing Rejected Activity:
```csharp
// In ActivityService.ClubUpdateAsync
if (activity.Status == "CollaborationRejected" && dto.ClubCollaborationId.HasValue)
{
    // Reset collaboration status for re-invitation
    activity.CollaborationStatus = "Pending";
    activity.CollaborationRejectionReason = null;
    activity.CollaborationRespondedAt = null;
    activity.CollaborationRespondedBy = null;
    activity.Status = "PendingCollaboration";
}
```

## Example Scenario

### Scenario: Date Conflict

1. **Club A creates activity:**
   - Title: "Photography Workshop"
   - Date: Nov 25, 2025
   - Invites Club B (Photography Club)

2. **Club B rejects:**
   - Reason: "We have another workshop on the same day"

3. **Club A sees rejection:**
   - Views activity in list
   - Sees red "Collaboration Rejected" badge
   - Reads rejection reason

4. **Club A has options:**
   
   **Option A: Change date**
   - Edit activity
   - Change date to Nov 26, 2025
   - Keep Club B as partner
   - Submit → New invitation sent to Club B

   **Option B: Choose different club**
   - Edit activity
   - Remove Club B
   - Select Club C (Art Club)
   - Submit → Invitation sent to Club C

   **Option C: Cancel**
   - Delete activity
   - Plan different event

## Implementation Status

- [x] Backend logic for rejection
- [x] Rejection reason saved
- [x] Status set to CollaborationRejected
- [ ] UI shows rejection badge
- [ ] UI shows rejection reason
- [ ] Edit page shows rejection info
- [ ] Edit allows re-invitation

## Quick Implementation

### 1. Update Index.cshtml status display
Add "CollaborationRejected" case to status badge

### 2. Show rejection reason in activity card
Add conditional display for rejection reason

### 3. Edit page - show rejection alert
Add alert at top of edit form when status is CollaborationRejected

### 4. Service - reset collaboration on edit
When editing rejected activity with new club, reset collaboration fields

This provides a complete workflow for handling rejections and re-invitations!
