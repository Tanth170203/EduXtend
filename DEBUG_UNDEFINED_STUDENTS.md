# 🐛 **DEBUG GUIDE - UNDEFINED STUDENT NAMES**

## **Issue**: Dropdown shows "undefined (SE170504)" instead of student names

---

## 🔍 **STEP 1: CHECK DATABASE**

### **Verify Student Data in Database**

```sql
-- Check if students have proper FullName
SELECT TOP 20 
    Id,
    StudentCode,
    FullName,
    Cohort,
    Status
FROM Students
ORDER BY FullName;

-- Check for NULL or empty FullName
SELECT TOP 20 
    Id,
    StudentCode,
    FullName,
    LEN(FullName) as NameLength
FROM Students
WHERE FullName IS NULL OR FullName = '' OR FullName = 'undefined'
ORDER BY Id;
```

**Expected Result**:
```
Id | StudentCode | FullName         | NameLength
---|-------------|------------------|----------
1  | SE170001    | Nguyễn Văn A     | 13
2  | SE170002    | Trần Thị B       | 12
3  | SE170003    | Phạm Minh C      | 12
```

**Problem**: If you see `FullName = 'undefined'` or NULL, that's the issue.

---

## 🔍 **STEP 2: CHECK API RESPONSE**

### **Test /api/students Endpoint**

**Using cURL**:
```bash
curl -X GET "https://localhost:5001/api/students" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k
```

**Expected Response**:
```json
[
  {
    "id": 1,
    "studentCode": "SE170001",
    "fullName": "Nguyễn Văn A",
    "email": "nguyenvana@fpt.edu.vn",
    "cohort": "K17"
  },
  {
    "id": 2,
    "studentCode": "SE170002",
    "fullName": "Trần Thị B",
    "email": "tranthib@fpt.edu.vn",
    "cohort": "K17"
  }
]
```

**Problem Signs**:
- ❌ `"fullName": null`
- ❌ `"fullName": "undefined"`
- ❌ `"fullName": ""`
- ❌ Missing `fullName` field entirely

### **Using Browser Console**

1. Open `/Admin/MovementReports`
2. Press `F12` to open Developer Tools
3. Go to **Console** tab
4. Paste:
```javascript
fetch('https://localhost:5001/api/students', {
    method: 'GET',
    credentials: 'include'
})
.then(r => r.json())
.then(data => {
    console.log('API Response:', data);
    console.log('First student:', data[0]);
    console.log('Names:', data.map(s => s.fullName));
});
```

5. Check console output for `undefined` values

---

## 🔍 **STEP 3: CHECK FRONTEND JAVASCRIPT**

### **Open Browser Console (F12)**

1. Go to `/Admin/MovementReports`
2. Click **[➕ Cộng Điểm]** button
3. Open **DevTools Console** (F12)
4. Look for these logs:

```javascript
// Expected logs:
✅ Students loaded from API: [Array(5)]  // Should show students
✅ Student object: {id: 1, fullName: "Nguyễn Văn A", ...}
✅ Loaded 5 students

// Problem logs:
❌ Students loaded from API: [Array(5)]
❌ Student object: {id: 1, fullName: undefined, ...}
❌ Warning: Student 1 has invalid name: "undefined"
```

---

## ✅ **SOLUTIONS**

### **Solution 1: Check Student Table for Bad Data**

```sql
-- Find all students with undefined/null names
SELECT Id, StudentCode, FullName, Status
FROM Students
WHERE FullName IS NULL OR FullName = '' OR FullName LIKE '%undefined%';

-- If found, update them:
UPDATE Students
SET FullName = 'Sinh viên cần cập nhật'
WHERE FullName IS NULL OR FullName = '' OR FullName LIKE '%undefined%';
```

---

### **Solution 2: Re-import Student Data**

If many students have invalid names:

1. **Prepare correct student data** (Excel file with columns):
   - Email
   - Full Name
   - Phone Number (optional)
   - Roles (e.g., "Student")
   - Student Code (required)
   - Cohort (e.g., "K17")
   - Gender (Male/Female/Other)
   - Major Code (e.g., "SE")

2. **Go to Admin Panel** → **User Management** → **Import Users**

3. **Upload Excel file** with correct student names

---

### **Solution 3: Check StudentController**

```csharp
// File: EduXtend/WebAPI/Controllers/StudentController.cs

// The GetAllActive method should return students with FullName
var dtos = students.Select(s => new StudentDropdownDto
{
    Id = s.Id,
    StudentCode = s.StudentCode,
    FullName = s.FullName,  // ✅ Must not be null
    Email = s.User?.Email ?? "",
    Cohort = s.Cohort
}).ToList();
```

**Check if**:
- ✅ `s.FullName` is being included
- ✅ `s.FullName` is not null in database
- ✅ StudentDropdownDto.FullName has correct value

---

### **Solution 4: Debug API with Postman**

1. Open Postman
2. **GET** `https://localhost:5001/api/students`
3. Add header: `Authorization: Bearer YOUR_TOKEN`
4. Set SSL Verification: OFF (dev mode)
5. **Send**
6. Check Response JSON for `fullName` values

**If empty or null**:
```json
❌ {
  "id": 1,
  "studentCode": "SE170001",
  "fullName": null,  // PROBLEM!
  "email": "...",
  "cohort": "K17"
}
```

**If correct**:
```json
✅ {
  "id": 1,
  "studentCode": "SE170001",
  "fullName": "Nguyễn Văn A",  // GOOD!
  "email": "...",
  "cohort": "K17"
}
```

---

## 📋 **DEBUG CHECKLIST**

```
Database Level:
☐ SELECT * FROM Students → Check FullName is not NULL
☐ Check for any rows with FullName = 'undefined'
☐ Check for empty FullName values

API Level:
☐ GET /api/students returns JSON with fullName field
☐ fullName values are not null or "undefined"
☐ studentCode field exists
☐ Response status is 200 OK

Frontend Level:
☐ Browser console shows "Students loaded from API"
☐ No warnings about invalid names
☐ Student objects have fullName property
☐ Dropdown options display correctly
```

---

## 🔧 **QUICK FIXES**

### **Fix 1: Refresh Database Cache**

```bash
# Restart WebAPI to clear any in-memory cache
# Then try loading students again
```

### **Fix 2: Rebuild DTO**

If StudentDropdownDto doesn't match Student model:

```csharp
// StudentController.cs - Ensure correct mapping
var dtos = students.Select(s => new StudentDropdownDto
{
    Id = s.Id,
    StudentCode = s.StudentCode?.Trim() ?? "N/A",
    FullName = (s.FullName?.Trim() ?? "Unknown").Equals("undefined") 
        ? "Chưa cập nhật" 
        : s.FullName?.Trim() ?? "Unknown",
    Email = s.User?.Email ?? "",
    Cohort = s.Cohort ?? ""
}).ToList();
```

### **Fix 3: Handle Undefined in Frontend**

Already done in updated JavaScript:
```javascript
const name = student.fullName?.trim() || 
            student.FullName?.trim() || 
            'Không rõ tên';
```

---

## 🚀 **TESTING AFTER FIX**

1. **Check Database**:
   ```sql
   SELECT * FROM Students WHERE Status = 'Active'
   ```

2. **Test API**:
   ```bash
   curl https://localhost:5001/api/students -k
   ```

3. **Test UI**:
   - Open `/Admin/MovementReports`
   - Click **[➕ Cộng Điểm]**
   - Dropdown should show: `Nguyễn Văn A (SE170001)`
   - Not: `undefined (SE170001)`

---

## 📝 **FILES INVOLVED**

- `EduXtend/WebAPI/Controllers/StudentController.cs` - API response mapping
- `EduXtend/WebFE/Pages/Admin/MovementReports/Index.cshtml` - Frontend display
- `EduXtend/Repositories/Students/StudentRepository.cs` - Database query
- Database: `Students` table → `FullName` column

---

**Status**: DEBUG GUIDE READY ✅  
**Next**: Follow steps above to identify the root cause
