# 🧪 **COMPLETE TEST GUIDE - ALL SCORING FEATURES**

## **Date: 17/10/2025**
## **Version: 2.0 (with /api/students endpoint)**

---

# **PART 1: API TESTING**

## **TEST 1: API /api/students Endpoint**

### **Prerequisite**:
- WebAPI running on `http://localhost:5000`
- Have Admin JWT token ready
- Database has at least 3 Active students

### **Test 1.1: GET /api/students (Get All Active Students)**

**Step 1**: Open Postman or Terminal

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json"
```

**Expected Response**:
```
Status: 200 OK

Body:
[
  {
    "id": 1,
    "studentCode": "SE160001",
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@fpt.edu.vn",
    "cohort": "K16"
  },
  {
    "id": 2,
    "studentCode": "SE160002",
    "fullName": "Trần Thị B",
    "email": "tranthib@fpt.edu.vn",
    "cohort": "K16"
  },
  ...
]
```

**Verification Checklist**:
- ✅ Response is Array of students
- ✅ Each student has 5 properties
- ✅ Only Active students in list
- ✅ Sorted by FullName alphabetically
- ✅ No Inactive/Graduated students

---

### **Test 1.2: GET /api/students (Without Token)**

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students"
```

**Expected Response**:
```
Status: 401 Unauthorized

Body:
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**Verification**: ✅ Request blocked without token

---

### **Test 1.3: GET /api/students (Non-Admin Token)**

**Setup**: Use Student or Teacher JWT token (not Admin)

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students" \
  -H "Authorization: Bearer STUDENT_TOKEN"
```

**Expected Response**:
```
Status: 403 Forbidden

Body:
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.2",
  "title": "Forbidden",
  "status": 403
}
```

**Verification**: ✅ Non-Admin users blocked

---

### **Test 1.4: GET /api/students/{id} (Get Student Details)**

**cURL Command** (using student ID 1):
```bash
curl -X GET "http://localhost:5000/api/students/1" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response**:
```
Status: 200 OK

Body:
{
  "id": 1,
  "studentCode": "SE160001",
  "fullName": "Nguyễn Văn A",
  "email": "nguyenvana@fpt.edu.vn",
  "cohort": "K16",
  "phone": "0912345678",
  "dateOfBirth": "2002-01-15",
  "gender": "Male",
  "status": "Active"
}
```

**Verification Checklist**:
- ✅ All 8 fields present
- ✅ DateOfBirth is ISO format (YYYY-MM-DD)
- ✅ Gender is string ("Male", "Female", "Other")
- ✅ Status is string ("Active", "Inactive", "Graduated")

---

### **Test 1.5: GET /api/students/{id} (Invalid ID)**

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students/99999" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response**:
```
Status: 404 Not Found

Body:
{
  "message": "Student with ID 99999 not found."
}
```

**Verification**: ✅ Returns 404 for non-existent student

---

### **Test 1.6: GET /api/students/search (Search by Name)**

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students/search?query=Nguyễn" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response**:
```
Status: 200 OK

Body:
[
  {
    "id": 1,
    "studentCode": "SE160001",
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@fpt.edu.vn",
    "cohort": "K16"
  },
  {
    "id": 3,
    "studentCode": "SE160003",
    "fullName": "Nguyễn Văn C",
    "email": "nguyenvanc@fpt.edu.vn",
    "cohort": "K16"
  }
]
```

**Verification**:
- ✅ Returns only matching results
- ✅ Partial match works
- ✅ Case-insensitive

---

### **Test 1.7: GET /api/students/search (Search by Code)**

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students/search?query=SE16000" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response**: Students with code matching "SE16000"

---

### **Test 1.8: GET /api/students/search (Empty Query)**

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/students/search?query=" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response**:
```
Status: 400 Bad Request

Body:
{
  "message": "Search query is required."
}
```

---

# **PART 2: UI TESTING - SCORING MODAL**

## **Prerequisites**:
- Both WebAPI and WebFE running
- Logged in as Admin
- Navigate to `/Admin/MovementReports`

---

## **TEST 2: Modal Opens**

**Step 1**: Open `/Admin/MovementReports`

**Step 2**: Locate and click **[➕ Cộng Điểm]** button (top right, yellow)

**Expected**: 
- ✅ Bootstrap modal appears with title "Cộng Điểm Phong Trào"
- ✅ Modal contains 6 fields

**Verification Checklist**:
```
Form Fields:
☐ Student Select (dropdown - currently empty)
☐ Category Select (dropdown - 4 options)
☐ Behavior Select (dropdown - currently disabled)
☐ Score Input (number field)
☐ Date Picker (date field)
☐ Comments TextArea (required)

Buttons:
☐ [Cancel] button (closes modal)
☐ [Xác nhận] button (submit form)
```

---

## **TEST 3: Student Dropdown Population**

**Step 1**: Modal is open

**Expected**:
- ✅ Within 1-2 seconds, student dropdown loads
- ✅ Shows "Họ Tên (Mã SV)" format

**Verification**:
```javascript
// Open browser dev console to verify API call
// Network tab shows: GET /api/students → 200 OK

// Student dropdown shows examples:
- Nguyễn Văn A (SE160001)
- Trần Thị B (SE160002)
- ...
```

**Step 2**: Click on Student dropdown

**Expected**:
- ✅ Can see 10+ students
- ✅ List is sorted alphabetically
- ✅ Can scroll if needed

---

## **TEST 4: Category Change → Behavior Auto-Load**

**Step 1**: Select Student (any one)

**Step 2**: Click Category dropdown

**Expected Options**:
```
1. Ý thức học tập
2. Hoạt động chính trị
3. Phẩm chất công dân
4. Công tác phụ trách
```

**Step 3**: Select **Category 1**

**Expected**:
- ✅ Behavior Select becomes ENABLED
- ✅ Behavior dropdown populates with:
  - Tuyên dương công khai (2 điểm)
  - Olympic/ACM/CPC/Robocon (10 điểm)
  - Cuộc thi cấp Trường (5 điểm)

**Step 4**: Select **Category 2** (Change)

**Expected**:
- ✅ Behavior dropdown updates with Category 2 behaviors:
  - Sự kiện nhỏ (3 điểm)
  - Sự kiện trung (5 điểm)
  - CLB sinh hoạt (5-10 điểm)
  - Sự kiện vừa (15 điểm)
  - Sự kiện lớn (20 điểm)

---

## **TEST 5: Score Validation**

**Step 1**: Select:
- Student: Any
- Category: 1
- Behavior: "Tuyên dương công khai" (Max 2 điểm)

**Step 2**: Enter Score = **5** (exceeds max of 2)

**Step 3**: Click **[Xác nhận]**

**Expected**:
- ✅ Alert: "⚠️ Điểm không được vượt quá 2"
- ✅ Form does NOT submit

---

## **TEST 6: Comments Required Validation**

**Step 1**: Fill form:
- Student: Any
- Category: 1
- Behavior: Tuyên dương (2 điểm)
- Score: 2
- Date: 17/10/2025
- Comments: **[LEAVE EMPTY]**

**Step 2**: Click **[Xác nhận]**

**Expected**:
- ✅ Alert: "⚠️ Comments không được trống"
- ✅ Form does NOT submit

---

## **TEST 7: Successful Score Submission**

**Step 1**: Select student without score yet

**Step 2**: Fill complete form:
```
Student: "Nguyễn Văn A (SE160001)"
Category: "1. Ý thức học tập"
Behavior: "Tuyên dương công khai (2 điểm)"
Score: 2
Date: 17/10/2025
Comments: "Tuyên dương xuất sắc"
```

**Step 3**: Click **[Xác nhận]**

**Expected**:
- ✅ Show loading spinner
- ✅ Alert: "✅ Đã cộng điểm thành công!"
- ✅ Modal closes automatically
- ✅ Page reloads
- ✅ Nguyễn Văn A now shows +2 in Cat 1

**Verification in Table**:
```
Student: Nguyễn Văn A (SE160001)
Cat 1: 2
Cat 2: 0
Cat 3: 0
Cat 4: 0
Total: 2
```

---

# **PART 3: AUTO-CAP TESTING**

## **TEST 8: Category 1 Cap (Max 35)**

**Step 1**: Find student with Category 1 = 0

**Step 2**: Add 20 points (Behavior: value of 20)
- Score becomes: 20

**Step 3**: Add 20 points again (same behavior)
- Score becomes: 40 → **AUTO-CAPPED TO 35** ✅

**Step 4**: Check student details

**Expected**:
```
Cat 1 Total: 35 (not 40)
Message/Log: "Điều chỉnh vì danh mục 1 vượt max (35)"
```

---

## **TEST 9: Category 2 Cap (Max 50)**

**Step 1**: Student with Cat 2 = 30

**Step 2**: Add 20 points → Total becomes 50 ✅

**Step 3**: Add 20 points → Total should be **50** (not 70) ✅

---

## **TEST 10: Total Cap (Max 140)**

**Step 1**: Find student or create one

**Step 2**: Add scores:
- Cat 1: 35 ✅ (capped)
- Cat 2: 50 ✅ (capped)
- Cat 3: 25 ✅ (capped)
- Cat 4: 30 ✅ (capped)
- **Total: 140** ✅

**Step 3**: Try adding one more point to any category

**Expected**:
- ✅ Total remains 140
- ✅ Other categories don't increase
- ✅ System prevents exceeding 140

---

## **TEST 11: Min Score Rule (≥ 60)**

**Step 1**: Create new student (Total = 0)

**Step 2**: Add 1 point

**Step 3**: Check student record

**Expected**:
- ✅ If Total < 60, score is **NOT saved** (or set to 0)
- ✅ Only when Total ≥ 60 does it count

---

# **PART 4: CLUB AUTO-SCORING (BACKGROUND JOB)**

## **TEST 12: Background Job Runs**

### **Setup**:
1. Create test data:
   - Club X (Active)
   - Student A (President of Club X)
   - Student B (Member of Club X)

2. Wait or manually trigger background job

### **Manual Trigger** (if needed):
```bash
# Restart the WebAPI to trigger background job
# Or call a trigger endpoint if available
```

### **Expected Result** (after 6 hours or manual trigger):

**For Student A (President)**:
```
Movement Record gets:
- New detail added
- Category 2: +10 points (role: President)
- AwardedDate: Current date
- Notes: "Club member scoring - Club X - President"
```

**For Student B (Member)**:
```
Movement Record gets:
- New detail added
- Category 2: +3 points (role: Member)
- AwardedDate: Current date
- Notes: "Club member scoring - Club X - Member"
```

---

## **TEST 13: Role-Based Scoring**

### **Test Different Roles**:

```
President → +10
VicePresident → +8
Manager → +5
Member → +3
Other → +1
```

### **Verification**:
- ✅ Each role gets correct points
- ✅ Points go to Category 2
- ✅ Capped at 50 total for Category 2

---

## **TEST 14: Duplicate Prevention (Monthly)**

### **Scenario**:
1. Background job runs on 17/10/2025 → Student A +10 points

2. Manual trigger background job again same day

### **Expected**:
- ✅ Student A does **NOT** get +10 again
- ✅ System checks: AwardedAt.Month == Current Month
- ✅ Log message: "Club score already added this month"

---

## **TEST 15: Multiple Clubs (Cộng Dồn)**

### **Setup**:
- Student C is:
  - President of Club X (+10)
  - Manager of Club Y (+5)
  - Member of Club Z (+3)

### **Expected After Background Job**:
```
Category 2 (Club Scoring):
10 + 5 + 3 = 18 points (cộng dồn)

Total: 18 points < 50 (no cap needed)
```

---

# **PART 5: DASHBOARD INTEGRATION**

## **TEST 16: Dashboard Shows Scoring Section**

**Step 1**: Navigate to `/Admin/Dashboard`

**Step 2**: Scroll down past statistics

**Expected**:
- ✅ New section: **"Scoring Management"**
- ✅ 3 cards visible:
  1. Manual Scoring (Vàng - Warning)
  2. Club Member Scoring (Xanh - Success)
  3. Score Validation (Xanh dương - Info)

---

## **TEST 17: Dashboard Cards Content**

### **Card 1: Manual Scoring**
```
Title: ✋ Manual Scoring
Subtitle: Cộng điểm trực tiếp cho SV

Content:
✓ Cộng điểm thủ công cho SV
✓ Chọn danh mục & hành vi
✓ Tự động điều chỉnh QĐ 414

Button: [➕ Cộng Điểm]
```

**Verification**: ✅ Click button → goes to `/Admin/MovementReports`

---

### **Card 2: Club Member Scoring**
```
Title: 👥 Club Member Scoring
Subtitle: Tự động hàng tháng

Content:
✓ Tự động chạy mỗi 6 giờ
✓ Tính điểm theo vai trò CLB
✓ Cộng dồn, cap 50 điểm

Status: [⚡ Status: Active]
```

**Verification**: ✅ Shows "Active" status

---

### **Card 3: Score Validation**
```
Title: ✅ Score Validation
Subtitle: Quy tắc QĐ 414

Content:
Giới hạn điểm:
✓ Cat 1: ≤35 | Cat 2: ≤50
✓ Cat 3: ≤25 | Cat 4: ≤30
✓ Tổng: 60-140 điểm

Button: [⚙️ View Criteria]
```

**Verification**: ✅ Click button → goes to `/Admin/Criteria`

---

## **TEST 18: Quick Action Button**

**Step 1**: Dashboard → "Quick Actions" section

**Step 2**: Scroll to see last action button

**Expected**:
- ✅ New button: **"➕ Add Score"**
- ✅ Yellow gradient color
- ✅ Description: "Cộng điểm cho sinh viên"

**Step 3**: Click button

**Expected**:
- ✅ Navigates to `/Admin/MovementReports`
- ✅ Modal opens automatically

---

# **PART 6: END-TO-END WORKFLOW**

## **TEST 19: Complete Admin Workflow**

### **Scenario**: Admin wants to score a student for 4 categories

**Step 1**: Dashboard → Click **[Add Score]** or **[Cộng Điểm]**

**Step 2**: Score Category 1 (Ý thức học tập)
```
Student: Nguyễn Văn A
Category: 1
Behavior: Olympic (10 điểm)
Score: 10
Date: 17/10/2025
Comments: "Tham gia Olympic Tin học"
→ [Xác nhận]
```

**Result**: ✅ +10 points to Category 1

**Step 3**: Score Category 2 (Hoạt động chính trị)
```
Student: Nguyễn Văn A
Category: 2
Behavior: Sự kiện lớn (20 điểm)
Score: 20
Date: 17/10/2025
Comments: "MC sự kiện Gala 100 người"
→ [Xác nhận]
```

**Result**: ✅ +20 points to Category 2

**Step 4**: Score Category 3 (Phẩm chất công dân)
```
Student: Nguyễn Văn A
Category: 3
Behavior: Tình nguyện (5 điểm)
Score: 5
Date: 17/10/2025
Comments: "Tình nguyện donation"
→ [Xác nhận]
```

**Result**: ✅ +5 points to Category 3

**Step 5**: Score Category 4 (Công tác phụ trách)
```
Student: Nguyễn Văn A
Category: 4
Behavior: Lớp trưởng (10 điểm)
Score: 10
Date: 17/10/2025
Comments: "Lớp trưởng xuất sắc"
→ [Xác nhận]
```

**Result**: ✅ +10 points to Category 4

**Step 6**: Check student record in `/Admin/MovementReports`

**Expected**:
```
Student: Nguyễn Văn A (SE160001)
Category 1: 10
Category 2: 20
Category 3: 5
Category 4: 10
Total Score: 45
Status: Khá (45 < 60, but close)

All 4 details with correct notes
```

---

# **TEST SUMMARY CHECKLIST**

```
TIER 1: API Endpoints
☐ GET /api/students (200 OK, list students)
☐ GET /api/students (401 without token)
☐ GET /api/students (403 non-Admin)
☐ GET /api/students/{id} (200 with details)
☐ GET /api/students/{id} (404 invalid)
☐ GET /api/students/search (200 results)
☐ GET /api/students/search (400 empty query)

TIER 2: Modal & UI
☐ Modal opens on [➕ Cộng Điểm]
☐ Student dropdown loads from API
☐ Category → Behavior dynamic load
☐ Score validation (max per behavior)
☐ Comments required
☐ Successful submission

TIER 3: Auto-Capping
☐ Cat 1 cap 35
☐ Cat 2 cap 50
☐ Cat 3 cap 25
☐ Cat 4 cap 30
☐ Total cap 140
☐ Min 60 rule

TIER 4: Club Auto-Scoring
☐ Background job runs (6h)
☐ President scores +10
☐ Member scores +3
☐ Duplicate prevention (monthly)
☐ Multiple clubs cộng dồn

TIER 5: Dashboard
☐ Scoring section visible
☐ 3 cards display correctly
☐ Quick action button
☐ Navigation links work

TIER 6: End-to-End
☐ Complete workflow 4 categories
☐ Stats update correctly
☐ Auto-cap applies
```

---

## 🎯 **QUICK START TEST (5 minutes)**

```bash
# 1. Test API
curl -X GET "http://localhost:5000/api/students" \
  -H "Authorization: Bearer YOUR_TOKEN"

# 2. Open /Admin/Dashboard
# 3. Check Scoring Management section visible
# 4. Click [Add Score]
# 5. Try submitting a score
```

---

**Status**: COMPLETE TEST GUIDE ✅  
**Version**: 2.0  
**Last Updated**: 17/10/2025
