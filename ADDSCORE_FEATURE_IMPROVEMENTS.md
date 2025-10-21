# Add Score Feature Improvements ✅

## Tổng quan
Đã cải thiện feature "Cộng Điểm Phong Trào" theo yêu cầu mới.

## Các thay đổi chính

### 1. ✅ Cascading Dropdown: Danh mục → Tiêu chí

**Trước:**
- Chỉ có 1 dropdown "Danh mục" (hardcoded 4 categories)
- Không có tiêu chí cụ thể

**Sau:**
- **Dropdown 1 - Danh mục (Group)**: Chọn danh mục trước
- **Dropdown 2 - Tiêu chí (Criterion)**: Sau khi chọn danh mục, hiển thị các tiêu chí thuộc danh mục đó

```javascript
// Logic cascading
function updateCriteriaDropdown() {
    const selectedGroupId = parseInt(groupSelect.value);
    const filteredCriteria = allCriteria.filter(c => c.groupId === selectedGroupId);
    // Populate criterion dropdown with filtered criteria
}
```

### 2. ✅ Filter theo TargetType = "Student"

**Backend (AddScore.cshtml.cs):**
```csharp
private async Task LoadGroupsAndCriteriaAsync(HttpClient httpClient)
{
    // Only get groups for Student
    Groups = allGroups.Where(g => g.TargetType == "Student").ToList();
    
    // Only get criteria for Student and active ones
    AllCriteria = allCriteria
        .Where(c => c.TargetType == "Student" && c.IsActive)
        .ToList();
}
```

**Kết quả:**
- ✅ Chỉ hiển thị danh mục dành cho Student
- ✅ Chỉ hiển thị tiêu chí dành cho Student (không hiển thị tiêu chí cho Club)
- ✅ Chỉ hiển thị tiêu chí đang active (`IsActive = true`)

### 3. ✅ Xóa field "Ngày thực hiện"

**Trước:**
```html
<input asp-for="AwardedDate" type="date" class="form-control" id="AwardedDate">
```

**Sau:**
- ❌ Đã xóa field "Ngày thực hiện" khỏi UI
- ✅ Mặc định sử dụng `DateTime.Now` khi submit

```csharp
var payload = new
{
    studentId = StudentId,
    criterionId = CriterionId,
    score = Score,
    comments = Comments,
    awardedDate = DateTime.Now // Mặc định là ngày hiện tại
};
```

### 4. ✅ API Call thay đổi

**Trước:**
```json
{
  "studentId": 123,
  "categoryId": 1,  // ❌ Hardcoded category
  "score": 10,
  "comments": "...",
  "awardedDate": "2025-10-21"
}
```

**Sau:**
```json
{
  "studentId": 123,
  "criterionId": 45,  // ✅ Real criterion ID from database
  "score": 10,
  "comments": "...",
  "awardedDate": "2025-10-21T09:30:00" // ✅ DateTime.Now
}
```

### 5. ✅ Cải thiện UX

**Các cải thiện:**
1. **Disabled state**: Criterion dropdown bị disable cho đến khi chọn danh mục
2. **Dynamic hints**: Hiển thị số lượng tiêu chí có sẵn
3. **Max score info**: Hiển thị giới hạn điểm của danh mục/tiêu chí
4. **Clear instructions**: Hướng dẫn rõ ràng về quy trình chọn

**Thông báo động:**
```javascript
// Khi chưa chọn danh mục
"Tiêu chí sẽ được hiển thị sau khi chọn danh mục"

// Sau khi chọn danh mục có tiêu chí
"Đã tải 5 tiêu chí cho danh mục này"

// Khi danh mục không có tiêu chí
"Danh mục này chưa có tiêu chí nào"
```

## File changes

### Modified Files

#### 1. `AddScore.cshtml.cs`
**Changes:**
- ✅ Added `using BusinessObject.DTOs.MovementCriteria;`
- ✅ Replaced `CategoryId` với `GroupId` và `CriterionId`
- ✅ Removed `AwardedDate` property (auto-generated in backend)
- ✅ Replaced `Categories` với `Groups` và `AllCriteria`
- ✅ Added `LoadGroupsAndCriteriaAsync()` method
- ✅ Added `ReloadDataAsync()` helper method
- ✅ Updated `OnPostAsync()` to use `CriterionId` và `DateTime.Now`
- ✅ Removed `SetupCategories()` method
- ✅ Removed `CategoryDto` class

**Tổng:** ~120 lines changed

#### 2. `AddScore.cshtml`
**Changes:**
- ✅ Replaced single "Danh mục" dropdown với 2 dropdowns (Group + Criterion)
- ✅ Removed "Ngày thực hiện" date field
- ✅ Added cascading dropdown logic in JavaScript
- ✅ Updated info box với instructions mới
- ✅ Added `allCriteria` JavaScript variable from server data
- ✅ Added `updateCriteriaDropdown()` function

**Tổng:** ~80 lines changed

## Testing checklist

### Frontend
- [ ] Khi trang load, dropdown "Tiêu chí" bị disabled
- [ ] Chọn danh mục → dropdown "Tiêu chí" được enable và hiển thị tiêu chí của danh mục đó
- [ ] Chỉ hiển thị danh mục và tiêu chí cho Student (không có Club)
- [ ] Thông báo động hiển thị đúng (số lượng tiêu chí, max score, etc.)
- [ ] Không có field "Ngày thực hiện" trong form

### Backend
- [ ] API call gửi `criterionId` thay vì `categoryId`
- [ ] `awardedDate` tự động set là `DateTime.Now`
- [ ] Chỉ load Groups với `TargetType = "Student"`
- [ ] Chỉ load Criteria với `TargetType = "Student"` và `IsActive = true`

### Integration
- [ ] Submit form thành công với criterion ID thực
- [ ] Điểm được cộng đúng vào sinh viên
- [ ] Error handling hoạt động đúng (validation, API errors)
- [ ] Redirect về `/Admin/MovementReports/Index` sau khi thành công

## Benefits

### 1. Chính xác hơn
- ✅ Sử dụng real criterion ID từ database
- ✅ Không còn hardcode categories
- ✅ Filter chính xác theo Student/Club

### 2. UX tốt hơn
- ✅ Cascading dropdown dễ hiểu
- ✅ Không cần chọn ngày (auto = hôm nay)
- ✅ Thông báo động, rõ ràng

### 3. Maintainable
- ✅ Data-driven (từ database)
- ✅ Dễ thêm/sửa criteria mà không cần sửa code
- ✅ Separation of concerns (Student vs Club)

### 4. Performance
- ✅ Load tất cả criteria 1 lần, filter ở client-side
- ✅ Không cần API call thêm khi chọn danh mục

## API Requirements

### Endpoints cần có:
1. ✅ `GET /api/movement-criterion-groups` - Get all groups
2. ✅ `GET /api/movement-criteria` - Get all criteria
3. ✅ `POST /api/movement-records/add-manual-score` - Add score

### DTO Changes:
**Payload mới:**
```csharp
{
    int studentId,
    int criterionId,  // Changed from categoryId
    double score,
    string comments,
    DateTime awardedDate  // Auto-generated, not from user input
}
```

## Notes

### Về "Chỉ cộng điểm cho học kì hiện tại"
- ⚠️ Feature này cần được implement ở backend API
- Backend cần check xem student có active trong semester hiện tại không
- Nếu không, trả về error: "Không thể cộng điểm cho sinh viên ở các kì khác"

### Về filter học kì
- ❓ User hỏi: "chỉnh sửa lại không cần filter tất cả học kì (có cần không?)"
- 💡 Suggest: Giữ filter học kì ở trang Index để xem lại lịch sử
- 💡 Trang AddScore: Mặc định cộng điểm cho kì hiện tại (không cần chọn)

---
**Date:** October 21, 2025  
**Status:** ✅ COMPLETE  
**Impact:** Major UX improvement - Cascading dropdowns, auto date, Student-only filter

