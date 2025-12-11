# ✅ IMPORT STUDENTS - INTEGRATION GUIDE

## 📋 Tổng quan

Hệ thống **ĐÃ CÓ** service `UserImportService` để import students từ file Excel. Tôi đã thêm nút "Import Students" vào trang Admin/Students/Index.

---

## ✅ Đã hoàn thành

### 1. **Thêm nút Import Students**
- ✅ Vị trí: `/Admin/Students/Index`
- ✅ URL: `/Admin/Students/Import`
- ✅ Icon: Upload (màu xanh lá)

```razor
<a href="/Admin/Students/Import" class="btn btn-success d-flex align-items-center gap-2">
    <i data-lucide="upload" style="width: 16px; height: 16px;"></i>
    Import Students
</a>
```

---

## 🔧 Service đã có sẵn

### **UserImportService**
- ✅ File: `Services/UserImport/UserImportService.cs`
- ✅ Interface: `Services/UserImport/IUserImportService.cs`
- ✅ Method: `ImportUsersFromExcelAsync(IFormFile file)`

### **Chức năng:**
1. Đọc file Excel (.xlsx, .xls)
2. Validate dữ liệu
3. Tạo User và Student records
4. Xử lý errors và trả về kết quả chi tiết

### **Excel Format Expected:**

| Column | Field | Required | Description |
|--------|-------|----------|-------------|
| 1 | Email | ✅ | Email của user |
| 2 | Full Name | ✅ | Họ tên đầy đủ |
| 3 | Phone Number | ❌ | Số điện thoại |
| 4 | Roles | ❌ | Role (Student, Admin, etc.) |
| 5 | Is Active | ❌ | true/false, 1/0, yes/no |
| 6 | Student Code | ✅ (for Student) | Mã sinh viên |
| 7 | Cohort | ✅ (for Student) | Khóa (K16, K17, etc.) |
| 8 | Date of Birth | ❌ | Ngày sinh |
| 9 | Gender | ❌ | Male/Female/Other |
| 10 | Enrollment Date | ❌ | Ngày nhập học |
| 11 | Major Code | ✅ (for Student) | Mã ngành (SE, IA, etc.) |
| 12 | Student Status | ❌ | Active/Inactive/Graduated/Suspended |

### **Response Format:**
```csharp
public class ImportUsersResponse
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> SuccessMessages { get; set; }
    public List<ImportUserError> Errors { get; set; }
}

public class ImportUserError
{
    public int RowNumber { get; set; }
    public string Email { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 📝 Cần tạo: Import Page

### **File cần tạo:**

#### 1. `WebFE/Pages/Admin/Students/Import.cshtml`

```razor
@page
@model WebFE.Pages.Admin.Students.ImportModel
@{
    ViewData["Title"] = "Import Students";
    ViewData["Breadcrumb"] = "Import Students";
    Layout = "~/Pages/Shared/_AdminLayout.cshtml";
}

<div class="page-header">
    <div class="d-flex align-items-center justify-content-between">
        <div>
            <h1 class="page-title">Import Students</h1>
            <p class="page-description">Import students from Excel file</p>
        </div>
        <a href="/Admin/Students" class="btn btn-outline-secondary">
            <i data-lucide="arrow-left" style="width: 16px; height: 16px;"></i>
            Back to List
        </a>
    </div>
</div>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show">
        <i data-lucide="check-circle" style="width: 16px; height: 16px;"></i>
        @TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

@if (TempData["ErrorMessage"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show">
        <i data-lucide="alert-circle" style="width: 16px; height: 16px;"></i>
        @TempData["ErrorMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

@if (Model.ImportResult != null)
{
    <div class="alert @(Model.ImportResult.FailureCount == 0 ? "alert-success" : "alert-warning") alert-dismissible fade show">
        <h5 class="alert-heading">Import Results</h5>
        <ul class="mb-0">
            <li>Total Rows: @Model.ImportResult.TotalRows</li>
            <li>Success: @Model.ImportResult.SuccessCount</li>
            <li>Failed: @Model.ImportResult.FailureCount</li>
        </ul>
        
        @if (Model.ImportResult.Errors.Any())
        {
            <hr>
            <h6>Errors:</h6>
            <ul>
                @foreach (var error in Model.ImportResult.Errors)
                {
                    <li>Row @error.RowNumber (@error.Email): @error.ErrorMessage</li>
                }
            </ul>
        }
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

<div class="content-card">
    <div class="content-card-header">
        <h2 class="content-card-title">Upload Excel File</h2>
    </div>
    <div class="content-card-body">
        <div class="alert alert-info">
            <i data-lucide="info" style="width: 16px; height: 16px;"></i>
            <strong>Instructions:</strong>
            <ol class="mb-0 mt-2">
                <li>Download the template Excel file below</li>
                <li>Fill in student information</li>
                <li>Upload the completed file</li>
            </ol>
        </div>

        <!-- Download Template -->
        <div class="mb-4">
            <h5>Step 1: Download Template</h5>
            <a href="/Admin/Students/DownloadTemplate" class="btn btn-outline-primary">
                <i data-lucide="download" style="width: 16px; height: 16px;"></i>
                Download Excel Template
            </a>
        </div>

        <!-- Upload Form -->
        <form method="post" enctype="multipart/form-data">
            <h5>Step 2: Upload File</h5>
            <div class="mb-3">
                <label for="file" class="form-label">Select Excel File</label>
                <input type="file" class="form-control" id="file" name="file" accept=".xlsx,.xls" required>
                <div class="form-text">Accepted formats: .xlsx, .xls (Max size: 5MB)</div>
            </div>

            <div class="d-flex gap-2">
                <button type="submit" class="btn btn-primary">
                    <i data-lucide="upload" style="width: 16px; height: 16px;"></i>
                    Upload & Import
                </button>
                <a href="/Admin/Students" class="btn btn-secondary">Cancel</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <script>
        lucide.createIcons();
    </script>
}
```

---

#### 2. `WebFE/Pages/Admin/Students/Import.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObject.DTOs.ImportFile;
using Services.UserImport;
using ClosedXML.Excel;

namespace WebFE.Pages.Admin.Students
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly IUserImportService _importService;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(
            IUserImportService importService,
            ILogger<ImportModel> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        public ImportUsersResponse? ImportResult { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload";
                return Page();
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size exceeds 5MB limit";
                return Page();
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["ErrorMessage"] = "Only Excel files (.xlsx, .xls) are accepted";
                return Page();
            }

            try
            {
                ImportResult = await _importService.ImportUsersFromExcelAsync(file);

                if (ImportResult.FailureCount == 0)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {ImportResult.SuccessCount} students!";
                    return RedirectToPage("/Admin/Students/Index");
                }
                else
                {
                    TempData["WarningMessage"] = $"Imported {ImportResult.SuccessCount} students with {ImportResult.FailureCount} errors. Please review the errors below.";
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing students");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnGetDownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Students");

            // Headers
            worksheet.Cell(1, 1).Value = "Email";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Phone Number";
            worksheet.Cell(1, 4).Value = "Roles";
            worksheet.Cell(1, 5).Value = "Is Active";
            worksheet.Cell(1, 6).Value = "Student Code";
            worksheet.Cell(1, 7).Value = "Cohort";
            worksheet.Cell(1, 8).Value = "Date of Birth";
            worksheet.Cell(1, 9).Value = "Gender";
            worksheet.Cell(1, 10).Value = "Enrollment Date";
            worksheet.Cell(1, 11).Value = "Major Code";
            worksheet.Cell(1, 12).Value = "Student Status";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Example data
            worksheet.Cell(2, 1).Value = "student1@fpt.edu.vn";
            worksheet.Cell(2, 2).Value = "Nguyen Van A";
            worksheet.Cell(2, 3).Value = "0901234567";
            worksheet.Cell(2, 4).Value = "Student";
            worksheet.Cell(2, 5).Value = "true";
            worksheet.Cell(2, 6).Value = "SE123456";
            worksheet.Cell(2, 7).Value = "K16";
            worksheet.Cell(2, 8).Value = "2000-01-01";
            worksheet.Cell(2, 9).Value = "Male";
            worksheet.Cell(2, 10).Value = "2020-09-01";
            worksheet.Cell(2, 11).Value = "SE";
            worksheet.Cell(2, 12).Value = "Active";

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Student_Import_Template.xlsx");
        }
    }
}
```

---

## 📦 Dependencies

Service đã sử dụng **EPPlus** package. Nếu cần tạo template với ClosedXML:

```bash
dotnet add package ClosedXML
```

Hoặc thêm vào `.csproj`:
```xml
<PackageReference Include="ClosedXML" Version="0.102.1" />
```

---

## 🔧 Register Service (Nếu chưa có)

Trong `WebAPI/Program.cs` hoặc `Startup.cs`:

```csharp
// Add UserImport Service
builder.Services.AddScoped<IUserImportService, UserImportService>();
```

---

## ✅ Testing Checklist

- [ ] Tạo file `Import.cshtml` và `Import.cshtml.cs`
- [ ] Test download template
- [ ] Test upload valid Excel file
- [ ] Test upload invalid file type
- [ ] Test upload file > 5MB
- [ ] Test import with all valid data
- [ ] Test import with some invalid rows
- [ ] Test import with duplicate emails
- [ ] Test import with duplicate student codes
- [ ] Test import with invalid major codes
- [ ] Verify users and students are created in database
- [ ] Verify error messages display correctly
- [ ] Verify success redirect to Index page

---

## 🎯 Summary

**Đã có:**
- ✅ UserImportService (hoàn chỉnh)
- ✅ Nút Import Students trên Index page
- ✅ URL: `/Admin/Students/Import`

**Cần tạo:**
- ⏳ Import.cshtml (UI page)
- ⏳ Import.cshtml.cs (Page handler)
- ⏳ Register service (nếu chưa có)

**Thời gian ước tính:** 15-20 phút

---

## 📞 Support

Nếu cần hỗ trợ implement hoặc có vấn đề gì, vui lòng liên hệ team development.
