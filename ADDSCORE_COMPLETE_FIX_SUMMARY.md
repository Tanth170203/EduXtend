# Sửa lỗi AddScore - Đầy đủ CategoryId và CriterionId

## 🎯 **Vấn đề đã hiểu đúng:**

### **Phân tích cấu trúc database:**
1. **MovementCriterionGroup** (Category) - Nhóm tiêu chí
   - `Id` = CategoryId (1-4)
   - `Name` = "Ý thức học tập", "Hoạt động chính trị", etc.
   - `MaxScore` = Giới hạn điểm cho nhóm

2. **MovementCriterion** (Criterion) - Tiêu chí cụ thể
   - `Id` = CriterionId
   - `GroupId` = CategoryId (foreign key)
   - `Title` = "Tuyên dương công khai", "Olympic/ACM", etc.
   - `MaxScore` = Giới hạn điểm cho tiêu chí

3. **MovementRecord** - Điểm tổng của sinh viên
4. **MovementRecordDetails** - Điểm chi tiết từng tiêu chí
   - `CriterionId` = ID của tiêu chí cụ thể
   - `Score` = Điểm sinh viên đạt được

### **Vấn đề trước đây:**
- API chỉ nhận `CategoryId` nhưng không có `CriterionId`
- Không thể lưu vào `MovementRecordDetails` vì thiếu `CriterionId`
- Frontend gửi sai payload format

## 🔧 **Giải pháp đã implement:**

### **1. Tạo DTO mới:**
```csharp
// AddManualScoreWithCriterionDto.cs
public class AddManualScoreWithCriterionDto
{
    [Required] public int StudentId { get; set; }
    [Required, Range(1, 4)] public int CategoryId { get; set; }  // GroupId
    [Required] public int CriterionId { get; set; }              // CriterionId cụ thể
    [Required, Range(0, 100)] public double Score { get; set; }
    [Required, MinLength(10)] public string Comments { get; set; }
    public DateTime? AwardedDate { get; set; }
}
```

### **2. Tạo API endpoint mới:**
```csharp
// MovementRecordController.cs
[HttpPost("add-manual-score-with-criterion")]
public async Task<ActionResult<MovementRecordDto>> AddManualScoreWithCriterion(
    [FromBody] AddManualScoreWithCriterionDto dto)
```

### **3. Implement service method:**
```csharp
// MovementRecordService.cs
public async Task<MovementRecordDto> AddManualScoreWithCriterionAsync(
    AddManualScoreWithCriterionDto dto)
{
    // 1. Validate criterion exists and belongs to category
    // 2. Check category max score
    // 3. Auto-adjust score if exceeds category max
    // 4. Create/Update MovementRecordDetail with specific CriterionId
    // 5. Update total score
    // 6. Apply caps and adjustments
}
```

### **4. Sửa Frontend:**
```csharp
// AddScore.cshtml.cs
[BindProperty] public int GroupId { get; set; }        // CategoryId
[BindProperty] public int CriterionId { get; set; }    // CriterionId
public List<MovementCriterionDto> AllCriteria { get; set; } = new();

var payload = new
{
    studentId = StudentId,
    categoryId = GroupId,        // CategoryId = GroupId
    criterionId = CriterionId,  // CriterionId để lưu vào MovementRecordDetail
    score = Score,
    comments = Comments,
    awardedDate = DateTime.Now
};
```

### **5. Sửa HTML form:**
```html
<!-- Group Selection -->
<select asp-for="GroupId" onchange="updateCriteriaDropdown()">
    <option value="">-- Chọn danh mục --</option>
    @foreach (var group in Model.Groups)
    {
        <option value="@group.Id" data-max="@group.MaxScore">
            @group.Name (Max: @group.MaxScore điểm)
        </option>
    }
</select>

<!-- Criterion Selection (dependent on Group) -->
<select asp-for="CriterionId" disabled>
    <option value="">-- Chọn danh mục trước --</option>
</select>
```

### **6. Sửa JavaScript:**
```javascript
function updateCriteriaDropdown() {
    const groupSelect = document.getElementById('GroupId');
    const criterionSelect = document.getElementById('CriterionId');
    const selectedGroupId = parseInt(groupSelect.value);
    
    if (selectedGroupId) {
        // Filter criteria by selected group
        const filteredCriteria = allCriteria.filter(c => c.groupId === selectedGroupId);
        
        // Add filtered criteria to dropdown
        filteredCriteria.forEach(criterion => {
            const option = document.createElement('option');
            option.value = criterion.id;
            option.textContent = `${criterion.title} (Max: ${criterion.maxScore} điểm)`;
            criterionSelect.appendChild(option);
        });
        
        criterionSelect.disabled = false;
    }
}
```

## 📊 **Payload đúng format:**

### **Trước (SAI):**
```json
{
    "studentId": 9,
    "criterionId": 4,  // ❌ SAI - API expect categoryId
    "score": 2,
    "comments": "Tuyen duong cong khai"
}
```

### **Sau (ĐÚNG):**
```json
{
    "studentId": 9,
    "categoryId": 1,      // ✅ GroupId (Category)
    "criterionId": 4,    // ✅ CriterionId cụ thể
    "score": 2,
    "comments": "Tuyen duong cong khai",
    "awardedDate": "2025-10-21T13:27:20.6110371+07:00"
}
```

## 🎯 **Kết quả:**

### **✅ Database được lưu đúng:**
1. **MovementRecord** - Điểm tổng của sinh viên
2. **MovementRecordDetails** - Điểm chi tiết với `CriterionId` cụ thể
3. **Validation đúng** - Criterion thuộc Category
4. **Auto-adjust** - Điểm không vượt quá giới hạn

### **✅ Frontend hoạt động đúng:**
1. **Cascading dropdown** - Chọn Group → hiện Criteria
2. **Validation** - Check cả GroupId và CriterionId
3. **Payload đúng** - Gửi cả CategoryId và CriterionId
4. **API call thành công** - Không còn lỗi 400

### **✅ Logic business đúng:**
1. **Category validation** - Criterion phải thuộc Category
2. **Score capping** - Không vượt quá giới hạn Category
3. **Duplicate handling** - Update nếu đã có, Create nếu chưa có
4. **Total score update** - Cập nhật điểm tổng

## 🚀 **Test case:**

### **1. Chọn Group và Criterion hợp lệ:**
- GroupId = 1 (Ý thức học tập)
- CriterionId = 4 (Tuyên dương công khai)
- Score = 2
- **Expected:** ✅ Success, lưu vào MovementRecordDetails

### **2. Chọn Criterion không thuộc Group:**
- GroupId = 1 (Ý thức học tập)
- CriterionId = 10 (thuộc Group 2)
- **Expected:** ❌ "Criterion does not belong to category"

### **3. Score vượt quá giới hạn:**
- GroupId = 1 (Max: 35 điểm)
- Current total = 33 điểm
- Score = 5
- **Expected:** ✅ Auto-adjust to 2 điểm

## 📋 **Files đã tạo/sửa:**

### **Tạo mới:**
- `AddManualScoreWithCriterionDto.cs` - DTO mới
- `ADDSCORE_COMPLETE_FIX_SUMMARY.md` - Tài liệu này

### **Sửa:**
- `AddScore.cshtml.cs` - Thêm CriterionId, sửa payload
- `AddScore.cshtml` - Thêm Criterion dropdown, JavaScript
- `MovementRecordController.cs` - Thêm endpoint mới
- `IMovementRecordService.cs` - Thêm method interface
- `MovementRecordService.cs` - Implement method mới

---
**Date:** October 21, 2025  
**Status:** ✅ COMPLETED  
**Priority:** HIGH - Đã sửa xong hoàn toàn
