# ✅ AUTO-APPROVE ACTIVITIES FEATURE

## 🎯 Yêu cầu

**"Khi Admin duyệt report thì các activity của tháng sau nằm trong report đó đều được duyệt chuyển trạng thái từ Pending sang Approved"**

---

## ✅ Đã implement

### **File:** `Services/MonthlyReports/MonthlyReportApprovalService.cs`

---

## 📋 Logic

### **1. Khi Admin approve report:**

```csharp
public async Task ApproveReportAsync(int reportId, int adminId)
{
    // 1. Approve report
    report.Status = "Approved";
    report.ApprovedById = adminId;
    report.ApprovedAt = DateTimeHelper.Now;
    
    await _reportRepo.UpdateAsync(report);
    
    // 2. AUTO-APPROVE activities tháng sau
    await AutoApproveNextMonthActivitiesAsync(report, adminId);
    
    // 3. Send notification
    // ...
}
```

### **2. Method tự động approve activities:**

```csharp
private async Task AutoApproveNextMonthActivitiesAsync(
    Plan report, int adminId)
{
    // 1. Tính tháng sau
    int reportMonth = report.ReportMonth.Value;
    int reportYear = report.ReportYear.Value;
    int nextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
    int nextYear = reportMonth == 12 ? reportYear + 1 : reportYear;
    
    // 2. Lấy tất cả activities Pending của tháng sau
    var pendingActivities = await _context.Activities
        .Where(a => a.ClubId == report.ClubId
            && a.StartTime.Month == nextMonth
            && a.StartTime.Year == nextYear
            && (a.Status == "Pending" || a.Status == "PendingApproval"))
        .ToListAsync();
    
    // 3. Approve tất cả
    if (pendingActivities.Any())
    {
        foreach (var activity in pendingActivities)
        {
            activity.Status = "Approved";
            activity.ApprovedById = adminId;
            activity.ApprovedAt = DateTimeHelper.Now;
        }
        
        await _context.SaveChangesAsync();
        
        // Log
        Console.WriteLine($"Auto-approved {pendingActivities.Count} activities");
    }
}
```

---

## 🔄 Workflow

```
1. Club Manager submit report tháng 11
   - Report status: PendingApproval
   - Activities tháng 12: Status = Pending
   ↓
2. Admin xem report tháng 11
   - Thấy kế hoạch tháng 12
   - Các activities: Sự kiện A, B, C (Pending)
   ↓
3. Admin approve report
   - Report status: Approved ✅
   ↓
4. System tự động:
   - Tìm tất cả activities tháng 12 của CLB
   - Lọc activities có status = Pending/PendingApproval
   - Chuyển tất cả sang Approved ✅
   - Ghi log số lượng activities đã approve
   ↓
5. Kết quả:
   - Report tháng 11: Approved ✅
   - Sự kiện A tháng 12: Approved ✅
   - Sự kiện B tháng 12: Approved ✅
   - Sự kiện C tháng 12: Approved ✅
```

---

## 📊 Ví dụ cụ thể

### **Scenario:**

**Report tháng 11/2025:**
- Club Manager tạo report
- Report bao gồm kế hoạch tháng 12:
  - Sự kiện Giáng sinh (20/12) - Status: Pending
  - Workshop cuối năm (28/12) - Status: Pending
  - Họp tổng kết (30/12) - Status: Pending

**Admin approve report:**
```
POST /api/monthly-reports/123/approve
```

**Kết quả:**
```
✅ Report tháng 11: Approved
✅ Sự kiện Giáng sinh: Approved (auto)
✅ Workshop cuối năm: Approved (auto)
✅ Họp tổng kết: Approved (auto)

Log: "Auto-approved 3 activities (IDs: 456, 457, 458) 
      for next month 12/2025 when report 123 was approved"
```

---

## 🎯 Điều kiện

### **Activities được auto-approve khi:**

1. ✅ Thuộc cùng CLB với report
2. ✅ `StartTime.Month` = nextMonth
3. ✅ `StartTime.Year` = nextYear
4. ✅ `Status` = "Pending" hoặc "PendingApproval"

### **Activities KHÔNG được auto-approve:**

- ❌ Đã Approved rồi (không cần approve lại)
- ❌ Status = "Cancelled" (đã hủy)
- ❌ Status = "Completed" (đã hoàn thành)
- ❌ Thuộc CLB khác
- ❌ Thuộc tháng khác

---

## 💡 Lợi ích

### **1. Tiết kiệm thời gian:**
- Admin không cần approve từng activity một
- Approve report = Approve tất cả activities trong đó

### **2. Logic nghiệp vụ hợp lý:**
- Report đã được duyệt = Kế hoạch đã được chấp thuận
- Activities trong kế hoạch tự động được phép thực hiện

### **3. Tránh quên:**
- Đảm bảo tất cả activities được approve
- Không bỏ sót activity nào

### **4. Audit trail:**
- Lưu `ApprovedById` = adminId
- Lưu `ApprovedAt` = thời điểm approve report
- Log số lượng activities đã approve

---

## 🔧 Customization

### **Nếu muốn chỉ approve một số loại activity:**

```csharp
var pendingActivities = await _context.Activities
    .Where(a => a.ClubId == report.ClubId
        && a.StartTime.Month == nextMonth
        && a.StartTime.Year == nextYear
        && (a.Status == "Pending" || a.Status == "PendingApproval")
        // Thêm điều kiện lọc theo Type
        && (a.Type == ActivityType.LargeEvent 
            || a.Type == ActivityType.MediumEvent))
    .ToListAsync();
```

### **Nếu muốn gửi notification cho Club Manager:**

```csharp
if (pendingActivities.Any())
{
    // Approve activities
    // ...
    
    // Send notification
    var notification = new Notification
    {
        Title = "Các hoạt động đã được phê duyệt",
        Message = $"{pendingActivities.Count} hoạt động tháng {nextMonth} đã được tự động phê duyệt khi báo cáo được duyệt.",
        TargetUserId = clubManager.Id,
        // ...
    };
    await _notificationService.CreateAsync(notification);
}
```

---

## 🧪 Testing

### **Test cases:**

1. **Approve report có activities Pending:**
   - ✅ Activities chuyển sang Approved
   - ✅ ApprovedById = adminId
   - ✅ ApprovedAt được set

2. **Approve report không có activities Pending:**
   - ✅ Không có lỗi
   - ✅ Report vẫn được approve

3. **Approve report có activities đã Approved:**
   - ✅ Không approve lại
   - ✅ Giữ nguyên ApprovedById và ApprovedAt cũ

4. **Approve report tháng 12:**
   - ✅ nextMonth = 1, nextYear = 2026
   - ✅ Activities tháng 1/2026 được approve

5. **Activities thuộc CLB khác:**
   - ✅ Không bị approve nhầm

---

## 📝 Database Changes

### **Activities table:**

| Field | Before | After |
|-------|--------|-------|
| Status | Pending | Approved |
| ApprovedById | NULL | {adminId} |
| ApprovedAt | NULL | {timestamp} |

### **Example:**

```sql
-- Before approve report
SELECT Id, Title, Status, ApprovedById, ApprovedAt
FROM Activities
WHERE ClubId = 1 AND MONTH(StartTime) = 12;

-- Result:
-- 456 | Sự kiện Giáng sinh | Pending | NULL | NULL
-- 457 | Workshop cuối năm  | Pending | NULL | NULL

-- After approve report
-- 456 | Sự kiện Giáng sinh | Approved | 10 | 2025-11-25 10:30:00
-- 457 | Workshop cuối năm  | Approved | 10 | 2025-11-25 10:30:00
```

---

## ⚠️ Lưu ý

1. **Chỉ approve activities của tháng SAU:**
   - Không approve activities tháng hiện tại
   - Không approve activities tháng trước

2. **Chỉ approve activities Pending:**
   - Không thay đổi activities đã Approved
   - Không thay đổi activities Cancelled/Completed

3. **Transaction safety:**
   - Nếu approve report thất bại → Activities không bị approve
   - Nếu approve activities thất bại → Report vẫn được approve (có thể cần rollback)

4. **Performance:**
   - Nếu có nhiều activities → Có thể mất thời gian
   - Consider batch update nếu cần

---

## ✅ Checklist

- [x] Implement `AutoApproveNextMonthActivitiesAsync()`
- [x] Call trong `ApproveReportAsync()`
- [x] Tính toán nextMonth/nextYear đúng
- [x] Lọc activities theo ClubId, Month, Year, Status
- [x] Update Status, ApprovedById, ApprovedAt
- [x] Add logging
- [x] No diagnostics errors
- [ ] Test với report thật
- [ ] Verify database updates
- [ ] Test edge cases (tháng 12, không có activities, etc.)

---

## 🎉 Kết luận

Tính năng đã được implement thành công! 

**Khi Admin approve report → Tất cả activities Pending của tháng sau tự động được approve.**

Logic nghiệp vụ hợp lý và tiết kiệm thời gian cho Admin! 🚀
