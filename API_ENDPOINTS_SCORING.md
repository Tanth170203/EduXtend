# 📡 API ENDPOINTS - SCORING FEATURES

## **Created: 17/10/2025**

Tất cả các endpoint được bảo vệ bằng JWT Token + Role Authorization (Admin)

---

## 🎯 **ENDPOINT 1: GET ALL ACTIVE STUDENTS**

### **Endpoint**: `GET /api/students`

**Description**: Lấy danh sách tất cả sinh viên Active để populate dropdown trong scoring modal

**Authorization**: `Admin` role required

**Response Status**: `200 OK`

**Response Body**:
```json
[
  {
    "id": 1,
    "studentCode": "SE160001",
    "fullName": "Nguyễn Văn A",
    "email": "SE160001@fpt.edu.vn",
    "cohort": "K16"
  },
  {
    "id": 2,
    "studentCode": "SE160002",
    "fullName": "Trần Thị B",
    "email": "SE160002@fpt.edu.vn",
    "cohort": "K16"
  },
  ...
]
```

**cURL Example**:
```bash
curl -X GET "http://localhost:5000/api/students" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**JavaScript/Fetch Example**:
```javascript
const loadStudents = async () => {
  try {
    const response = await fetch('/api/students', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Content-Type': 'application/json'
      }
    });
    
    if (!response.ok) throw new Error('Failed to load students');
    
    const students = await response.json();
    populateStudentDropdown(students);
  } catch (error) {
    console.error('Error loading students:', error);
  }
};
```

---

## 🎯 **ENDPOINT 2: GET STUDENT BY ID**

### **Endpoint**: `GET /api/students/{id}`

**Description**: Lấy thông tin chi tiết sinh viên (dùng khi xem hoặc edit score)

**Authorization**: `Admin` role required

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| id | int | Student ID |

**Response Status**: 
- `200 OK` - Student found
- `404 Not Found` - Student not found

**Response Body** (200):
```json
{
  "id": 1,
  "studentCode": "SE160001",
  "fullName": "Nguyễn Văn A",
  "email": "SE160001@fpt.edu.vn",
  "cohort": "K16",
  "phone": "0912345678",
  "dateOfBirth": "2002-01-15",
  "gender": "Male",
  "status": "Active"
}
```

**Response Body** (404):
```json
{
  "message": "Student with ID 999 not found."
}
```

**cURL Example**:
```bash
curl -X GET "http://localhost:5000/api/students/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 🎯 **ENDPOINT 3: SEARCH STUDENTS**

### **Endpoint**: `GET /api/students/search?query={query}`

**Description**: Tìm kiếm sinh viên theo tên, mã sinh viên hoặc email (dùng cho search box trong scoring modal)

**Authorization**: `Admin` role required

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| query | string | Yes | Search keyword (name/code/email) |

**Response Status**: 
- `200 OK` - Search successful
- `400 Bad Request` - Empty query

**Response Body** (200):
```json
[
  {
    "id": 1,
    "studentCode": "SE160001",
    "fullName": "Nguyễn Văn A",
    "email": "SE160001@fpt.edu.vn",
    "cohort": "K16"
  },
  {
    "id": 3,
    "studentCode": "SE160003",
    "fullName": "Nguyễn Văn C",
    "email": "SE160003@fpt.edu.vn",
    "cohort": "K16"
  }
]
```

**Response Body** (400):
```json
{
  "message": "Search query is required."
}
```

**cURL Example**:
```bash
curl -X GET "http://localhost:5000/api/students/search?query=Nguyễn" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**JavaScript Example** (for live search):
```javascript
const searchStudents = async (query) => {
  if (!query || query.trim().length < 2) return;
  
  try {
    const response = await fetch(`/api/students/search?query=${encodeURIComponent(query)}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${getToken()}`,
        'Content-Type': 'application/json'
      }
    });
    
    const results = await response.json();
    updateSearchResults(results);
  } catch (error) {
    console.error('Search failed:', error);
  }
};
```

---

## 📍 **EXISTING ENDPOINTS (FOR SCORING)**

### **ADD SCORE TO MOVEMENT RECORD**

**Endpoint**: `POST /api/movement-records/add-score`

**Description**: Thêm điểm cho sinh viên (gọi từ manual scoring modal)

**Authorization**: `Admin` role required

**Request Body**:
```json
{
  "studentId": 1,
  "categoryId": 1,
  "score": 10,
  "awardedDate": "2025-10-17",
  "notes": "Tuyên dương xuất sắc",
  "semesterId": 5
}
```

**Response** (201):
```json
{
  "id": 123,
  "studentId": 1,
  "semesterId": 5,
  "totalScore": 45,
  "createdAt": "2025-10-17T10:30:00"
}
```

---

## 📋 **INTEGRATION IN SCORING MODAL**

### **HTML Form** (Index.cshtml):
```html
<select id="studentSelect" class="form-control" required>
  <option value="">-- Chọn sinh viên --</option>
  <!-- Populated by API -->
</select>
```

### **JavaScript Load Students**:
```javascript
async function loadStudentsForScoring() {
  try {
    const response = await fetch('/api/students', {
      headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    
    const students = await response.json();
    const select = document.getElementById('studentSelect');
    
    students.forEach(student => {
      const option = document.createElement('option');
      option.value = student.id;
      option.textContent = `${student.fullName} (${student.studentCode})`;
      select.appendChild(option);
    });
  } catch (error) {
    console.error('Error loading students:', error);
    alert('Lỗi: Không thể tải danh sách sinh viên');
  }
}
```

---

## ✅ **TESTING CHECKLIST**

### **Test GET /api/students**
```
1. ✅ Call endpoint without token → 401 Unauthorized
2. ✅ Call with non-Admin token → 403 Forbidden
3. ✅ Call with Admin token → 200 OK + list of students
4. ✅ Response contains at least 5 fields (id, studentCode, fullName, email, cohort)
5. ✅ Students sorted by fullName
6. ✅ Only Active students returned
```

### **Test GET /api/students/{id}**
```
1. ✅ Call with valid ID → 200 OK + student details
2. ✅ Call with invalid ID → 404 Not Found
3. ✅ Response contains all 8 fields
4. ✅ DateOfBirth in correct format
5. ✅ Gender and Status as string
```

### **Test GET /api/students/search**
```
1. ✅ Call without query param → 400 Bad Request
2. ✅ Search by fullName → returns matching students
3. ✅ Search by studentCode → returns matching students
4. ✅ Search by email → returns matching students
5. ✅ Case-insensitive search
6. ✅ Partial match works (e.g., "Nguyễn" finds "Nguyễn Văn A")
7. ✅ Only Active students returned
```

### **Test Modal Integration**
```
1. ✅ Click [➕ Cộng Điểm] → Modal opens
2. ✅ Student dropdown auto-populates from /api/students
3. ✅ Student dropdown shows "Họ Tên (Mã SV)" format
4. ✅ Can select any student from list
5. ✅ Search box can filter students
```

---

## 🔧 **TROUBLESHOOTING**

### **Issue: 401 Unauthorized**
**Solution**: Ensure JWT token is in request header:
```javascript
headers: {
  'Authorization': `Bearer ${token}`
}
```

### **Issue: 403 Forbidden**
**Solution**: User account must have "Admin" role

### **Issue: Empty student list**
**Solution**: Check database - there may be no Active students

### **Issue: CORS error**
**Solution**: Ensure WebFE and WebAPI can communicate. Check CORS configuration in Program.cs

---

## 📊 **API BASE URL**

**Development**: `http://localhost:5000`  
**Production**: `https://yourdomain.com`

---

## 🔐 **Authentication**

All endpoints require:
```
Authorization: Bearer <JWT_TOKEN>
```

Token is typically stored in cookie or localStorage after login.

---

## 📝 **FILE LOCATIONS**

- **Controller**: `EduXtend/WebAPI/Controllers/StudentController.cs`
- **Repository**: `EduXtend/Repositories/Students/StudentRepository.cs`
- **Interface**: `EduXtend/Repositories/Students/IStudentRepository.cs`
- **DTOs**: Defined in StudentController.cs

---

**Status**: ENDPOINTS CREATED ✅  
**Version**: 1.0  
**Last Updated**: 17/10/2025
