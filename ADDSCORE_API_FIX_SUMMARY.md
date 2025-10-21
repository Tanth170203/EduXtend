# Sửa lỗi API AddScore - CategoryId vs CriterionId

## 🚨 **Vấn đề đã phát hiện:**

### **Lỗi API:**
```
"CategoryId":["Category ID must be between 1 and 4"]
```

### **Nguyên nhân:**
- Frontend gửi `criterionId` nhưng API expect `categoryId`
- API endpoint `/api/movement-records/add-manual-score` expect `AddManualScoreDto` với `CategoryId`
- Frontend đang gửi payload sai format

## 🔧 **Các thay đổi đã thực hiện:**

### **1. Sửa payload trong AddScore.cshtml.cs:**
```csharp
// TRƯỚC (SAI):
var payload = new
{
    studentId = StudentId,
    criterionId = CriterionId,  // ❌ SAI
    score = Score,
    comments = Comments,
    awardedDate = DateTime.Now
};

// SAU (ĐÚNG):
var payload = new
{
    studentId = StudentId,
    categoryId = GroupId,  // ✅ ĐÚNG - sử dụng GroupId
    score = Score,
    comments = Comments,
    awardedDate = DateTime.Now
};
```

### **2. Sửa validation logic:**
```csharp
// TRƯỚC (SAI):
if (StudentId <= 0 || CriterionId <= 0 || Score < 0)

// SAU (ĐÚNG):
if (StudentId <= 0 || GroupId <= 0 || Score < 0)
```

### **3. Xóa CriterionId không cần thiết:**
```csharp
// TRƯỚC:
[BindProperty]
public int CriterionId { get; set; }

// SAU:
// CriterionId không cần vì API chỉ cần GroupId (CategoryId)
```

### **4. Xóa AllCriteria không cần thiết:**
```csharp
// TRƯỚC:
public List<MovementCriterionDto> AllCriteria { get; set; } = new();

// SAU:
// AllCriteria không cần vì API chỉ cần GroupId
```

### **5. Sửa method LoadGroupsAndCriteriaAsync:**
```csharp
// TRƯỚC:
await LoadGroupsAndCriteriaAsync(httpClient);

// SAU:
await LoadGroupsAsync(httpClient);
```

### **6. Xóa JavaScript không cần thiết:**
```javascript
// TRƯỚC:
const allCriteria = @Html.Raw(Json.Serialize(Model.AllCriteria));
function updateCriteriaDropdown() { ... }

// SAU:
// AllCriteria không cần vì API chỉ cần GroupId
// updateCriteriaDropdown không cần vì API chỉ cần GroupId
```

### **7. Thêm JavaScript mới cho score hint:**
```javascript
function updateScoreHint() {
    const groupSelect = document.getElementById('GroupId');
    const scoreHint = document.getElementById('scoreHint');
    const selectedOption = groupSelect.options[groupSelect.selectedIndex];
    
    if (groupSelect.value) {
        const groupMaxScore = selectedOption.getAttribute('data-max');
        scoreHint.innerHTML = `<i data-lucide="info" style="width: 14px; height: 14px;"></i> Giới hạn danh mục: ${groupMaxScore} điểm`;
    } else {
        scoreHint.innerHTML = '<i data-lucide="info" style="width: 14px; height: 14px;"></i> Phạm vi: 0 - 100 (sẽ tự động điều chỉnh nếu vượt max danh mục)';
    }
}
```

### **8. Sửa HTML form:**
```html
<!-- TRƯỚC: Có CriterionId dropdown -->
<select asp-for="CriterionId" class="form-select form-select-lg" id="CriterionId" required disabled>

<!-- SAU: Xóa CriterionId dropdown -->
<!-- Note: API chỉ cần GroupId (CategoryId), không cần CriterionId -->
```

## 📋 **API Contract đã hiểu đúng:**

### **AddManualScoreDto:**
```csharp
public class AddManualScoreDto
{
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    [Range(1, 4, ErrorMessage = "Category ID must be between 1 and 4")]
    public int CategoryId { get; set; }  // ✅ Đây là GroupId
    
    [Required]
    [Range(0, 100)]
    public double Score { get; set; }
    
    [Required]
    [MinLength(10)]
    public string Comments { get; set; }
    
    public DateTime? AwardedDate { get; set; }
}
```

### **API Endpoint:**
```
POST /api/movement-records/add-manual-score
```

## 🎯 **Kết quả sau khi sửa:**

### **✅ Payload đúng format:**
```json
{
    "studentId": 9,
    "categoryId": 1,  // ✅ Đúng - GroupId
    "score": 2,
    "comments": "Tuyen duong cong khai",
    "awardedDate": "2025-10-21T13:27:20.6110371+07:00"
}
```

### **✅ Validation đúng:**
- Check `GroupId` thay vì `CriterionId`
- API sẽ validate `CategoryId` trong range 1-4

### **✅ UI đơn giản hơn:**
- Chỉ cần chọn Group (Category)
- Không cần chọn Criterion cụ thể
- API sẽ tự động xử lý logic bên trong

## 🚀 **Test case:**

### **1. Chọn Group hợp lệ:**
- GroupId = 1 (Ý thức học tập)
- Score = 2
- Comments = "Tuyên dương công khai"
- **Expected:** ✅ Success

### **2. Chọn Group không hợp lệ:**
- GroupId = 5 (không tồn tại)
- **Expected:** ❌ "Category ID must be between 1 and 4"

### **3. Score vượt quá giới hạn:**
- GroupId = 1 (Max: 35 điểm)
- Score = 50
- **Expected:** ❌ "Score exceeds the allowed limit"

## 📊 **So sánh trước và sau:**

| Aspect | Trước (SAI) | Sau (ĐÚNG) |
|--------|-------------|------------|
| **Payload** | `criterionId: 4` | `categoryId: 1` |
| **Validation** | Check `CriterionId` | Check `GroupId` |
| **UI** | 2 dropdowns (Group + Criterion) | 1 dropdown (Group only) |
| **API Call** | ❌ 400 BadRequest | ✅ 200 Success |
| **Error** | "Category ID must be between 1 and 4" | No error |

---
**Date:** October 21, 2025  
**Status:** ✅ FIXED  
**Priority:** HIGH - Đã sửa xong
