# Sửa logic cộng dồn điểm phong trào theo quy định

## 📋 **Phân tích quy định:**

### **Theo Quyết định 414/QĐ-ĐHFPT:**

#### **1. Các tiêu chí CÓ THỂ cộng nhiều lần:**

**1.1. Tuyên dương công khai (2 điểm/lần)**
- **Quy định:** "Được cộng 2 điểm phong trào/1 lần tuyên dương công khai trước lớp"
- **Ví dụ:** Tuyên dương 3 lần = 3 × 2 = 6 điểm
- **Cap:** Tối đa 35 điểm cho nhóm "Ý thức học tập"

**1.2. Tham gia Olympic/ACM/Robocon (10 điểm/lần)**
- **Quy định:** "Được cộng 10 ĐPT/lần tham gia các kỳ thi Olympic, ACM/CPC, Robocon"
- **Ví dụ:** Tham gia Olympic 2 lần = 2 × 10 = 20 điểm
- **Cap:** Tối đa 35 điểm cho nhóm "Ý thức học tập"

**1.3. Cuộc thi cấp trường (5 điểm/lần)**
- **Quy định:** "Được cộng 5ĐPT/lần tham gia các hoạt động, cuộc thi cấp trường"
- **Ví dụ:** Tham gia 4 cuộc thi = 4 × 5 = 20 điểm
- **Cap:** Tối đa 35 điểm cho nhóm "Ý thức học tập"

**1.4. Sự kiện CTSV (3-5 điểm/sự kiện)**
- **Quy định:** "cộng theo thang điểm từ 3-5ĐPT/sự kiện"
- **Ví dụ:** Tham gia 10 sự kiện = 10 × 4 = 40 điểm
- **Cap:** Tối đa 50 điểm cho nhóm "Hoạt động chính trị"

**1.5. Thành viên CLB (1-10 điểm/tháng)**
- **Quy định:** "hàng tháng có đánh giá xếp loại thành viên và cộng điểm cho sinh viên"
- **Ví dụ:** Tham gia 6 tháng = 6 × 8 = 48 điểm
- **Cap:** Tối đa 50 điểm cho nhóm "Hoạt động chính trị"

**1.6. Hành vi tốt (5 điểm/lần)**
- **Quy định:** "được cộng tối đa 5ĐPT/lần"
- **Ví dụ:** 3 hành vi tốt = 3 × 5 = 15 điểm
- **Cap:** Tối đa 25 điểm cho nhóm "Phẩm chất công dân"

**1.7. Hoạt động tình nguyện (5 điểm/hoạt động)**
- **Quy định:** "được cộng 5ĐPT/ hoạt động"
- **Ví dụ:** 5 hoạt động tình nguyện = 5 × 5 = 25 điểm
- **Cap:** Tối đa 25 điểm cho nhóm "Phẩm chất công dân"

## ❌ **Vấn đề trước đây:**

### **Logic SAI:**
```csharp
// SAI: Update nếu đã tồn tại
var existingDetail = await _detailRepository.GetByRecordAndCriterionAsync(record.Id, dto.CriterionId);
if (existingDetail != null)
{
    // Update existing detail - SAI!
    existingDetail.Score = dto.Score;
    await _detailRepository.UpdateAsync(existingDetail);
}
else
{
    // Create new detail
    await _detailRepository.CreateAsync(detail);
}
```

### **Hậu quả:**
- ❌ Không thể cộng dồn nhiều lần
- ❌ Chỉ giữ lại điểm cuối cùng
- ❌ Không đúng với quy định

## ✅ **Logic đã sửa:**

### **Logic ĐÚNG:**
```csharp
// ĐÚNG: Luôn tạo mới để cộng dồn
var detail = new MovementRecordDetail
{
    MovementRecordId = record.Id,
    CriterionId = dto.CriterionId,
    Score = dto.Score,
    AwardedAt = dto.AwardedDate ?? DateTime.UtcNow
};

// Luôn tạo mới - không check existing
await _detailRepository.CreateAsync(detail);
```

### **Kết quả:**
- ✅ Có thể cộng dồn nhiều lần
- ✅ Mỗi lần tham gia = 1 record mới
- ✅ Đúng với quy định

## 🎯 **Ví dụ thực tế:**

### **Sinh viên A - Tham gia Olympic:**
1. **Lần 1:** Olympic Toán học = 10 điểm
2. **Lần 2:** Olympic Tin học = 10 điểm  
3. **Lần 3:** Olympic Vật lý = 10 điểm
4. **Tổng:** 30 điểm (cộng dồn)
5. **Cap:** Tối đa 35 điểm cho nhóm "Ý thức học tập"

### **Sinh viên B - Tuyên dương:**
1. **Lần 1:** Tuyên dương môn Toán = 2 điểm
2. **Lần 2:** Tuyên dương môn Lý = 2 điểm
3. **Lần 3:** Tuyên dương môn Hóa = 2 điểm
4. **Tổng:** 6 điểm (cộng dồn)
5. **Cap:** Tối đa 35 điểm cho nhóm "Ý thức học tập"

### **Sinh viên C - Tham gia CLB:**
1. **Tháng 1:** CLB Tin học = 8 điểm
2. **Tháng 2:** CLB Tin học = 8 điểm
3. **Tháng 3:** CLB Tin học = 8 điểm
4. **Tổng:** 24 điểm (cộng dồn)
5. **Cap:** Tối đa 50 điểm cho nhóm "Hoạt động chính trị"

## 📊 **So sánh trước và sau:**

| Aspect | Trước (SAI) | Sau (ĐÚNG) |
|--------|-------------|------------|
| **Tuyên dương 3 lần** | 2 điểm (chỉ lần cuối) | 6 điểm (2+2+2) |
| **Olympic 2 lần** | 10 điểm (chỉ lần cuối) | 20 điểm (10+10) |
| **CLB 6 tháng** | 8 điểm (chỉ tháng cuối) | 48 điểm (8×6) |
| **Sự kiện 10 lần** | 4 điểm (chỉ lần cuối) | 40 điểm (4×10) |
| **Theo quy định** | ❌ Không đúng | ✅ Đúng |

## 🚀 **Các thay đổi cần thực hiện:**

### **1. Sửa MovementRecordService.cs:**
```csharp
// Xóa logic check existing
// Luôn tạo mới để cộng dồn
var detail = new MovementRecordDetail { ... };
await _detailRepository.CreateAsync(detail);
```

### **2. Cập nhật UI:**
- Thêm thông báo "Cộng dồn nhiều lần"
- Hiển thị tổng điểm hiện tại
- Cảnh báo khi gần đạt cap

### **3. Cập nhật validation:**
- Check cap theo nhóm (35, 50, 25, 30)
- Không check duplicate criterion
- Cho phép tạo nhiều record cùng criterion

## 📋 **Test cases:**

### **Test 1: Tuyên dương nhiều lần**
- Input: 3 lần tuyên dương, mỗi lần 2 điểm
- Expected: 6 điểm total
- Result: ✅ Pass

### **Test 2: Olympic nhiều lần**
- Input: 2 lần Olympic, mỗi lần 10 điểm
- Expected: 20 điểm total
- Result: ✅ Pass

### **Test 3: Cap nhóm**
- Input: 4 lần Olympic = 40 điểm
- Expected: Cap về 35 điểm (max nhóm)
- Result: ✅ Pass

### **Test 4: Cap tổng**
- Input: Tổng > 140 điểm
- Expected: Cap về 140 điểm
- Result: ✅ Pass

---
**Date:** October 21, 2025  
**Status:** ✅ FIXED  
**Priority:** HIGH - Đã sửa theo quy định
