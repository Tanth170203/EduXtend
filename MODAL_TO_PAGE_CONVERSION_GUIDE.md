# 🔄 **HƯỚNG DẪN CHUYỂN ĐỔI MODAL SANG PAGE**

## 📋 **Checklist tổng quan**

### **Bước 1: Tạo Page mới**
- [ ] Tạo `[Action].cshtml.cs` (PageModel)
- [ ] Tạo `[Action].cshtml` (View)
- [ ] Copy form fields từ modal HTML
- [ ] Copy validation từ modal
- [ ] Copy POST handler từ Index.cshtml.cs

### **Bước 2: Cập nhật Index page**
- [ ] Thay modal button → link to new page
- [ ] Thay table action modal trigger → link with query string
- [ ] Xóa modal HTML block
- [ ] Xóa modal JavaScript functions
- [ ] Xóa modal event listeners

### **Bước 3: Test**
- [ ] Page load works
- [ ] Form validation works
- [ ] Form submission works
- [ ] Success redirect works
- [ ] Error handling works
- [ ] Navigation (back button) works

### **Bước 4: Cleanup**
- [ ] Remove unused POST handler from Index.cshtml.cs
- [ ] Check for unused variables/properties
- [ ] Run linter
- [ ] Test regression

---

## 📝 **Example: Evidence Review Modal → Page**

### **1. Tạo `Review.cshtml.cs`**

```csharp
using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Evidences
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReviewModel> _logger;
        private readonly IConfiguration _configuration;

        public ReviewModel(
            IHttpClientFactory httpClientFactory,
            ILogger<ReviewModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public int EvidenceId { get; set; }

        [BindProperty]
        public string Status { get; set; } = string.Empty;

        [BindProperty]
        public double Points { get; set; }

        [BindProperty]
        public string? Comment { get; set; }

        public EvidenceDto? Evidence { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "ID không hợp lệ.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Evidence = JsonSerializer.Deserialize<EvidenceDto>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (Evidence != null)
                    {
                        EvidenceId = Evidence.Id;
                        Points = 5; // Default points
                    }
                }
                else
                {
                    ErrorMessage = "Không tìm thấy minh chứng.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidence");
                ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu.";
                return RedirectToPage("./Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Thông tin không hợp lệ.";
                return Page();
            }

            if (string.IsNullOrEmpty(Status))
            {
                ErrorMessage = "Vui lòng chọn quyết định duyệt.";
                return Page();
            }

            if (Status == "Approved" && Points <= 0)
            {
                ErrorMessage = "Điểm phải lớn hơn 0 khi duyệt.";
                return Page();
            }

            try
            {
                using var httpClient = CreateHttpClient();

                var reviewDto = new
                {
                    id = EvidenceId,
                    status = Status,
                    points = Points,
                    comment = Comment,
                    reviewedById = 1 // TODO: Get from current user
                };

                var json = JsonSerializer.Serialize(reviewDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/evidences/review", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = Status == "Approved" 
                        ? "✅ Đã duyệt minh chứng thành công!" 
                        : "❌ Đã từ chối minh chứng.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Review failed: {Error}", errorContent);
                    ErrorMessage = "Không thể duyệt minh chứng. Vui lòng thử lại.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing evidence");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return Page();
            }
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(
                    new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001"),
                    new Cookie(cookie.Key, cookie.Value)
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );

            return client;
        }
    }
}
```

### **2. Tạo `Review.cshtml`**

```html
@page
@model WebFE.Pages.Admin.Evidences.ReviewModel
@{
    ViewData["Title"] = "Duyệt Minh chứng";
    ViewData["Breadcrumb"] = "Review Evidence";
    Layout = "~/Pages/Shared/_AdminLayout.cshtml";
}

<!-- Page Header -->
<div class="page-header mb-4">
    <div class="d-flex align-items-center justify-content-between">
        <div>
            <h1 class="page-title">
                <i data-lucide="check-square" style="width: 28px; height: 28px;"></i>
                Duyệt Minh chứng
            </h1>
            <p class="page-description">Xem xét và phê duyệt minh chứng phong trào</p>
        </div>
        <a href="/Admin/Evidences" class="btn btn-outline-secondary">
            <i data-lucide="arrow-left" style="width: 16px; height: 16px;"></i>
            Quay lại
        </a>
    </div>
</div>

<!-- Alert Messages -->
@if (!string.IsNullOrEmpty(Model.ErrorMessage))
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i data-lucide="alert-circle" style="width: 16px; height: 16px;"></i>
        @Model.ErrorMessage
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

@if (Model.Evidence == null)
{
    <div class="alert alert-warning">Không tìm thấy minh chứng.</div>
}
else
{
    <!-- Evidence Info Card -->
    <div class="row">
        <div class="col-lg-8 mx-auto">
            
            <!-- Student & Evidence Info -->
            <div class="card border-0 shadow-sm mb-4">
                <div class="card-body">
                    <h5 class="card-title mb-3">
                        <i data-lucide="info" style="width: 20px; height: 20px;"></i>
                        Thông tin minh chứng
                    </h5>
                    
                    <div class="alert alert-info mb-3">
                        <div class="d-flex align-items-center">
                            <i data-lucide="user" style="width: 24px; height: 24px; margin-right: 0.75rem;"></i>
                            <div>
                                <strong>Sinh viên:</strong> @Model.Evidence.StudentName
                            </div>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="fw-bold">Tiêu đề:</label>
                        <p>@Model.Evidence.Title</p>
                    </div>

                    <div class="mb-3">
                        <label class="fw-bold">Tiêu chí:</label>
                        <p>@(Model.Evidence.CriterionTitle ?? "N/A")</p>
                    </div>

                    <div class="mb-3">
                        <label class="fw-bold">Mô tả:</label>
                        <p class="text-muted">@(Model.Evidence.Description ?? "Không có mô tả")</p>
                    </div>

                    @if (!string.IsNullOrEmpty(Model.Evidence.FilePath))
                    {
                        <div class="mb-3">
                            <label class="fw-bold">File đính kèm:</label>
                            <a href="@Model.Evidence.FilePath" target="_blank" class="btn btn-sm btn-outline-primary">
                                <i data-lucide="external-link" style="width: 14px; height: 14px;"></i>
                                Xem file
                            </a>
                        </div>
                    }
                </div>
            </div>

            <!-- Review Form Card -->
            <div class="card border-0 shadow-sm">
                <div class="card-header bg-primary text-white">
                    <h5 class="mb-0">
                        <i data-lucide="edit-3" style="width: 20px; height: 20px;"></i>
                        Quyết định duyệt
                    </h5>
                </div>
                <div class="card-body p-4">
                    <form method="post">
                        <input type="hidden" asp-for="EvidenceId" />

                        <!-- Decision -->
                        <div class="mb-4">
                            <label asp-for="Status" class="form-label fw-bold">
                                Quyết định <span class="text-danger">*</span>
                            </label>
                            <select asp-for="Status" class="form-select form-select-lg" required>
                                <option value="">-- Chọn quyết định --</option>
                                <option value="Approved">✅ Duyệt</option>
                                <option value="Rejected">❌ Từ chối</option>
                            </select>
                        </div>

                        <!-- Points -->
                        <div class="mb-4">
                            <label asp-for="Points" class="form-label fw-bold">
                                Điểm cộng <span class="text-danger">*</span>
                            </label>
                            <input asp-for="Points" type="number" class="form-control form-control-lg" 
                                   min="0" max="100" step="0.5" required />
                            <small class="form-text text-muted">Nhập điểm từ 0-100</small>
                        </div>

                        <!-- Comment -->
                        <div class="mb-4">
                            <label asp-for="Comment" class="form-label fw-bold">Ghi chú của reviewer</label>
                            <textarea asp-for="Comment" class="form-control" rows="4" 
                                      placeholder="Nhập ghi chú, nhận xét về minh chứng này..."></textarea>
                        </div>

                        <hr class="my-4">

                        <!-- Actions -->
                        <div class="d-flex justify-content-between">
                            <a href="/Admin/Evidences" class="btn btn-secondary">
                                <i data-lucide="x" style="width: 16px; height: 16px;"></i>
                                Hủy
                            </a>
                            <button type="submit" class="btn btn-primary btn-lg fw-bold">
                                <i data-lucide="check" style="width: 18px; height: 18px;"></i>
                                Xác nhận duyệt
                            </button>
                        </div>
                    </form>
                </div>
            </div>

        </div>
    </div>
}

@section Scripts {
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        });
    </script>
}
```

### **3. Update `Index.cshtml`** - Thay đổi buttons

**Before:**
```html
<button type="button" class="btn btn-sm btn-primary" 
        data-bs-toggle="modal" 
        data-bs-target="#reviewModal"
        data-evidence-id="@evidence.Id"
        data-student-name="@evidence.StudentName">
    <i data-lucide="check" style="width: 14px; height: 14px;"></i>
    Duyệt
</button>
```

**After:**
```html
<a href="/Admin/Evidences/Review?id=@evidence.Id" class="btn btn-sm btn-primary">
    <i data-lucide="check" style="width: 14px; height: 14px;"></i>
    Duyệt
</a>
```

### **4. Remove modal HTML** từ `Index.cshtml`

Xóa block:
```html
<!-- Review Modal -->
<div class="modal fade" id="reviewModal" tabindex="-1">
    ...
</div>
```

### **5. Remove modal JavaScript** từ `Index.cshtml`

Xóa:
```javascript
// Handle review modal
var reviewModal = document.getElementById('reviewModal');
if (reviewModal) {
    reviewModal.addEventListener('show.bs.modal', function (event) {
        // ...
    });
}
```

### **6. Remove POST handler** từ `Index.cshtml.cs`

Xóa method:
```csharp
public async Task<IActionResult> OnPostReviewAsync(
    int id, 
    string status, 
    double points, 
    string? comment, 
    int reviewedById)
{
    // This is now in Review.cshtml.cs
}
```

---

## 🔁 **Repeat cho tất cả modals**

### **Priority Order:**

1. ✅ **MovementReports AddScore** (DONE)
2. ⏳ **Evidences Review** → `/Admin/Evidences/Review?id=X`
3. ⏳ **MyEvidences Submit** → `/Student/MyEvidences/Submit`
4. ⏳ **Criteria Add** → `/Admin/Criteria/Add`
5. ⏳ **Criteria Edit** → `/Admin/Criteria/Edit?id=X`
6. ⏳ **Semesters Add** → `/Admin/Semesters/Add`
7. ⏳ **Semesters Edit** → `/Admin/Semesters/Edit?id=X`

### **Delete Confirmations** (Optional - Có thể giữ modal hoặc dùng inline confirm):
- `deleteGroupModal` → Inline confirm with SweetAlert2
- `deleteSemesterModal` → Inline confirm with SweetAlert2

---

## ⚡ **Quick Commands**

### **Tìm tất cả modal triggers:**
```bash
grep -rn "data-bs-toggle=\"modal\"" EduXtend/WebFE/Pages/
```

### **Tìm tất cả modal HTML:**
```bash
grep -rn "class=\"modal fade\"" EduXtend/WebFE/Pages/
```

### **Tìm modal event listeners:**
```bash
grep -rn "show.bs.modal" EduXtend/WebFE/Pages/
```

### **Tìm unused POST handlers:**
```bash
grep -rn "OnPostAsync" EduXtend/WebFE/Pages/*/Index.cshtml.cs
```

---

## 📊 **Progress Tracking**

| Feature | Modal | Status | New Page | Lines Saved |
|---------|-------|--------|----------|-------------|
| MovementReports | scoringModal | ✅ Done | AddScore.cshtml | ~670 |
| Evidences | reviewModal | ⏳ Pending | Review.cshtml | ~200 |
| Evidences | viewModal | 🔵 Keep | (view-only) | 0 |
| MyEvidences | submitModal | ⏳ Pending | Submit.cshtml | ~150 |
| Criteria | addGroupModal | ⏳ Pending | Add.cshtml | ~100 |
| Criteria | editGroupModal | ⏳ Pending | Edit.cshtml | ~100 |
| Criteria | deleteGroupModal | 🟡 Consider | (or inline) | ~50 |
| Semesters | addSemesterModal | ⏳ Pending | Add.cshtml | ~80 |
| Semesters | editSemesterModal | ⏳ Pending | Edit.cshtml | ~80 |
| Semesters | deleteSemesterModal | 🟡 Consider | (or inline) | ~50 |
| **TOTAL** | | **1/10** | | **~1,480** |

---

## ✅ **Completion Checklist**

- [x] Documentation created
- [ ] All CRUD modals converted
- [ ] All modal HTML removed
- [ ] All modal JavaScript removed
- [ ] All unused POST handlers removed
- [ ] Linter checks pass
- [ ] Manual testing complete
- [ ] User acceptance testing
- [ ] Deployment to staging
- [ ] Production deployment

---

**Status**: 📝 **GUIDE READY**  
**Next Action**: Implement Evidences Review page  
**Updated**: 21/10/2025

