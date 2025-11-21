# Schedule Time Validation Rules

## Overview
Khi ClubManager tạo Activity với Schedule items, các validation rules sau được áp dụng để đảm bảo tính hợp lý của dữ liệu.

## Validation Rules

### 1. Basic Time Validation
**Rule:** Schedule End Time phải sau Schedule Start Time
- **Error Message:** "End time must be after start time"
- **Type:** Hard validation (blocks submission)
- **Example:** 
  - ✅ Start: 09:00, End: 10:00
  - ❌ Start: 10:00, End: 09:00

### 2. Activity Time Range Validation
**Rule:** Schedule time phải nằm trong khoảng thời gian của Activity

#### 2.1 Start Time Validation
- Schedule Start Time >= Activity Start Time
- **Error Message:** "Schedule start time cannot be before activity start time"
- **Type:** Hard validation (blocks submission)
- **Example:**
  - Activity: 2024-01-15 09:00 - 2024-01-15 17:00
  - ✅ Schedule Start: 10:00
  - ❌ Schedule Start: 08:00

#### 2.2 End Time Validation
- Schedule End Time <= Activity End Time
- **Error Message:** "Schedule end time cannot be after activity end time"
- **Type:** Hard validation (blocks submission)
- **Example:**
  - Activity: 2024-01-15 09:00 - 2024-01-15 17:00
  - ✅ Schedule End: 16:00
  - ❌ Schedule End: 18:00

### 3. Schedule Overlap Detection
**Rule:** Phát hiện khi các schedule items có thời gian overlap
- **Warning Message:** "⚠️ Overlaps with Schedule Item #X"
- **Type:** Soft validation (warning only, không block submission)
- **Color:** Orange (#ff9800)
- **Rationale:** Một số activities có thể cố ý có overlapping schedules (ví dụ: nhiều tracks song song)

**Overlap Detection Logic:**
```
Schedule A overlaps Schedule B if:
- A.start >= B.start AND A.start < B.end, OR
- A.end > B.start AND A.end <= B.end, OR
- A.start <= B.start AND A.end >= B.end
```

### 4. Midnight Crossing Handling
**Rule:** Nếu Schedule End Time < Schedule Start Time (về mặt giờ), tự động cộng thêm 1 ngày
- **Example:**
  - Start: 23:00, End: 01:00
  - System interprets as: 23:00 same day → 01:00 next day

## UI/UX Features

### Helper Text
- Start Time: "Must be within activity time range"
- End Time: "Must be after start time and within activity range"

### Real-time Validation
- Validation chạy khi user thay đổi time inputs
- Validation chạy lại cho tất cả schedules khi Activity time thay đổi
- Error messages hiển thị ngay lập tức dưới input fields
- **Auto-clear Invalid Values:** Giá trị không hợp lệ tự động bị xóa và focus vào field để nhập lại

### Visual Feedback
- **Error (Red):** Hard validation failures
  - Invalid value is automatically cleared
  - Input field receives focus for re-entry
  - Clear error message explaining what went wrong
- **Warning (Orange):** Soft validation warnings (overlap detection)
  - Value is NOT cleared (may be intentional)
  - Warning message displayed
- **Success (No message):** All validations pass

## Implementation Details

### JavaScript Functions
- `validateScheduleTime(scheduleItem)`: Main validation function
  - Returns `false` if validation fails (hard error)
  - Returns `true` if validation passes or only has warnings
  - Automatically clears invalid values
  - Sets focus to the invalid field
- `revalidateAllSchedules()`: Re-validate all schedules (called when activity time changes)

### Validation Timing
1. **On schedule time input change** (individual field validation)
   - Start Time validates immediately when entered
   - End Time validates immediately when entered
   - No need to wait for both fields to be filled
2. **On activity time change** (revalidates all schedules)
3. **On form submission** (final validation)

### Auto-Clear Behavior
When validation fails:
1. Error message is displayed
2. Invalid value is cleared (`input.value = ''`)
3. Input field receives focus (`input.focus()`)
4. User must enter a new valid value

### Independent Field Validation
- **Start Time** validates independently when entered
  - Checks against activity start time immediately
  - No need to wait for end time to be filled
- **End Time** validates independently when entered
  - Checks against activity end time immediately
  - No need to wait for start time to be filled
- **Cross-field validation** (end > start) only runs when both fields have values
- **Overlap detection** only runs when both fields have values

## UX Benefits of Auto-Clear

### Why Auto-Clear Invalid Values?

1. **Prevents Form Submission with Invalid Data**
   - User cannot accidentally submit form with invalid times
   - Reduces server-side validation errors

2. **Clear Visual Feedback**
   - Empty field + error message = obvious problem
   - User knows exactly what to fix

3. **Forces Correct Input**
   - User must enter valid value to proceed
   - No confusion about whether current value is acceptable

4. **Reduces Cognitive Load**
   - User doesn't need to remember what was wrong
   - Fresh start with clear guidance

5. **Immediate Focus**
   - Auto-focus on invalid field
   - User can immediately correct the mistake

### User Flow Example

**Scenario:** User enters schedule end time before start time

1. User enters Start Time: 10:00
2. User enters End Time: 09:00
3. **System Response:**
   - ❌ Error message: "End time must be after start time. Please enter a valid time."
   - 🗑️ End Time field is cleared
   - 🎯 Cursor moves to End Time field
4. User enters new End Time: 11:00
5. ✅ Validation passes

## Future Enhancements (Optional)

### Potential Additional Validations
1. **Minimum Duration:** Schedule phải có duration tối thiểu (ví dụ: 15 phút)
2. **Maximum Duration:** Schedule không được quá dài (ví dụ: không quá 8 giờ)
3. **Break Time Suggestions:** Gợi ý thêm break time giữa các schedules dài
4. **Time Slot Recommendations:** Gợi ý time slots phổ biến (9:00, 10:00, 14:00, etc.)
5. **Conflict Resolution:** Tự động suggest time slots không overlap

## Testing Scenarios

### Test Case 1: Valid Schedule
- Activity: 2024-01-15 09:00 - 17:00
- Schedule: 10:00 - 12:00
- Expected: ✅ Pass all validations

### Test Case 2: Schedule Before Activity
- Activity: 2024-01-15 09:00 - 17:00
- Schedule: 08:00 - 10:00
- Expected: ❌ Error "Schedule start time cannot be before activity start time"

### Test Case 3: Schedule After Activity
- Activity: 2024-01-15 09:00 - 17:00
- Schedule: 16:00 - 18:00
- Expected: ❌ Error "Schedule end time cannot be after activity end time"

### Test Case 4: Overlapping Schedules
- Activity: 2024-01-15 09:00 - 17:00
- Schedule 1: 10:00 - 12:00
- Schedule 2: 11:00 - 13:00
- Expected: ⚠️ Warning "Overlaps with Schedule Item #1"

### Test Case 5: End Before Start
- Schedule: 14:00 - 13:00
- Expected: 
  - ❌ Error "End time must be after start time. Please enter a valid time."
  - 🗑️ End time field cleared
  - 🎯 Focus on end time field

### Test Case 6: Auto-Clear on Activity Time Change
- Initial: Activity 09:00-17:00, Schedule 10:00-12:00 ✅
- Change Activity to: 11:00-17:00
- Expected:
  - ❌ Schedule start time (10:00) now invalid
  - 🗑️ Schedule start time cleared
  - Error: "Schedule start time cannot be before activity start time. Please enter a valid time."

### Test Case 7: Independent Start Time Validation
- Activity: 2024-01-15 10:00 - 17:00
- User enters Schedule Start Time: 09:00 (before activity start)
- Expected:
  - ❌ Error immediately: "Schedule start time cannot be before activity start time. Please enter a valid time."
  - 🗑️ Start time field cleared
  - 🎯 Focus on start time field
  - **Note:** Validation happens even if end time is not yet filled

### Test Case 8: Independent End Time Validation
- Activity: 2024-01-15 10:00 - 17:00
- User enters Schedule End Time: 18:00 (after activity end)
- Expected:
  - ❌ Error immediately: "Schedule end time cannot be after activity end time. Please enter a valid time."
  - 🗑️ End time field cleared
  - 🎯 Focus on end time field
  - **Note:** Validation happens even if start time is not yet filled

## Key Improvement: Independent Field Validation

### Problem (Before Fix)
Validation only ran when BOTH start time AND end time were filled:
```javascript
if (!startTimeInput.value || !endTimeInput.value) {
    return true; // Skip validation
}
```

**Issue:** User could enter invalid start time (e.g., before activity start), and no error would show until they also filled end time.

### Solution (After Fix)
Each field validates independently as soon as it has a value:
- **Start Time:** Validates against activity start time immediately
- **End Time:** Validates against activity end time immediately
- **Cross-validation:** Only runs when both fields have values

**Benefit:** Immediate feedback - user knows right away if their input is invalid.

## Visual Examples

### Before Auto-Clear (Old Behavior)
```
Start Time: [10:00] ✓
End Time:   [09:00] ✗ "End time must be after start time"
                    ↑ User sees error but value still there
                    ↑ Confusing - should I delete it manually?
```

### After Auto-Clear (New Behavior)
```
Start Time: [10:00] ✓
End Time:   [     ] ✗ "End time must be after start time. Please enter a valid time."
                    ↑ Field is empty and focused
                    ↑ Clear indication to enter new value
```
