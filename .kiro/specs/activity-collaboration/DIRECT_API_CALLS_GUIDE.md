# Guide: Update to Direct WebAPI Calls

## Changes Needed

### 1. CollaborationInvitations.cshtml

Replace all proxy calls with direct API calls:

#### Load Invitations:
```javascript
// OLD (with proxy):
const url = `/Api/GetCollaborationInvitations?clubId=${clubId}`;
const response = await fetch(url);

// NEW (direct API):
const url = `@Model.ApiBaseUrl/api/activity/collaboration-invitations/list?clubId=${clubId}`;
const response = await fetch(url, {
    credentials: 'include'
});
```

#### Accept Invitation:
```javascript
// OLD:
const response = await fetch(`/Api/AcceptCollaboration?activityId=${activityId}&clubId=${clubId}`, {
    method: 'POST'
});

// NEW:
const response = await fetch(`@Model.ApiBaseUrl/api/activity/${activityId}/collaboration/accept?clubId=${clubId}`, {
    method: 'POST',
    credentials: 'include'
});
```

#### Reject Invitation:
```javascript
// OLD:
const response = await fetch(`/Api/RejectCollaboration?activityId=${currentActivityId}&clubId=${clubId}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason: reason })
});

// NEW:
const response = await fetch(`@Model.ApiBaseUrl/api/activity/${currentActivityId}/collaboration/reject?clubId=${clubId}`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason: reason })
});
```

### 2. Index.cshtml (Badge Counter)

```javascript
// OLD:
const countRes = await fetch(`/Api/GetCollaborationInvitationCount?clubId=${clubId}`);

// NEW:
const countRes = await fetch(`@Model.ApiBaseUrl/api/activity/collaboration-invitations/count?clubId=${clubId}`, {
    credentials: 'include'
});
```

### 3. _ClubSelectionModal.cshtml

```javascript
// OLD:
const url = `/Api/GetAvailableClubs?excludeClubId=${excludeClubId || 0}`;
const response = await fetch(url);

// NEW:
const url = `${apiBaseUrl}/api/activity/available-clubs?excludeClubId=${excludeClubId || 0}`;
const response = await fetch(url, {
    credentials: 'include'
});
```

## Key Points:

1. **Always add `credentials: 'include'`** - This ensures cookies are sent
2. **Use `@Model.ApiBaseUrl`** - Gets API URL from configuration
3. **Remove all fallback logic** - No need for proxy fallback anymore
4. **Simpler code** - Direct calls are cleaner

## Testing:

After making changes:
1. Restart WebFE
2. Clear browser cache
3. Test all features:
   - Select club modal
   - View invitations
   - Accept invitation
   - Reject invitation
   - Badge counter

All should work directly with WebAPI!
