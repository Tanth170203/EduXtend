# Member Selection Feature

## Overview
Tính năng cho phép ClubManager và Admin select members khi assign tasks trong Activity Schedule.

## Implementation

### 1. Member Selection Modal (`_MemberSelectionModal.cshtml`)

Modal component có thể được sử dụng ở nhiều nơi với 3 modes:

#### Mode 1: Load Members from Current User's Managed Club
```javascript
window.MemberSelectionModal.show(apiBaseUrl, function(selectedMember) {
    // Handle selected member
    console.log(selectedMember.studentName);
});
```

**Use Case:** ClubManager selecting members from their own club

**API Endpoint:** `GET /api/club/my-club-members`

**Modal Title:** "Select Club Member"

#### Mode 2: Load Members from Specific Club
```javascript
window.MemberSelectionModal.showForClub(apiBaseUrl, clubId, function(selectedMember) {
    // Handle selected member
    console.log(selectedMember.studentName);
});
```

**Use Case:** Admin selecting members from a specific club (e.g., in ClubCollaboration activities)

**API Endpoint:** `GET /api/club/{clubId}/members`

**Modal Title:** "Select Club Member"

#### Mode 3: Load All Active Students
```javascript
window.MemberSelectionModal.showAllStudents(apiBaseUrl, function(selectedMember) {
    // Handle selected member
    console.log(selectedMember.studentName);
});
```

**Use Case:** Admin selecting from all students (for Events, Competitions, etc.)

**API Endpoint:** `GET /api/students/active`

**Modal Title:** "Select Student"

### 2. ClubManager Implementation

**File:** `WebFE/Pages/ClubManager/Activities/Create.cshtml`

**Features:**
- Nút "Select Member" (icon users) bên cạnh "Responsible Person" input
- Click nút → mở modal với members của club mà ClubManager đang manage
- Select member → tên tự động điền vào input field

**Code:**
```javascript
selectMemberBtn.addEventListener('click', () => {
    if (window.MemberSelectionModal) {
        window.MemberSelectionModal.show('@Model.ApiBaseUrl', function(selectedMember) {
            nameInput.value = selectedMember.studentName;
            nameInput.dataset.memberId = selectedMember.id;
        });
    }
});
```

### 3. Admin Implementation

**File:** `WebFE/Pages/Admin/Activities/Create.cshtml`

**Features:**
- Nút "Select Member" (icon people) bên cạnh "Responsible Person" input
- Smart behavior based on Activity Type:
  - **ClubCollaboration:** Select from specific club members
  - **Other types:** Select from all active students

**Code:**
```javascript
selectMemberBtn.addEventListener('click', () => {
    const activityType = document.getElementById('activityTypeSelect').value;
    const clubCollaborationId = document.getElementById('ClubCollaborationIdInput').value;
    
    if (activityType === '10') {
        // ClubCollaboration type
        if (clubCollaborationId) {
            // Club selected - show members from that club
            if (window.MemberSelectionModal) {
                window.MemberSelectionModal.showForClub('@Model.ApiBaseUrl', clubCollaborationId, function(selectedMember) {
                    nameInput.value = selectedMember.studentName;
                    nameInput.dataset.memberId = selectedMember.id;
                });
            }
        } else {
            // No club selected yet
            alert('Please select a collaborating club first to choose members from that club.');
        }
    } else {
        // Other activity types - show all students
        if (window.MemberSelectionModal) {
            window.MemberSelectionModal.showAllStudents('@Model.ApiBaseUrl', function(selectedMember) {
                nameInput.value = selectedMember.studentName;
                nameInput.dataset.memberId = selectedMember.id;
            });
        }
    }
});
```

## API Endpoints

### 1. Get My Club Members (ClubManager)
```
GET /api/club/my-club-members
Authorization: Required (ClubManager role)
```

**Response:**
```json
[
  {
    "id": 1,
    "studentId": 123,
    "studentName": "Nguyen Van A",
    "studentCode": "SE123456",
    "email": "nguyenvana@example.com",
    "avatarUrl": "https://...",
    "roleInClub": "Member",
    "departmentId": 1,
    "departmentName": "Technical",
    "joinedAt": "2024-01-15T00:00:00",
    "isActive": true
  }
]
```

### 2. Get Club Members by ID (Admin)
```
GET /api/club/{clubId}/members
Authorization: Required
```

**Response:** Same as above

### 3. Get All Active Students (Admin)
```
GET /api/students/active
Authorization: Required
```

**Response:**
```json
[
  {
    "id": 123,
    "studentCode": "SE123456",
    "fullName": "Nguyen Van A",
    "email": "nguyenvana@example.com",
    "cohort": "K18"
  }
]
```

**Note:** This endpoint returns a simpler DTO (StudentDropdownDto) compared to club members. The modal automatically normalizes this to match the ClubMemberDto structure for consistent rendering.

## UI/UX

### Member Selection Modal

**Features:**
- Search by name or student code
- Display member info:
  - Avatar (or default icon)
  - Full name
  - Student code
  - Role in club (badge)
  - Department name
- Radio button selection
- Confirm button (disabled until selection)

**Search:**
- Real-time filtering
- Case-insensitive
- Searches in: name, student code

**Visual Feedback:**

*Modal:*
- Selected item: blue background (#e7f3ff)
- Hover: light gray background
- Loading state: spinner with message
- Error state: red alert with error message
- Empty state: inbox icon with "No members available"

*Assignment Input (Admin):*
- Error message: red text below input field
- Auto-scroll: smooth scroll to club selection section
- Button highlight: yellow background for 2 seconds
- Error clears: when user types manually or selects member successfully

## User Flows

### ClubManager Flow
1. ClubManager creates activity
2. Adds schedule item
3. Clicks "Add Assignment"
4. Clicks "Select Member" button (users icon)
5. Modal opens with club members
6. Searches/selects member
7. Clicks "Select"
8. Member name auto-fills in input field

### Admin Flow (ClubCollaboration)
1. Admin creates activity
2. Selects Type = "Club Collaboration"
3. Selects collaborating club
4. Adds schedule item
5. Clicks "Add Assignment"
6. Clicks "Select Member" button (people icon)
7. Modal opens with selected club's members
8. Searches/selects member
9. Clicks "Select"
10. Member name auto-fills in input field

### Admin Flow (ClubCollaboration - No Club Selected)
1. Admin creates activity
2. Selects Type = "Club Collaboration"
3. Does NOT select club yet
4. Adds schedule item
5. Clicks "Add Assignment"
6. Clicks "Select Member" button
7. **Error message appears below input:** "Please select a collaborating club first"
8. Page auto-scrolls to club selection section
9. "Select Club" button briefly highlights (yellow) for 2 seconds
10. Admin selects club
11. Can now select members

### Admin Flow (Other Activity Types)
1. Admin creates activity
2. Selects Type = "Large Event" (or any non-ClubCollaboration type)
3. Adds schedule item
4. Clicks "Add Assignment"
5. Clicks "Select Member" button
6. Modal opens with **all active students**
7. Searches/selects student
8. Clicks "Select"
9. Student name auto-fills in input field

## Benefits

### For ClubManager
- ✅ Quick member selection from their club
- ✅ No need to remember/type member names
- ✅ Ensures correct spelling
- ✅ Can see member roles and departments

### For Admin
- ✅ **ClubCollaboration:** Can assign tasks to specific club members
- ✅ **Other Activities:** Can assign tasks to any active student
- ✅ Flexible selection based on activity type
- ✅ Ensures assignments go to actual students
- ✅ Reduces errors in manual entry
- ✅ Access to all students in the system

## Future Enhancements

### Potential Features
1. **Multi-select:** Select multiple members at once
2. **Filter by role:** Show only specific roles (e.g., only Presidents)
3. **Filter by department:** Show only members from specific department
4. **Recent selections:** Show recently selected members at top
5. **Favorites:** Mark frequently assigned members
6. **Bulk assignment:** Assign same role to multiple members
7. **Member availability:** Show if member is already assigned to other tasks
8. **Contact info:** Show phone/email in modal for quick reference

## Error Handling

### ClubCollaboration Without Club Selected

**Scenario:** Admin tries to select member for ClubCollaboration activity but hasn't selected a club yet.

**Behavior:**
1. Error message displays below input: "Please select a collaborating club first"
2. Page auto-scrolls to club selection section (smooth scroll)
3. "Select Club" button briefly highlights in yellow for 2 seconds
4. Error persists until:
   - User selects a club and then selects a member, OR
   - User starts typing a name manually

**Code:**
```javascript
if (!clubCollaborationId) {
    // Show error message
    nameError.textContent = 'Please select a collaborating club first';
    
    // Scroll to club selection section
    const clubSection = document.getElementById('clubCollaborationSection');
    if (clubSection) {
        clubSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
        
        // Highlight the select club button
        const selectClubBtn = document.getElementById('selectClubBtn');
        if (selectClubBtn) {
            selectClubBtn.classList.add('btn-warning');
            setTimeout(() => {
                selectClubBtn.classList.remove('btn-warning');
            }, 2000);
        }
    }
}
```

**UX Benefits:**
- ✅ No disruptive alert popup
- ✅ Error message stays visible
- ✅ Auto-scroll guides user to the right place
- ✅ Visual highlight draws attention to action needed
- ✅ Error clears automatically when resolved

## Technical Notes

### Modal Reusability
The modal is designed to be reusable:
- Single modal component included in both ClubManager and Admin pages
- Two public methods: `show()` and `showForClub()`
- Callback pattern for handling selection
- No tight coupling to specific pages

### Data Storage
Selected member ID is stored in dataset:
```javascript
nameInput.dataset.memberId = selectedMember.id;
```

This allows backend to:
- Validate that the person exists
- Link assignment to actual user record
- Send notifications to assigned members

### Error Handling
- Network errors: Show error alert in modal
- No members: Show empty state message
- No club selected (Admin): Show alert before opening modal
- API errors: Log to console + show user-friendly message
