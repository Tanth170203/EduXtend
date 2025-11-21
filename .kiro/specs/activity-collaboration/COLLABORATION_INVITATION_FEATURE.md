# Collaboration Invitation Feature

## Overview

Khi một club tạo activity với Type = "Club Collaboration" và chọn một club khác để hợp tác, club được mời cần có khả năng:
1. Xem danh sách các lời mời hợp tác (Pending Invitations)
2. Accept hoặc Reject lời mời
3. Nếu Reject, phải nhập lý do

## Database Schema Changes

### Table: Activities

Thêm các columns mới:

```sql
ALTER TABLE Activities
ADD CollaborationStatus NVARCHAR(50) NULL,
ADD CollaborationRejectionReason NVARCHAR(500) NULL,
ADD CollaborationRespondedAt DATETIME2 NULL,
ADD CollaborationRespondedBy INT NULL;

-- Add foreign key for CollaborationRespondedBy
ALTER TABLE Activities
ADD CONSTRAINT FK_Activities_CollaborationRespondedBy
FOREIGN KEY (CollaborationRespondedBy) REFERENCES Users(Id);
```

### CollaborationStatus Values

- `NULL` hoặc không set: Activity không phải collaboration hoặc chưa gửi lời mời
- `"Pending"`: Lời mời đang chờ phản hồi từ club được mời
- `"Accepted"`: Club được mời đã chấp nhận
- `"Rejected"`: Club được mời đã từ chối

### Business Logic

**Khi tạo Club Collaboration Activity:**
1. Nếu `ClubCollaborationId` được set → `CollaborationStatus = "Pending"`
2. Activity vẫn được tạo nhưng chưa "active" cho club được mời

**Khi Club được mời Accept:**
1. `CollaborationStatus = "Accepted"`
2. `CollaborationRespondedAt = DateTime.Now`
3. `CollaborationRespondedBy = CurrentUserId`
4. Activity trở nên visible cho members của club được mời

**Khi Club được mời Reject:**
1. `CollaborationStatus = "Rejected"`
2. `CollaborationRejectionReason = [user input]`
3. `CollaborationRespondedAt = DateTime.Now`
4. `CollaborationRespondedBy = CurrentUserId`
5. Thông báo cho club tạo activity
6. Activity vẫn tồn tại nhưng không có collaboration

## UI Changes

### ClubManager/Activities/Index.cshtml

Thêm button "Collaboration Invitations" với badge số lượng:

```html
<a href="/ClubManager/Activities/CollaborationInvitations" class="btn btn-warning position-relative">
    <i class="bi bi-envelope"></i> Collaboration Invitations
    <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
        {count}
    </span>
</a>
```

### New Page: ClubManager/Activities/CollaborationInvitations.cshtml

Hiển thị danh sách các activities mà club của user được mời hợp tác:

```
┌─────────────────────────────────────────────────────────────┐
│ Collaboration Invitations                                    │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Image] Activity Title                                  │ │
│ │         Invited by: Photography Club                    │ │
│ │         Date: Nov 20, 2025 - Nov 21, 2025              │ │
│ │         Collaboration Points: 3                         │ │
│ │         [Accept] [Reject]                               │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Reject Modal

```html
<div class="modal" id="rejectModal">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5>Reject Collaboration</h5>
            </div>
            <div class="modal-body">
                <label>Reason for rejection:</label>
                <textarea class="form-control" rows="3" required></textarea>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary">Cancel</button>
                <button class="btn btn-danger">Reject</button>
            </div>
        </div>
    </div>
</div>
```

## API Endpoints

### 1. Get Collaboration Invitations

```
GET /api/activity/collaboration-invitations
Authorization: ClubManager
```

Response:
```json
[
  {
    "activityId": 123,
    "title": "Photography Workshop",
    "organizingClubId": 1,
    "organizingClubName": "Photography Club",
    "startTime": "2025-11-20T10:00:00",
    "endTime": "2025-11-20T12:00:00",
    "collaborationPoint": 3,
    "imageUrl": "https://...",
    "description": "..."
  }
]
```

### 2. Accept Collaboration

```
POST /api/activity/{activityId}/collaboration/accept
Authorization: ClubManager
```

Response:
```json
{
  "success": true,
  "message": "Collaboration accepted successfully"
}
```

### 3. Reject Collaboration

```
POST /api/activity/{activityId}/collaboration/reject
Authorization: ClubManager
Body: {
  "reason": "We have another event on the same day"
}
```

Response:
```json
{
  "success": true,
  "message": "Collaboration rejected"
}
```

## Migration Steps

### 1. Create Migration

```csharp
public partial class AddCollaborationInvitationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CollaborationStatus",
            table: "Activities",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CollaborationRejectionReason",
            table: "Activities",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CollaborationRespondedAt",
            table: "Activities",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CollaborationRespondedBy",
            table: "Activities",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Activities_CollaborationRespondedBy",
            table: "Activities",
            column: "CollaborationRespondedBy");

        migrationBuilder.AddForeignKey(
            name: "FK_Activities_Users_CollaborationRespondedBy",
            table: "Activities",
            column: "CollaborationRespondedBy",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Activities_Users_CollaborationRespondedBy",
            table: "Activities");

        migrationBuilder.DropIndex(
            name: "IX_Activities_CollaborationRespondedBy",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "CollaborationStatus",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "CollaborationRejectionReason",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "CollaborationRespondedAt",
            table: "Activities");

        migrationBuilder.DropColumn(
            name: "CollaborationRespondedBy",
            table: "Activities");
    }
}
```

### 2. Update Activity Model

```csharp
// BusinessObject/Models/Activity.cs
public class Activity
{
    // ... existing properties ...
    
    // Collaboration Invitation fields
    public string? CollaborationStatus { get; set; } // "Pending", "Accepted", "Rejected"
    public string? CollaborationRejectionReason { get; set; }
    public DateTime? CollaborationRespondedAt { get; set; }
    public int? CollaborationRespondedBy { get; set; }
    public User? CollaborationResponder { get; set; }
}
```

### 3. Update DTOs

```csharp
// BusinessObject/DTOs/Activity/ActivityDetailDto.cs
public class ActivityDetailDto
{
    // ... existing properties ...
    
    public string? CollaborationStatus { get; set; }
    public string? CollaborationRejectionReason { get; set; }
    public DateTime? CollaborationRespondedAt { get; set; }
    public string? CollaborationResponderName { get; set; }
}

// New DTO for invitation list
public class CollaborationInvitationDto
{
    public int ActivityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrganizingClubId { get; set; }
    public string OrganizingClubName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? CollaborationPoint { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
}
```

## Implementation Priority

1. **High Priority:**
   - Database migration
   - Model updates
   - Set CollaborationStatus = "Pending" when creating collaboration activity
   - API endpoint to get invitations
   - API endpoints to accept/reject

2. **Medium Priority:**
   - UI for invitation list page
   - Badge counter on Activities page
   - Reject modal with reason input

3. **Low Priority:**
   - Email notifications
   - Real-time notifications
   - Activity history/audit log

## Testing Checklist

- [ ] Create Club Collaboration activity → Status = "Pending"
- [ ] Invited club can see invitation in list
- [ ] Accept invitation → Status = "Accepted", activity visible to members
- [ ] Reject invitation → Status = "Rejected", reason saved
- [ ] Organizing club can see rejection reason
- [ ] Badge counter shows correct number
- [ ] Cannot accept/reject after already responded
- [ ] Cannot accept/reject if not the invited club

## Notes

- Collaboration invitation chỉ áp dụng cho **Club Collaboration** type
- School Collaboration không cần invitation (school admin tự approve)
- Nếu invitation bị reject, organizing club có thể edit activity và chọn club khác
- Activity vẫn có thể proceed nếu collaboration bị reject (chỉ mất phần collaboration)
