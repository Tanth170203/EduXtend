# Collaboration Invitation - Implementation Summary

## Đã hoàn thành ✓

1. **Documentation**
   - ✓ Feature specification (`COLLABORATION_INVITATION_FEATURE.md`)
   - ✓ Migration guide (`CREATE_MIGRATION_COMMAND.md`)

2. **Database Model**
   - ✓ Updated `Activity.cs` model with new properties:
     - `CollaborationStatus`
     - `CollaborationRejectionReason`
     - `CollaborationRespondedAt`
     - `CollaborationRespondedBy`

## Cần làm tiếp

### 1. Database Migration (PRIORITY: HIGH)

```bash
cd DataAccess
dotnet ef migrations add AddCollaborationInvitationFields --context EduXtendDbContext
dotnet ef database update
```

### 2. Update DbContext Configuration

File: `DataAccess/EduXtendDbContext.cs`

Thêm configuration cho foreign key:

```csharp
modelBuilder.Entity<Activity>()
    .HasOne(a => a.CollaborationResponder)
    .WithMany()
    .HasForeignKey(a => a.CollaborationRespondedBy)
    .OnDelete(DeleteBehavior.Restrict);
```

### 3. Service Layer

**IActivityService.cs** - Thêm methods:
```csharp
Task<List<CollaborationInvitationDto>> GetCollaborationInvitationsAsync(int clubId);
Task<(bool success, string message)> AcceptCollaborationAsync(int activityId, int userId);
Task<(bool success, string message)> RejectCollaborationAsync(int activityId, int userId, string reason);
```

**ActivityService.cs** - Implement methods

### 4. Repository Layer

**IActivityRepository.cs** - Thêm methods:
```csharp
Task<List<Activity>> GetPendingCollaborationInvitationsAsync(int clubId);
Task<bool> UpdateCollaborationStatusAsync(int activityId, string status, int respondedBy, string? reason = null);
```

### 5. API Controller

**ActivityController.cs** - Thêm endpoints:
```csharp
[HttpGet("collaboration-invitations")]
[Authorize(Roles = "ClubManager")]
public async Task<IActionResult> GetCollaborationInvitations()

[HttpPost("{activityId}/collaboration/accept")]
[Authorize(Roles = "ClubManager")]
public async Task<IActionResult> AcceptCollaboration(int activityId)

[HttpPost("{activityId}/collaboration/reject")]
[Authorize(Roles = "ClubManager")]
public async Task<IActionResult> RejectCollaboration(int activityId, [FromBody] RejectDto dto)
```

### 6. Frontend - ClubManager

**Pages/ClubManager/Activities/Index.cshtml**
- Thêm button "Collaboration Invitations" với badge counter

**Pages/ClubManager/Activities/CollaborationInvitations.cshtml** (NEW)
- Hiển thị danh sách invitations
- Accept/Reject buttons
- Reject modal với reason input

### 7. Update Create Activity Logic

Khi tạo activity với `ClubCollaborationId`:
```csharp
if (dto.ClubCollaborationId.HasValue)
{
    activity.CollaborationStatus = "Pending";
}
```

## Testing Checklist

- [ ] Migration chạy thành công
- [ ] Tạo Club Collaboration → Status = "Pending"
- [ ] Club được mời thấy invitation
- [ ] Accept invitation → Status = "Accepted"
- [ ] Reject invitation → Status = "Rejected", reason saved
- [ ] Badge counter hiển thị đúng số lượng
- [ ] Không thể accept/reject 2 lần

## Estimated Time

- Migration & Model: 30 minutes ✓ DONE
- Service & Repository: 2 hours
- API Endpoints: 1 hour
- Frontend UI: 3 hours
- Testing: 1 hour

**Total: ~7 hours**
