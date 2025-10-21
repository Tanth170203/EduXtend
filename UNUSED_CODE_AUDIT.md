# 🔍 **AUDIT CODE DƯ THỪA - 21/10/2025**

## 📋 **Tổng quan**

Sau khi chuyển đổi modal sang page-based CRUD, cần kiểm tra và xóa:
1. JavaScript functions không sử dụng
2. CSS modal-related không cần thiết
3. PageModel POST handlers đã di chuyển
4. Unused imports và variables

---

## 🗑️ **A. JavaScript Functions không sử dụng**

### **1. MovementReports/Index.cshtml** ✅ (Đã dọn dẹp)
**Đã xóa:**
- ❌ `loadStudentsForScoring()`
- ❌ `loadBehaviorsForCategory()`
- ❌ `validateScore()`
- ❌ `submitScore()`
- ❌ `updateStudentInfo()`
- ❌ `openQuickScoreModal()`
- ❌ Modal event listener for `scoringModal`

**Còn lại (cần thiết):**
- ✅ `filterTable()` - Search functionality
- ✅ `sortTable()` - Sort functionality
- ✅ `resetFilters()` - Reset filters
- ✅ `exportToExcel()` - Export feature
- ✅ `printReport()` - Print feature

---

### **2. Evidences/Index.cshtml** ⏳ (Cần dọn dẹp sau convert)
**Cần xóa sau khi convert reviewModal:**
```javascript
// Handle review modal
var reviewModal = document.getElementById('reviewModal');
if (reviewModal) {
    reviewModal.addEventListener('show.bs.modal', function (event) {
        var button = event.relatedTarget;
        var evidenceId = button.getAttribute('data-evidence-id');
        // ... ~30 lines
    });
}
```

**Cần xóa sau khi xử lý viewModal:**
```javascript
// Handle view modal
var viewModal = document.getElementById('viewModal');
if (viewModal) {
    viewModal.addEventListener('show.bs.modal', function (event) {
        // ... ~40 lines
    });
}
```

**Estimate**: ~70-80 lines JavaScript có thể xóa

---

### **3. Student/MyEvidences/Index.cshtml** ⏳
**Cần xóa sau khi convert submitModal:**
```javascript
// Submit modal handler
var submitModal = document.getElementById('submitModal');
if (submitModal) {
    submitModal.addEventListener('show.bs.modal', function() {
        // Reset form, load criteria, etc.
        // ... ~50 lines
    });
}

function loadCriteriaForSubmit() { ... }
function validateSubmitForm() { ... }
// ... more functions
```

**Estimate**: ~100-120 lines JavaScript

---

### **4. Criteria/Index.cshtml** ⏳
**Cần xóa sau khi convert:**
- `openAddGroupModal()`
- `openEditGroupModal(id, name, maxScore)`
- `openDeleteGroupModal(id, name)`
- Modal event listeners (3 modals)

**Estimate**: ~80-100 lines JavaScript

---

### **5. Semesters/Index.cshtml** ⏳
**Cần xóa sau khi convert:**
- `openAddSemesterModal()`
- `openEditSemesterModal(id, name, startDate, endDate, isActive)`
- `openDeleteSemesterModal(id, name)`
- Modal event listeners (4 modals)

**Estimate**: ~100-120 lines JavaScript

---

## 🎨 **B. CSS không sử dụng**

### **admin-dashboard.css**
**Location**: `EduXtend/WebFE/wwwroot/css/admin-dashboard.css`

#### **Modal-specific styles (có thể xóa nếu không còn modal):**
```css
/* Lines 48-117: Modal alignment fixes */
.modal {
    position: fixed !important;
    /* ... */
}

.modal-dialog {
    margin: 0 auto !important;
    /* ... */
}

.modal-backdrop {
    position: fixed !important;
    /* ... */
}
```

**Status**: 
- 🟡 **KEEP** nếu còn view-only modals (viewModal, delete confirms)
- ❌ **REMOVE** nếu xóa tất cả modals

**Estimate**: ~70 lines CSS

---

### **Unused utility classes:**
**Check for usage:**
```bash
# Check if classes are used anywhere
grep -rn "class.*modal-custom" EduXtend/WebFE/Pages/
grep -rn "class.*modal-overlay" EduXtend/WebFE/Pages/
```

**Estimate**: ~20-30 lines CSS utilities

---

## 📄 **C. PageModel POST Handlers không sử dụng**

### **1. MovementReports/Index.cshtml.cs** ✅ (Đã kiểm tra)
**Handlers còn lại:**
- ✅ `OnGetAsync()` - Load data
- ✅ `OnPostDeleteAsync()` - Inline delete (có thể giữ)

**Status**: Clean ✅

---

### **2. Evidences/Index.cshtml.cs** ⏳
**Cần xóa sau convert:**
```csharp
public async Task<IActionResult> OnPostReviewAsync(
    int id, 
    string status, 
    double points, 
    string? comment, 
    int reviewedById)
{
    // ~50 lines
    // Now in Review.cshtml.cs
}
```

**Estimate**: ~50 lines C#

---

### **3. Criteria/Index.cshtml.cs** ⏳
**Cần xóa sau convert:**
```csharp
public async Task<IActionResult> OnPostCreateGroupAsync(...)
{
    // Now in Add.cshtml.cs
}

public async Task<IActionResult> OnPostUpdateGroupAsync(...)
{
    // Now in Edit.cshtml.cs
}

public async Task<IActionResult> OnPostDeleteGroupAsync(int id)
{
    // Keep if using inline delete, else remove
}
```

**Estimate**: ~100-150 lines C#

---

### **4. Semesters/Index.cshtml.cs** ⏳
**Cần xóa sau convert:**
```csharp
public async Task<IActionResult> OnPostCreateAsync(...)
{
    // Now in Add.cshtml.cs
}

public async Task<IActionResult> OnPostUpdateAsync(...)
{
    // Now in Edit.cshtml.cs
}

public async Task<IActionResult> OnPostDeleteAsync(int id)
{
    // Keep or remove
}
```

**Estimate**: ~100-150 lines C#

---

### **5. Student/MyEvidences/Index.cshtml.cs** ⏳
**Cần xóa sau convert:**
```csharp
public async Task<IActionResult> OnPostSubmitAsync(...)
{
    // Now in Submit.cshtml.cs
}
```

**Estimate**: ~80-100 lines C#

---

## 🔧 **D. Unused Imports và Variables**

### **Sau khi cleanup, check:**

```powershell
# Find unused usings (sau khi xóa code)
cd EduXtend\WebFE
dotnet build /p:TreatWarningsAsErrors=true
```

**Common unused after modal removal:**
- `using System.Text.Json;` (nếu không còn AJAX)
- Modal-related ViewData properties
- TempData properties chỉ dùng cho modal

**Estimate**: ~10-20 lines per file

---

## 📊 **Tổng hợp ước tính**

| Category | Files Affected | Lines to Remove | Priority |
|----------|----------------|-----------------|----------|
| JavaScript (modal functions) | 5 | ~450-550 | 🔴 HIGH |
| CSS (modal styles) | 1 | ~90-120 | 🟡 MEDIUM |
| POST handlers | 4 | ~330-450 | 🔴 HIGH |
| Unused imports | 5 | ~50-100 | 🟢 LOW |
| **TOTAL** | **15** | **~920-1,220** | |

**Combined with modal HTML removal**: ~3,200 lines  
**Grand Total Cleanup**: ~4,120-4,420 lines 🎉

---

## ✅ **Cleanup Checklist**

### **Phase 1: After Each Modal Conversion**
- [ ] Remove modal HTML block
- [ ] Remove modal JavaScript functions
- [ ] Remove modal event listeners
- [ ] Remove POST handler from Index.cshtml.cs
- [ ] Test that page still works
- [ ] Run linter, fix errors

### **Phase 2: After All Conversions**
- [ ] Audit all JavaScript files for unused functions
- [ ] Check CSS for unused modal styles
- [ ] Remove unused imports (dotnet build warnings)
- [ ] Remove unused variables/properties
- [ ] Run full test suite
- [ ] Performance check

### **Phase 3: Final Polish**
- [ ] Code review
- [ ] Update documentation
- [ ] Measure code reduction (git diff)
- [ ] Celebrate 🎉

---

## 🔍 **Automated Detection Commands**

### **Find potentially unused functions:**
```powershell
# Find all function definitions
grep -rn "function\s\+\w\+\s*(" EduXtend/WebFE/Pages/

# Check if function is called anywhere
grep -rn "functionName(" EduXtend/WebFE/Pages/
```

### **Find unused CSS classes:**
```powershell
# Extract CSS class names
grep -oP "\.[\w-]+" wwwroot/css/admin-dashboard.css > classes.txt

# Check usage in HTML
foreach ($class in Get-Content classes.txt) {
    $usage = grep -rn "$class" Pages/
    if (!$usage) {
        Write-Host "Unused: $class"
    }
}
```

### **Find unused C# methods:**
```powershell
# Build with warnings as errors
dotnet build /p:TreatWarningsAsErrors=true

# Look for IDE0051 (unused private members)
# Look for IDE0052 (unread private members)
```

---

## 📝 **Example: Manual Cleanup Workflow**

### **Step 1: Convert modal → page**
```bash
# Example: Evidences Review
1. Create Review.cshtml.cs
2. Create Review.cshtml
3. Test new page works
```

### **Step 2: Remove old code**
```bash
# In Index.cshtml:
1. Remove <div class="modal fade" id="reviewModal">...</div>
2. Remove <script> reviewModal.addEventListener(...) </script>
```

### **Step 3: Remove POST handler**
```bash
# In Index.cshtml.cs:
1. Remove OnPostReviewAsync() method
```

### **Step 4: Verify**
```bash
dotnet build
# Check for warnings
# Manual test
```

---

## 🎯 **Success Metrics**

### **Code Quality:**
- [ ] Zero linter warnings
- [ ] Zero unused variable warnings
- [ ] Zero dead code paths

### **Performance:**
- [ ] Page load < 2s
- [ ] No console errors
- [ ] No 404s for removed assets

### **Maintainability:**
- [ ] Code reduction 25-40%
- [ ] Consistent patterns
- [ ] Clear documentation

---

**Status**: 📝 **AUDIT DOCUMENTED**  
**Next**: Execute cleanup after each modal conversion  
**Updated**: 21/10/2025  
**Estimated Cleanup**: ~4,120-4,420 lines total

