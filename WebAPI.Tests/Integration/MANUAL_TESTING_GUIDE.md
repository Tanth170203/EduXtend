# AI Chatbot Assistant - Manual Testing Guide

This guide provides comprehensive manual testing procedures for the AI Chatbot Assistant feature.

## Prerequisites

Before starting manual testing:

1. ✅ Ensure Gemini API key is configured in `WebAPI/appsettings.json`
2. ✅ Database is seeded with test data (students, clubs, activities)
3. ✅ Application is running (WebAPI and WebFE)
4. ✅ Test user account is created and can log in

## Test Environment Setup

### Test User Credentials
- **Student ID**: [Your test student ID]
- **Email**: [Your test email]
- **Major**: Software Engineering (or relevant major)
- **Current Clubs**: At least one club membership

### Test Data Requirements
- At least 3-5 active clubs with recruitment open
- At least 3-5 upcoming activities (StartTime > Now)
- Various club categories (Technology, Arts, Sports, etc.)

---

## 13.1 End-to-End Chat Flow Tests

### Test Case 1.1: Find Clubs Matching Major

**Objective**: Verify AI responds with relevant club recommendations based on student's major

**Steps**:
1. Log in as a student
2. Open the chatbot by clicking the floating chat button
3. Type: "Tôi muốn tìm CLB phù hợp với chuyên ngành của mình"
4. Click Send or press Enter

**Expected Results**:
- ✅ AI responds within 5 seconds
- ✅ Response mentions specific club names
- ✅ Response explains why clubs match the student's major
- ✅ Response is in Vietnamese
- ✅ Response is friendly and encouraging

**Pass/Fail**: ⬜

**Notes**:
```
[Record actual AI response and observations here]
```

---

### Test Case 1.2: Ask About Upcoming Activities

**Objective**: Verify AI responds with upcoming activity recommendations

**Steps**:
1. Continue from previous test (chatbot still open)
2. Type: "Có hoạt động nào sắp tới không?"
3. Click Send

**Expected Results**:
- ✅ AI responds with specific activity names
- ✅ Response includes dates/times for activities
- ✅ Response includes locations
- ✅ Activities mentioned are actually upcoming (StartTime > Now)
- ✅ Response is personalized based on student's interests/clubs

**Pass/Fail**: ⬜

**Notes**:
```
[Record actual AI response and observations here]
```

---

### Test Case 1.3: Conversation History Maintained

**Objective**: Verify conversation context is maintained across multiple messages

**Steps**:
1. Continue from previous tests
2. Type: "Cho tôi biết thêm về hoạt động đầu tiên"
3. Click Send

**Expected Results**:
- ✅ AI refers back to the first activity mentioned in previous response
- ✅ AI provides additional details about that specific activity
- ✅ No need to repeat activity name - context is understood
- ✅ Response is coherent with conversation flow

**Pass/Fail**: ⬜

**Notes**:
```
[Record actual AI response and observations here]
```

---

### Test Case 1.4: Quick Action Buttons

**Objective**: Verify quick action buttons trigger appropriate AI responses

**Steps**:
1. Close and reopen the chatbot (to see welcome message)
2. Click "🔍 Tìm CLB phù hợp" button
3. Wait for response
4. Click "📅 Xem hoạt động" button
5. Wait for response
6. Click "💡 Tìm hiểu thêm" button
7. Wait for response

**Expected Results**:
- ✅ Each button sends the corresponding message
- ✅ "Tìm CLB phù hợp" returns club recommendations
- ✅ "Xem hoạt động" returns activity information
- ✅ "Tìm hiểu thêm" returns general information about the system
- ✅ All responses are relevant to the button clicked

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations for each quick action]
```

---

## 13.2 UI/UX Tests on Different Devices and Browsers

### Test Case 2.1: Floating Chat Button Visibility

**Objective**: Verify floating chat button appears on all pages

**Browsers to Test**: Chrome, Firefox, Edge

**Pages to Test**:
- Home/Dashboard
- Clubs List
- Club Details
- Activities List
- Activity Details
- Profile Page
- Any other authenticated pages

**Steps** (repeat for each browser):
1. Log in as student
2. Navigate to each page listed above
3. Verify floating chat button is visible

**Expected Results**:
- ✅ Button appears in bottom-right corner on all pages
- ✅ Button has consistent styling across pages
- ✅ Button is always accessible (not hidden by other elements)
- ✅ Button has hover effect

**Test Results**:

| Page | Chrome | Firefox | Edge |
|------|--------|---------|------|
| Home | ⬜ | ⬜ | ⬜ |
| Clubs List | ⬜ | ⬜ | ⬜ |
| Club Details | ⬜ | ⬜ | ⬜ |
| Activities List | ⬜ | ⬜ | ⬜ |
| Activity Details | ⬜ | ⬜ | ⬜ |
| Profile | ⬜ | ⬜ | ⬜ |

**Notes**:
```
[Record any issues or observations]
```

---

### Test Case 2.2: Chat Modal Open/Close

**Objective**: Verify chat modal opens and closes correctly

**Steps**:
1. Click floating chat button
2. Verify modal opens
3. Click X (close button)
4. Verify modal closes
5. Click floating button again
6. Click outside modal area
7. Verify modal behavior

**Expected Results**:
- ✅ Modal opens smoothly with animation
- ✅ Modal appears centered or in appropriate position
- ✅ Close button (X) closes the modal
- ✅ Clicking outside modal does NOT close it (or does, based on design)
- ✅ Modal content is preserved when reopened

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.3: Welcome Message Display

**Objective**: Verify welcome message displays on first open

**Steps**:
1. Clear browser session storage (F12 > Application > Session Storage > Clear)
2. Refresh page
3. Click floating chat button

**Expected Results**:
- ✅ Welcome message "Xin chào! 👋" appears
- ✅ Introduction message appears
- ✅ Three quick action buttons appear
- ✅ No previous chat history is shown

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.4: Message Display Styling

**Objective**: Verify user and AI messages display with correct styling

**Steps**:
1. Send a user message: "Hello"
2. Wait for AI response
3. Inspect message bubbles

**Expected Results**:

**User Messages**:
- ✅ Displayed on right side
- ✅ Different background color (e.g., blue)
- ✅ Timestamp shown
- ✅ Text is readable

**AI Messages**:
- ✅ Displayed on left side
- ✅ Different background color (e.g., gray)
- ✅ Timestamp shown
- ✅ Text is readable
- ✅ Proper formatting (line breaks, lists if applicable)

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.5: Typing Indicator

**Objective**: Verify typing indicator shows while waiting for AI response

**Steps**:
1. Send a message
2. Immediately observe the chat area

**Expected Results**:
- ✅ Typing indicator appears immediately after sending
- ✅ Indicator shows "AI đang suy nghĩ..." or similar
- ✅ Indicator has animation (e.g., dots bouncing)
- ✅ Indicator disappears when response arrives
- ✅ Send button is disabled while indicator is showing

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.6: Error Message Display

**Objective**: Verify error messages display correctly in chat UI

**Steps**:
1. Temporarily disconnect internet or stop WebAPI
2. Send a message
3. Observe error handling

**Expected Results**:
- ✅ Error message appears in chat area
- ✅ Error message has distinct styling (e.g., red color)
- ✅ Error message is user-friendly (Vietnamese)
- ✅ User can retry after error
- ✅ Send button is re-enabled after error

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.7: Chat History Persistence

**Objective**: Verify chat history persists when modal is closed and reopened

**Steps**:
1. Send 3-4 messages and receive responses
2. Close the chat modal
3. Navigate to a different page
4. Reopen the chat modal

**Expected Results**:
- ✅ All previous messages are still visible
- ✅ Messages are in correct order
- ✅ Scroll position is at bottom (most recent message)
- ✅ Can continue conversation from where left off

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.8: Chat History Clears on Logout

**Objective**: Verify chat history is cleared when user logs out

**Steps**:
1. Have some chat history
2. Log out
3. Log back in
4. Open chatbot

**Expected Results**:
- ✅ Chat history is empty
- ✅ Welcome message appears again
- ✅ No previous conversation is visible

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 2.9: Mobile Responsive Design

**Objective**: Verify chatbot works on mobile devices

**Devices to Test**: iPhone, Android phone, Tablet

**Steps** (for each device):
1. Open application on mobile browser
2. Log in
3. Click floating chat button
4. Send messages
5. Test all interactions

**Expected Results**:
- ✅ Floating button is appropriately sized for touch
- ✅ Chat modal fits screen properly
- ✅ Text input is accessible (keyboard doesn't cover it)
- ✅ Messages are readable without horizontal scrolling
- ✅ All buttons are touch-friendly
- ✅ Typing indicator is visible
- ✅ Can scroll through chat history

**Test Results**:

| Feature | iPhone | Android | Tablet |
|---------|--------|---------|--------|
| Button Size | ⬜ | ⬜ | ⬜ |
| Modal Layout | ⬜ | ⬜ | ⬜ |
| Text Input | ⬜ | ⬜ | ⬜ |
| Message Display | ⬜ | ⬜ | ⬜ |
| Scrolling | ⬜ | ⬜ | ⬜ |

**Notes**:
```
[Record observations for each device]
```

---

## 13.3 Error Handling Tests

### Test Case 3.1: Invalid Gemini API Key

**Objective**: Verify error message displays when API key is invalid

**Steps**:
1. Stop WebAPI
2. Edit `appsettings.json` - set invalid API key
3. Restart WebAPI
4. Send a message in chatbot

**Expected Results**:
- ✅ Error message appears in chat
- ✅ Message: "Cấu hình AI Assistant không hợp lệ. Vui lòng liên hệ quản trị viên."
- ✅ Error is logged on server
- ✅ User can still interact with UI

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations and server logs]
```

---

### Test Case 3.2: Network Disconnected

**Objective**: Verify timeout error displays when network is unavailable

**Steps**:
1. Disconnect internet or block API endpoint
2. Send a message
3. Wait for timeout (30 seconds)

**Expected Results**:
- ✅ Typing indicator shows for up to 30 seconds
- ✅ After timeout, error message appears
- ✅ Message: "Không thể kết nối đến AI Assistant. Vui lòng thử lại sau."
- ✅ Send button is re-enabled
- ✅ User can retry

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 3.3: Unauthenticated User

**Objective**: Verify redirect to login when user is not authenticated

**Steps**:
1. Log out
2. Try to access a page with chatbot
3. Try to click chatbot button (if visible)

**Expected Results**:
- ✅ Chatbot button is NOT visible when logged out
- ✅ OR if visible and clicked, redirects to login page
- ✅ After login, user can access chatbot normally

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 3.4: Rate Limit Exceeded

**Objective**: Verify 429 error message when rate limit is exceeded

**Steps**:
1. Send 11+ messages rapidly (within 1 minute)
2. Observe response after limit is reached

**Expected Results**:
- ✅ After 10 requests, subsequent requests return error
- ✅ Error message: "AI Assistant tạm thời quá tải. Vui lòng thử lại sau ít phút."
- ✅ HTTP status 429 is returned
- ✅ After waiting 1 minute, requests work again

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 3.5: Database Connection Error

**Objective**: Verify generic error message when database is unavailable

**Steps**:
1. Stop database server or break connection string
2. Send a message

**Expected Results**:
- ✅ Generic error message appears
- ✅ Message: "Đã xảy ra lỗi. Vui lòng thử lại sau."
- ✅ Error is logged on server with full details
- ✅ No sensitive information exposed to user

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations and server logs]
```

---

## 13.4 Performance and Scalability Tests

### Test Case 4.1: API Response Time

**Objective**: Measure API response time for typical requests

**Steps**:
1. Open browser DevTools (F12) > Network tab
2. Send various messages
3. Record response times

**Test Messages**:
- "Tôi muốn tìm CLB phù hợp"
- "Có hoạt động nào sắp tới không?"
- "Cho tôi biết thêm về CLB công nghệ"

**Expected Results**:
- ✅ 95% of requests complete in < 5 seconds
- ✅ Average response time < 3 seconds
- ✅ No requests timeout (30 seconds)

**Recorded Times**:

| Message | Response Time | Pass/Fail |
|---------|---------------|-----------|
| Message 1 | ___ seconds | ⬜ |
| Message 2 | ___ seconds | ⬜ |
| Message 3 | ___ seconds | ⬜ |
| Message 4 | ___ seconds | ⬜ |
| Message 5 | ___ seconds | ⬜ |

**Average**: ___ seconds

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 4.2: UI Responsiveness During API Calls

**Objective**: Verify chat UI remains responsive during API calls

**Steps**:
1. Send a message
2. While waiting for response, try to:
   - Scroll through chat history
   - Click close button
   - Interact with other page elements

**Expected Results**:
- ✅ Can scroll chat history while waiting
- ✅ Can close modal while waiting (cancels request)
- ✅ UI doesn't freeze or become unresponsive
- ✅ Other page elements remain interactive

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

### Test Case 4.3: Session Storage Limit

**Objective**: Verify session storage doesn't grow unbounded

**Steps**:
1. Send 60+ messages (more than 50 message limit)
2. Check browser session storage (F12 > Application > Session Storage)
3. Count messages stored

**Expected Results**:
- ✅ Only last 50 messages are stored
- ✅ Older messages are removed automatically
- ✅ Storage size remains reasonable (< 1MB)
- ✅ No performance degradation with many messages

**Pass/Fail**: ⬜

**Notes**:
```
[Record actual message count and storage size]
```

---

### Test Case 4.4: Caching Reduces Database Queries

**Objective**: Verify caching reduces repeated database queries

**Steps**:
1. Enable SQL logging in application
2. Send first message
3. Count database queries
4. Send second message within 5 minutes
5. Count database queries again

**Expected Results**:
- ✅ First request makes queries for student context, clubs, activities
- ✅ Second request (within cache time) makes fewer queries
- ✅ Student context is cached (5 min)
- ✅ Clubs are cached (10 min)
- ✅ Activities are cached (5 min)

**Pass/Fail**: ⬜

**Notes**:
```
[Record query counts and observations]
```

---

### Test Case 4.5: Rate Limiting Prevents Abuse

**Objective**: Verify rate limiting prevents rapid requests

**Steps**:
1. Create a script or manually send 15 requests rapidly
2. Observe responses

**Expected Results**:
- ✅ First 10 requests succeed (200 OK)
- ✅ Requests 11-15 return 429 Too Many Requests
- ✅ Error message is displayed
- ✅ After 1 minute, requests work again
- ✅ Rate limit is per-user (doesn't affect other users)

**Pass/Fail**: ⬜

**Notes**:
```
[Record observations]
```

---

## Test Summary

### Overall Results

| Test Category | Total Tests | Passed | Failed | Skipped |
|---------------|-------------|--------|--------|---------|
| 13.1 End-to-End | 4 | ⬜ | ⬜ | ⬜ |
| 13.2 UI/UX | 9 | ⬜ | ⬜ | ⬜ |
| 13.3 Error Handling | 5 | ⬜ | ⬜ | ⬜ |
| 13.4 Performance | 5 | ⬜ | ⬜ | ⬜ |
| **TOTAL** | **23** | **⬜** | **⬜** | **⬜** |

### Critical Issues Found

```
[List any critical issues that must be fixed before release]
```

### Minor Issues Found

```
[List any minor issues or improvements]
```

### Recommendations

```
[List any recommendations for improvements or future enhancements]
```

### Sign-Off

- **Tester Name**: _______________
- **Date**: _______________
- **Status**: ⬜ Approved for Release  ⬜ Needs Fixes  ⬜ Blocked

---

## Appendix: Test Data Setup Scripts

### SQL Script to Create Test Student

```sql
-- Insert test student with clubs and activities
-- Run this script to set up test data for manual testing

-- [Add SQL scripts here if needed]
```

### Quick Test Checklist

Use this for rapid smoke testing:

- ⬜ Chatbot button visible
- ⬜ Modal opens/closes
- ⬜ Can send message
- ⬜ AI responds
- ⬜ Typing indicator works
- ⬜ Error handling works
- ⬜ Mobile responsive
- ⬜ Chat history persists
- ⬜ Logout clears history
- ⬜ Performance acceptable
