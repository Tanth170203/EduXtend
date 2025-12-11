# ✅ IMPORT STUDENTS FEATURE ADDED

## 📋 Tổng quan

Đã thêm chức năng **Import Students** vào trang **Admin/Students/Index** để cho phép Admin import hàng loạt sinh viên từ file Excel.

---

## 🎯 Các thay đổi đã thực hiện

### 1. **Thêm nút Import Students** (Header)

```razor
<button type="button" class="btn btn-success d-flex align-items-center gap-2" 
        data-bs-toggle="modal" data-bs-target="#importModal">
    <i data-lucide="upload" style="width: 16px; height: 16px;"></i>
    Import Students
</button>
```

**Vị trí:** Bên cạnh nút "Add Student Info" ở header trang

---

### 2. **Thêm Import Modal** (UI)

Modal bao gồm:

#### **Step 1: Download Template**
- Nút download template Excel
- Link: `/Admin/Students/DownloadTemplate`

#### **Step 2: Upload File**
- Input file chấp nhận `.xlsx`, `.xls`
- Giới hạn: 5MB
- Progress bar hiển thị tiến trình upload
- Khu vực hiển thị kết quả import

#### **Hướng dẫn sử dụng:**
```
- Download the template Excel file below
- Fill in student information (Student Code, Full Name, Email, Cohort, Major Code, Status)
- Upload the completed file
- Status: 0 = Active, 1 = Inactive, 2 = Graduated, 3 = Suspended
```

---

### 3. **JavaScript Function: uploadImportFile()**

**Chức năng:**
- Validate file (type, size)
- Upload file qua API: `POST /api/students/import`
- Hiển thị progress bar
- Hiển thị kết quả import (success/error)
- Auto reload trang sau 3 giây nếu import thành công

**Validation:**
```javascript
// File size: Max 5MB
if (file.size > 5 * 1024 * 1024) {
    alert('File size exceeds 5MB limit');
    return;
}

// File type: .xlsx, .xls only
const allowedTypes = [
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-excel'
];
```

**API Call:**
```javascript
const response = await fetch('/api/students/import', {
    method: 'POST',
    headers: {
        'Authorization': `Bearer ${token}`
    },
    body: formData
});
```

**Response Format Expected:**
```json
{
    "totalProcessed": 100,
    "successCount": 95,
    "failedCount": 5,
    "errors": [
        "Row 10: Invalid email format",
        "Row 25: Student code already exists"
    ]
}
```

---

## 🔧 Backend Requirements (Cần implement)

### 1. **API Endpoint: Download Template**

**Route:** `GET /Admin/Students/DownloadTemplate`

**Chức năng:**
- Tạo file Excel template với các cột:
  - Student Code (required)
  - Full Name (required)
  - Email (required)
  - Cohort (required)
  - Major Code (required)
  - Status (0/1/2/3)

**Example Implementation:**
```csharp
public async Task<IActionResult> OnGetDownloadTemplate()
{
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Students");
    
    // Headers
    worksheet.Cell(1, 1).Value = "Student Code";
    worksheet.Cell(1, 2).Value = "Full Name";
    worksheet.Cell(1, 3).Value = "Email";
    worksheet.Cell(1, 4).Value = "Cohort";
    worksheet.Cell(1, 5).Value = "Major Code";
    worksheet.Cell(1, 6).Value = "Status";
    
    // Example data
    worksheet.Cell(2, 1).Value = "SE123456";
    worksheet.Cell(2, 2).Value = "Nguyen Van A";
    worksheet.Cell(2, 3).Value = "anvn@fpt.edu.vn";
    worksheet.Cell(2, 4).Value = "K16";
    worksheet.Cell(2, 5).Value = "SE";
    worksheet.Cell(2, 6).Value = "0";
    
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var content = stream.ToArray();
    
    return File(content, 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "Student_Import_Template.xlsx");
}
```

---

### 2. **API Endpoint: Import Students**

**Route:** `POST /api/students/import`

**Request:**
- Content-Type: `multipart/form-data`
- Body: Excel file

**Response:**
```json
{
    "totalProcessed": 100,
    "successCount": 95,
    "failedCount": 5,
    "errors": [
        "Row 10: Invalid email format",
        "Row 25: Student code already exists"
    ]
}
```

**Example Implementation:**
```csharp
[HttpPost("import")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ImportStudents(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "No file uploaded" });

    if (file.Length > 5 * 1024 * 1024)
        return BadRequest(new { message = "File size exceeds 5MB" });

    var result = new {
        totalProcessed = 0,
        successCount = 0,
        failedCount = 0,
        errors = new List<string>()
    };

    try
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        
        var rows = worksheet.RowsUsed().Skip(1); // Skip header
        result.totalProcessed = rows.Count();
        
        foreach (var row in rows)
        {
            try
            {
                var studentCode = row.Cell(1).GetString();
                var fullName = row.Cell(2).GetString();
                var email = row.Cell(3).GetString();
                var cohort = row.Cell(4).GetString();
                var majorCode = row.Cell(5).GetString();
                var status = row.Cell(6).GetValue<int>();
                
                // Validate
                if (string.IsNullOrWhiteSpace(studentCode))
                {
                    result.errors.Add($"Row {row.RowNumber()}: Student code is required");
                    result.failedCount++;
                    continue;
                }
                
                // Check if student exists
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == studentCode);
                    
                if (existingStudent != null)
                {
                    result.errors.Add($"Row {row.RowNumber()}: Student code {studentCode} already exists");
                    result.failedCount++;
                    continue;
                }
                
                // Get major
                var major = await _context.Majors
                    .FirstOrDefaultAsync(m => m.Code == majorCode);
                    
                if (major == null)
                {
                    result.errors.Add($"Row {row.RowNumber()}: Major code {majorCode} not found");
                    result.failedCount++;
                    continue;
                }
                
                // Create user first (if not exists)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                    
                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        FullName = fullName,
                        RoleId = 3, // Student role
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                
                // Create student
                var student = new Student
                {
                    UserId = user.Id,
                    StudentCode = studentCode,
                    Cohort = cohort,
                    MajorId = major.Id,
                    Status = status
                };
                
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                
                result.successCount++;
            }
            catch (Exception ex)
            {
                result.errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                result.failedCount++;
            }
        }
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error importing students");
        return StatusCode(500, new { message = "Error processing file" });
    }
}
```

---

## 📦 Required NuGet Packages

Để xử lý Excel files, cần cài đặt:

```bash
dotnet add package ClosedXML
```

Hoặc thêm vào `.csproj`:
```xml
<PackageReference Include="ClosedXML" Version="0.102.1" />
```

---

## 🎨 UI Features

### **Progress Bar**
- Hiển thị khi đang upload
- Animated striped progress bar
- Tự động ẩn sau khi hoàn thành

### **Result Display**
- **Success:** Alert màu xanh với thống kê
  - Total processed
  - Success count
  - Failed count
  - Expandable error list
- **Error:** Alert màu đỏ với thông báo lỗi

### **Auto Reload**
- Tự động reload trang sau 3 giây nếu import thành công
- Giúp cập nhật danh sách sinh viên mới

---

## 🔒 Security

- **Authentication:** Yêu cầu Bearer token
- **Authorization:** Chỉ Admin mới có quyền import
- **File Validation:**
  - Type: Chỉ chấp nhận .xlsx, .xls
  - Size: Giới hạn 5MB
- **Data Validation:**
  - Required fields
  - Email format
  - Duplicate check
  - Foreign key validation (Major)

---

## 📝 Template Excel Format

| Student Code | Full Name      | Email              | Cohort | Major Code | Status |
|--------------|----------------|--------------------|--------|------------|--------|
| SE123456     | Nguyen Van A   | anvn@fpt.edu.vn    | K16    | SE         | 0      |
| SE123457     | Tran Thi B     | btt@fpt.edu.vn     | K16    | SE         | 0      |
| SE123458     | Le Van C       | clv@fpt.edu.vn     | K17    | IA         | 0      |

**Status Values:**
- `0` = Active
- `1` = Inactive
- `2` = Graduated
- `3` = Suspended

---

## ✅ Testing Checklist

- [ ] Download template works
- [ ] Upload valid Excel file
- [ ] Upload invalid file type (rejected)
- [ ] Upload file > 5MB (rejected)
- [ ] Import with all valid data
- [ ] Import with some invalid rows (partial success)
- [ ] Import with duplicate student codes (error)
- [ ] Import with invalid major codes (error)
- [ ] Progress bar displays correctly
- [ ] Result message displays correctly
- [ ] Page reloads after successful import
- [ ] Modal resets when closed

---

## 🚀 Next Steps

1. **Implement Backend:**
   - Create `DownloadTemplate` handler
   - Create `POST /api/students/import` endpoint
   - Add validation logic
   - Add error handling

2. **Install Dependencies:**
   - Add ClosedXML package

3. **Test:**
   - Test with various Excel files
   - Test error scenarios
   - Test with large files

4. **Optional Enhancements:**
   - Add preview before import
   - Support CSV format
   - Add import history/logs
   - Email notification after import
   - Bulk update existing students

---

## 📞 Support

Nếu cần hỗ trợ implement backend hoặc có vấn đề gì, vui lòng liên hệ team development.
