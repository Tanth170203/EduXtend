# Manual Testing Checklist for Activity Collaboration Feature

This document provides a comprehensive checklist for manually testing the Activity Collaboration feature. Follow these test cases to ensure all functionality works as expected.

## Prerequisites

Before starting manual testing:
- [ ] Database is migrated with collaboration fields (ClubCollaborationId, CollaborationPoint)
- [ ] Application is running (both WebAPI and WebFE)
- [ ] Test data exists:
  - [ ] At least 3 clubs in the system
  - [ ] Admin user account
  - [ ] Club Manager accounts for at least 2 different clubs
  - [ ] Student accounts that are members of different clubs

## Test Environment Setup

1. **Create Test Clubs** (if not already exist):
   - Club A: "Tech Club" (you will be manager)
   - Club B: "English Club" (for collaboration)
   - Club C: "Art Club" (for collaboration)

2. **Create Test Users**:
   - Admin user
   - Manager for Club A
   - Manager for Club B
   - Student 1 (member of Club A)
   - Student 2 (member of Club B)
   - Student 3 (not member of any club)

---

## Test Suite 1: Admin - Club Collaboration Creation

### Test Case 1.1: Create Club Collaboration Activity
**User Role**: Admin

**Steps**:
1. [ ] Login as Admin
2. [ ] Navigate to Admin > Activities > Create
3. [ ] Fill in basic activity details:
   - Title: "Admin Club Collaboration Test"
   - Description: "Testing collaboration feature"
   - Location: "Main Hall"
   - Start Time: Tomorrow at 10:00 AM
   - End Time: Tomorrow at 5:00 PM
4. [ ] Select Activity Type: "ClubCollaboration"
5. [ ] **Verify**: Club selection button appears
6. [ ] **Verify**: Collaboration Point field appears (range 1-3)
7. [ ] **Verify**: Movement Point field is hidden or disabled
8. [ ] Click "Select Collaborating Club" button
9. [ ] **Verify**: Modal opens with list of clubs
10. [ ] Select "Tech Club" from the list
11. [ ] **Verify**: Selected club name appears in the form
12. [ ] Enter Collaboration Point: 2
13. [ ] Set IsPublic: true
14. [ ] Click "Create Activity"
15. [ ] **Verify**: Activity is created successfully
16. [ ] **Verify**: Activity details show:
    - Collaborating Club: "Tech Club"
    - Collaboration Point: 2
    - Status: "Approved"

**Expected Result**: ✅ Activity created with collaboration settings

---

### Test Case 1.2: Create Club Collaboration - Validation Tests
**User Role**: Admin

**Steps**:
1. [ ] Navigate to Admin > Activities > Create
2. [ ] Select Activity Type: "ClubCollaboration"
3. [ ] Try to submit without selecting a club
4. [ ] **Verify**: Error message "Collaborating club must be selected"
5. [ ] Select a club
6. [ ] Enter Collaboration Point: 0
7. [ ] **Verify**: Validation error "Must be between 1 and 3"
8. [ ] Enter Collaboration Point: 4
9. [ ] **Verify**: Validation error "Must be between 1 and 3"
10. [ ] Enter Collaboration Point: 2
11. [ ] **Verify**: Form validates successfully

**Expected Result**: ✅ Validation works correctly

---

## Test Suite 2: Admin - School Collaboration Creation

### Test Case 2.1: Create School Collaboration Activity
**User Role**: Admin

**Steps**:
1. [ ] Login as Admin
2. [ ] Navigate to Admin > Activities > Create
3. [ ] Fill in basic activity details
4. [ ] Select Activity Type: "SchoolCollaboration"
5. [ ] **Verify**: Club selection button appears
6. [ ] **Verify**: Collaboration Point field appears (range 1-3)
7. [ ] **Verify**: Movement Point field is hidden
8. [ ] Select "English Club" as collaborating club
9. [ ] Enter Collaboration Point: 3
10. [ ] Set IsPublic: true
11. [ ] Click "Create Activity"
12. [ ] **Verify**: Activity created successfully with:
    - Collaborating Club: "English Club"
    - Collaboration Point: 3
    - Movement Point: null or 0

**Expected Result**: ✅ School collaboration activity created

---

## Test Suite 3: Club Manager - Club Collaboration Creation

### Test Case 3.1: Create Club Collaboration Activity
**User Role**: Club Manager (Tech Club)

**Steps**:
1. [ ] Login as Club Manager for Tech Club
2. [ ] Navigate to Club Manager > Activities > Create
3. [ ] Fill in basic activity details
4. [ ] Select Activity Type: "ClubCollaboration"
5. [ ] **Verify**: Club selection button appears
6. [ ] **Verify**: Collaboration Point field appears (range 1-3)
7. [ ] **Verify**: Movement Point field appears (range 1-10)
8. [ ] Click "Select Collaborating Club"
9. [ ] **Verify**: Modal shows clubs EXCLUDING Tech Club (own club)
10. [ ] Select "English Club"
11. [ ] Enter Collaboration Point: 2
12. [ ] Enter Movement Point: 5
13. [ ] Set IsPublic: false
14. [ ] Click "Create Activity"
15. [ ] **Verify**: Activity created with:
    - Organizing Club: Tech Club
    - Collaborating Club: English Club
    - Collaboration Point: 2
    - Movement Point: 5
    - Status: "PendingApproval"

**Expected Result**: ✅ Club collaboration created with both point types

---

### Test Case 3.2: Validation - Cannot Select Own Club
**User Role**: Club Manager (Tech Club)

**Steps**:
1. [ ] Navigate to Club Manager > Activities > Create
2. [ ] Select Activity Type: "ClubCollaboration"
3. [ ] Open club selection modal
4. [ ] **Verify**: Tech Club (own club) is NOT in the list
5. [ ] **Verify**: Only other clubs are shown

**Expected Result**: ✅ Own club is excluded from selection

---

### Test Case 3.3: Validation - Point Ranges
**User Role**: Club Manager

**Steps**:
1. [ ] Create ClubCollaboration activity
2. [ ] Enter Movement Point: 0
3. [ ] **Verify**: Error "Must be between 1 and 10"
4. [ ] Enter Movement Point: 11
5. [ ] **Verify**: Error "Must be between 1 and 10"
6. [ ] Enter Movement Point: 5
7. [ ] Enter Collaboration Point: 0
8. [ ] **Verify**: Error "Must be between 1 and 3"
9. [ ] Enter Collaboration Point: 4
10. [ ] **Verify**: Error "Must be between 1 and 3"

**Expected Result**: ✅ Point range validation works

---

## Test Suite 4: Club Manager - School Collaboration Creation

### Test Case 4.1: Create School Collaboration Activity
**User Role**: Club Manager (Tech Club)

**Steps**:
1. [ ] Login as Club Manager
2. [ ] Navigate to Club Manager > Activities > Create
3. [ ] Select Activity Type: "SchoolCollaboration"
4. [ ] **Verify**: Club selection button is HIDDEN
5. [ ] **Verify**: Collaboration Point field is HIDDEN
6. [ ] **Verify**: Movement Point field appears (range 1-10)
7. [ ] Enter Movement Point: 7
8. [ ] Fill in other details
9. [ ] Click "Create Activity"
10. [ ] **Verify**: Activity created with:
    - Organizing Club: Tech Club
    - Collaborating Club: null
    - Collaboration Point: null
    - Movement Point: 7

**Expected Result**: ✅ School collaboration created without collaborating club

---

## Test Suite 5: Activity Editing

### Test Case 5.1: Edit Activity - Change Type
**User Role**: Admin or Club Manager

**Steps**:
1. [ ] Open an existing ClubCollaboration activity for editing
2. [ ] **Verify**: Collaboration fields are populated
3. [ ] Change Activity Type to "LargeEvent"
4. [ ] **Verify**: Collaboration fields disappear
5. [ ] **Verify**: Collaboration data is cleared
6. [ ] Change back to "ClubCollaboration"
7. [ ] **Verify**: Collaboration fields reappear (empty)
8. [ ] Re-select club and points
9. [ ] Save changes
10. [ ] **Verify**: Changes saved correctly

**Expected Result**: ✅ Type changes handle collaboration fields correctly

---

## Test Suite 6: Registration Eligibility

### Test Case 6.1: Public Club Collaboration - Any User Can Register
**User Role**: Student 3 (not member of any club)

**Steps**:
1. [ ] Login as Student 3
2. [ ] Navigate to Activities list
3. [ ] Find a PUBLIC ClubCollaboration activity
4. [ ] Click "Register"
5. [ ] **Verify**: Registration succeeds
6. [ ] **Verify**: Success message displayed

**Expected Result**: ✅ Non-member can register for public activity

---

### Test Case 6.2: Non-Public Club Collaboration - Organizer Member
**User Role**: Student 1 (member of Tech Club)

**Steps**:
1. [ ] Login as Student 1
2. [ ] Find a NON-PUBLIC ClubCollaboration activity
   - Organizing Club: Tech Club
   - Collaborating Club: English Club
3. [ ] Click "Register"
4. [ ] **Verify**: Registration succeeds
5. [ ] **Verify**: Student 1 is registered

**Expected Result**: ✅ Organizer club member can register

---

### Test Case 6.3: Non-Public Club Collaboration - Collaborator Member
**User Role**: Student 2 (member of English Club)

**Steps**:
1. [ ] Login as Student 2
2. [ ] Find the same NON-PUBLIC ClubCollaboration activity
   - Organizing Club: Tech Club
   - Collaborating Club: English Club
3. [ ] Click "Register"
4. [ ] **Verify**: Registration succeeds
5. [ ] **Verify**: Student 2 is registered

**Expected Result**: ✅ Collaborating club member can register

---

### Test Case 6.4: Non-Public Club Collaboration - Non-Member Rejected
**User Role**: Student 3 (not member of either club)

**Steps**:
1. [ ] Login as Student 3
2. [ ] Find the NON-PUBLIC ClubCollaboration activity
3. [ ] Try to register
4. [ ] **Verify**: Registration fails
5. [ ] **Verify**: Error message: "This activity is for members of the organizing or collaborating clubs only"

**Expected Result**: ✅ Non-member cannot register for non-public collaboration

---

## Test Suite 7: Activity Display

### Test Case 7.1: Activity Detail View
**User Role**: Any user

**Steps**:
1. [ ] Navigate to activity details page for a collaboration activity
2. [ ] **Verify**: Collaborating club name is displayed
3. [ ] **Verify**: Collaborating club logo is displayed (if available)
4. [ ] **Verify**: Collaboration point value is shown
5. [ ] **Verify**: Activity type indicates collaboration
6. [ ] **Verify**: All information is clearly visible

**Expected Result**: ✅ Collaboration info displayed correctly

---

### Test Case 7.2: Activity List View
**User Role**: Any user

**Steps**:
1. [ ] Navigate to activities list
2. [ ] Find collaboration activities
3. [ ] **Verify**: Visual indicator for collaboration activities
4. [ ] **Verify**: Collaborating club name shown in list item
5. [ ] **Verify**: Can distinguish collaboration from regular activities

**Expected Result**: ✅ Collaboration activities clearly marked in list

---

## Test Suite 8: Club Selection Modal

### Test Case 8.1: Modal Functionality
**User Role**: Admin or Club Manager

**Steps**:
1. [ ] Open activity creation form
2. [ ] Select ClubCollaboration type
3. [ ] Click "Select Collaborating Club"
4. [ ] **Verify**: Modal opens
5. [ ] **Verify**: List of clubs displayed
6. [ ] **Verify**: Each club shows:
   - Club logo
   - Club name
   - Member count
7. [ ] **Verify**: Search/filter functionality works (if implemented)
8. [ ] Select a club
9. [ ] **Verify**: Modal closes
10. [ ] **Verify**: Selected club appears in form

**Expected Result**: ✅ Modal works smoothly

---

### Test Case 8.2: Modal - Club Manager Exclusion
**User Role**: Club Manager (Tech Club)

**Steps**:
1. [ ] Open club selection modal
2. [ ] **Verify**: Tech Club is NOT in the list
3. [ ] **Verify**: All other clubs are present
4. [ ] Count clubs in modal
5. [ ] **Verify**: Count = Total clubs - 1 (own club)

**Expected Result**: ✅ Own club excluded for managers

---

## Test Suite 9: Form Validation

### Test Case 9.1: Client-Side Validation
**User Role**: Any authorized user

**Steps**:
1. [ ] Try to create activity with invalid data
2. [ ] **Verify**: Validation errors appear immediately
3. [ ] **Verify**: Error messages are clear
4. [ ] **Verify**: Form cannot be submitted with errors
5. [ ] Fix all errors
6. [ ] **Verify**: Form can be submitted

**Expected Result**: ✅ Client-side validation prevents invalid submissions

---

### Test Case 9.2: Server-Side Validation
**User Role**: Any authorized user

**Steps**:
1. [ ] Bypass client validation (using browser dev tools)
2. [ ] Submit invalid data
3. [ ] **Verify**: Server returns validation error
4. [ ] **Verify**: Error message displayed to user
5. [ ] **Verify**: Form not submitted

**Expected Result**: ✅ Server-side validation catches invalid data

---

## Test Suite 10: End-to-End Workflows

### Test Case 10.1: Complete Collaboration Workflow
**Roles**: Admin, Club Manager, Students

**Steps**:
1. [ ] **Admin**: Create ClubCollaboration activity
   - Collaborating Club: Tech Club
   - Collaboration Point: 2
   - IsPublic: false
2. [ ] **Club Manager**: Approve activity (if needed)
3. [ ] **Student 1** (Tech Club member): Register for activity
4. [ ] **Student 2** (English Club member): Register for activity
5. [ ] **Student 3** (non-member): Try to register (should fail)
6. [ ] **Activity starts**
7. [ ] **Students**: Check in with attendance code
8. [ ] **Admin/Manager**: Mark attendance
9. [ ] **Verify**: Points assigned correctly:
   - Tech Club members get Collaboration Points (2)
   - Other participants get appropriate points
10. [ ] **Students**: Submit feedback
11. [ ] **Admin**: View feedback and attendance reports

**Expected Result**: ✅ Complete workflow works end-to-end

---

## Test Suite 11: Edge Cases

### Test Case 11.1: Activity with Deleted Club
**Steps**:
1. [ ] Create collaboration activity with Club A
2. [ ] Delete Club A (if system allows)
3. [ ] View activity details
4. [ ] **Verify**: System handles gracefully (no crash)
5. [ ] **Verify**: Appropriate message shown

**Expected Result**: ✅ System handles deleted club gracefully

---

### Test Case 11.2: Concurrent Editing
**Steps**:
1. [ ] User A opens activity for editing
2. [ ] User B opens same activity for editing
3. [ ] User A saves changes
4. [ ] User B tries to save changes
5. [ ] **Verify**: System handles conflict appropriately

**Expected Result**: ✅ Concurrent edits handled properly

---

## Test Suite 12: Performance and UX

### Test Case 12.1: Large Club List
**Steps**:
1. [ ] Ensure system has 50+ clubs
2. [ ] Open club selection modal
3. [ ] **Verify**: Modal loads quickly (< 2 seconds)
4. [ ] **Verify**: Search/filter works smoothly
5. [ ] **Verify**: Scrolling is smooth

**Expected Result**: ✅ Good performance with many clubs

---

### Test Case 12.2: Field Visibility Transitions
**Steps**:
1. [ ] Open activity creation form
2. [ ] Change activity type multiple times
3. [ ] **Verify**: Fields appear/disappear smoothly
4. [ ] **Verify**: No visual glitches
5. [ ] **Verify**: Form remains usable

**Expected Result**: ✅ Smooth UI transitions

---

## Summary Checklist

After completing all tests, verify:

- [ ] All validation tests passed
- [ ] All workflow tests passed
- [ ] All UI tests passed
- [ ] No critical bugs found
- [ ] Performance is acceptable
- [ ] User experience is smooth
- [ ] Documentation is accurate

## Bug Reporting Template

If you find any issues during testing, document them using this template:

```
**Bug ID**: [Unique identifier]
**Test Case**: [Which test case revealed the bug]
**Severity**: [Critical/High/Medium/Low]
**Description**: [What went wrong]
**Steps to Reproduce**:
1. 
2. 
3. 
**Expected Result**: [What should happen]
**Actual Result**: [What actually happened]
**Screenshots**: [If applicable]
**Environment**: [Browser, OS, etc.]
```

---

## Notes

- Perform tests in multiple browsers (Chrome, Firefox, Edge)
- Test on different screen sizes (desktop, tablet, mobile)
- Clear browser cache between major test suites
- Document any unexpected behavior
- Take screenshots of successful test completions
- Report any usability issues even if functionality works

---

**Testing Completed By**: _______________
**Date**: _______________
**Overall Result**: ☐ Pass ☐ Fail ☐ Pass with Minor Issues
