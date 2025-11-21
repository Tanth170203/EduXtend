# Collaboration Workflow - Updated

## New Workflow (Collaboration Accept Before Admin Approval)

### For Club Collaboration Activities:

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: Club A Creates Activity                             │
│ - Type: Club Collaboration                                  │
│ - Select Club B as partner                                  │
│ - Status: "PendingCollaboration"                           │
│ - CollaborationStatus: "Pending"                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: Club B Reviews Invitation                           │
│ - Club B sees invitation in "Invitations" page             │
│ - Can view activity details                                 │
│ - Decision: Accept or Reject                                │
└─────────────────────────────────────────────────────────────┘
                    ↙               ↘
        ┌──────────────┐    ┌──────────────────┐
        │ ACCEPT       │    │ REJECT           │
        └──────────────┘    └──────────────────┘
                ↓                       ↓
┌─────────────────────────┐  ┌──────────────────────────┐
│ Status: PendingApproval │  │ Status: CollaborationRej │
│ CollabStatus: Accepted  │  │ CollabStatus: Rejected   │
│ → Goes to Admin queue   │  │ → Does NOT go to Admin   │
└─────────────────────────┘  └──────────────────────────┘
                ↓                       ↓
┌─────────────────────────┐  ┌──────────────────────────┐
│ Step 3: Admin Reviews   │  │ Club A is notified       │
│ - Only sees accepted    │  │ - Can edit & choose      │
│   collaborations        │  │   another club           │
│ - Approve or Reject     │  │ - Or cancel activity     │
└─────────────────────────┘  └──────────────────────────┘
                ↓
┌─────────────────────────┐
│ Status: Approved        │
│ Activity is now ACTIVE  │
│ Both clubs can see it   │
└─────────────────────────┘
```

## Status Values

### Activity.Status
- **"PendingCollaboration"** - Waiting for partner club to accept
- **"PendingApproval"** - Waiting for admin approval (after partner accepted)
- **"Approved"** - Active and visible
- **"Rejected"** - Admin rejected
- **"CollaborationRejected"** - Partner club rejected (won't go to admin)

### Activity.CollaborationStatus
- **"Pending"** - Invitation sent, waiting for response
- **"Accepted"** - Partner club accepted
- **"Rejected"** - Partner club rejected

## Benefits of This Workflow

1. **Admin efficiency** - Only reviews activities with confirmed partnerships
2. **No wasted effort** - Admin doesn't review activities that might be rejected by partner
3. **Clear communication** - Both clubs agree before admin involvement
4. **Better planning** - Organizing club knows partnership status before admin review

## UI Changes Needed

### Admin Pending Activities Page
- Filter to show only activities with Status = "PendingApproval"
- Should NOT show activities with Status = "PendingCollaboration"
- Show collaboration info (partner club, status)

### Club Manager Activities Page
- Show different badges for different statuses:
  - "Pending Collaboration" (yellow) - Waiting for partner
  - "Pending Approval" (blue) - Waiting for admin
  - "Collaboration Rejected" (red) - Partner rejected
  - "Approved" (green) - Active

## Database Query Updates

### Admin Get Pending Activities
```csharp
// OLD: Get all PendingApproval
var activities = await _context.Activities
    .Where(a => a.Status == "PendingApproval")
    .ToListAsync();

// SAME: Still correct, but now only includes activities where:
// - Non-collaboration activities, OR
// - Collaboration activities that have been accepted by partner
```

### Club Manager Get My Activities
```csharp
// Show all activities created by this club, with status indicators
var activities = await _context.Activities
    .Where(a => a.ClubId == clubId)
    .Include(a => a.CollaboratingClub)
    .OrderByDescending(a => a.CreatedAt)
    .ToListAsync();
```

## Notifications (Future Enhancement)

1. **When Club A creates collaboration:**
   - Notify Club B manager: "You have a new collaboration invitation"

2. **When Club B accepts:**
   - Notify Club A manager: "Your collaboration was accepted"
   - Notify Admin: "New activity pending approval"

3. **When Club B rejects:**
   - Notify Club A manager: "Your collaboration was rejected: [reason]"

4. **When Admin approves:**
   - Notify both Club A and Club B managers: "Activity approved"

## Testing Scenarios

### Scenario 1: Happy Path
1. Club A creates collaboration with Club B
2. Club B accepts
3. Admin approves
4. Activity is active for both clubs

### Scenario 2: Partner Rejects
1. Club A creates collaboration with Club B
2. Club B rejects with reason
3. Club A sees rejection reason
4. Club A can edit and choose different partner
5. Admin never sees this activity

### Scenario 3: Admin Rejects After Accept
1. Club A creates collaboration with Club B
2. Club B accepts
3. Admin rejects (e.g., inappropriate content)
4. Both clubs are notified

## Implementation Status

- [x] Database schema supports workflow
- [x] Service layer implements new status logic
- [x] Activity creation sets "PendingCollaboration" for collaborations
- [x] Accept changes status to "PendingApproval"
- [x] Reject changes status to "CollaborationRejected"
- [ ] Admin UI filters for PendingApproval only
- [ ] Club Manager UI shows status badges
- [ ] Notifications (future)
