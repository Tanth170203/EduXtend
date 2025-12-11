# 📅 GIẢI THÍCH: LẤY DATA SỰ KIỆN THÁNG SAU

## 🎯 Câu hỏi

**"Trong báo cáo, có data các sự kiện của tháng sau, lấy như thế nào?"**

---

## 💡 Câu trả lời ngắn gọn

**Lấy từ bảng `Activities` với điều kiện `StartTime.Month = nextMonth`**

Các sự kiện tháng sau là những **Activity đã được tạo trước** (planned activities) với thời gian bắt đầu rơi vào tháng sau.

---

## 📊 Chi tiết Code

### **Method:** `GetNextMonthPlansAsync()`

**File:** `Services/MonthlyReports/MonthlyReportDataAggregator.cs`

```csharp
public async Task<NextMonthPlansDto> GetNextMonthPlansAsync(
    int clubId, 
    int reportMonth,    // Tháng báo cáo (tháng hiện tại)
    int reportYear, 
    int nextMonth,      // Tháng sau
    int nextYear)
{
    var result = new NextMonthPlansDto();
    
    // ============================================
    // PHẦN 1: Lấy sự kiện dự kiến (Planned Events)
    // ============================================
    
    var plannedEvents = await _context.Activities
        .AsNoTracking()
        .Where(a => a.ClubId == clubId
            && a.StartTime.Month == nextMonth      // ← KEY: Lọc theo tháng sau
            && a.StartTime.Year == nextYear        // ← KEY: Lọc theo năm
            && (a.Type == ActivityType.LargeEvent 
                || a.Type == ActivityType.MediumEvent 
                || a.Type == ActivityType.SmallEvent))
        .Include(a => a.Registrations)             // Lấy người đăng ký
            .ThenInclude(r => r.User)
        .OrderBy(a => a.StartTime)
        .ToListAsync();
    
    // Map sang DTO
    result.PlannedEvents = plannedEvents.Select(activity => new PlannedEventDto
    {
        PlanId = activity.Id,
        EventName = activity.Title,
        EventContent = activity.Description,
        OrganizationTime = activity.StartTime,
        Location = activity.Location,
        ExpectedStudents = activity.MaxParticipants ?? 0,
        // Participants từ Registrations (người đã đăng ký)
    }).ToList();
    
    // ============================================
    // PHẦN 2: Lấy cuộc thi dự kiến
    // ============================================
    
    var plannedCompetitions = await _context.Activities
        .AsNoTracking()
        .Where(a => a.ClubId == clubId
            && a.StartTime.Month == nextMonth      // ← KEY: Lọc theo tháng sau
            && a.StartTime.Year == nextYear
            && (a.Type == ActivityType.SchoolCompetition
                || a.Type == ActivityType.ProvincialCompetition
                || a.Type == ActivityType.NationalCompetition))
        .Include(a => a.Registrations)
            .ThenInclude(r => r.User)
        .OrderBy(a => a.StartTime)
        .ToListAsync();
    
    result.PlannedCompetitions = plannedCompetitions.Select(activity => 
        new PlannedCompetitionDto
        {
            CompetitionName = activity.Title,
            CompetitionTime = activity.StartTime,
            Location = activity.Location,
            Participants = activity.Registrations.Select(r => 
                new CompetitionParticipantDto
                {
                    FullName = r.User.FullName,
                    StudentCode = GetStudentCode(r.User.Id),
                    Email = r.User.Email
                }).ToList()
        }).ToList();
    
    // ============================================
    // PHẦN 3: Lấy kế hoạch truyền thông
    // ============================================
    
    var communicationPlans = await _communicationPlanRepo
        .GetByClubAndMonthAsync(clubId, nextMonth, nextYear);
    
    foreach (var commPlan in communicationPlans)
    {
        foreach (var item in commPlan.Items)
        {
            result.CommunicationPlan.Add(new CommunicationItemDto
            {
                Content = item.Content,
                Time = item.ScheduledDate,
                ResponsiblePerson = item.ResponsiblePerson
            });
        }
    }
    
    return result;
}
```

---

## 🔍 Phân tích chi tiết

### **1. Điều kiện lọc quan trọng:**

```csharp
.Where(a => a.ClubId == clubId
    && a.StartTime.Month == nextMonth    // ← Tháng sau
    && a.StartTime.Year == nextYear      // ← Năm (xử lý trường hợp tháng 12)
    && a.Type == ActivityType.LargeEvent)
```

**Giải thích:**
- `StartTime.Month == nextMonth`: Lọc activities có thời gian bắt đầu trong tháng sau
- `StartTime.Year == nextYear`: Xử lý trường hợp tháng 12 → tháng 1 năm sau
- `Type`: Lọc theo loại sự kiện (Event, Competition, etc.)

### **2. Tính toán nextMonth và nextYear:**

**File:** `Services/MonthlyReports/MonthlyReportService.cs`

```csharp
private async Task<MonthlyReportDto> BuildMonthlyReportDto(
    Plan plan, bool includeAggregatedData)
{
    int reportMonth = plan.ReportMonth.Value;  // Ví dụ: 11 (tháng 11)
    int reportYear = plan.ReportYear.Value;    // Ví dụ: 2025
    
    // Tính tháng sau
    int nextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
    int nextYear = reportMonth == 12 ? reportYear + 1 : reportYear;
    
    // Ví dụ:
    // - Nếu reportMonth = 11 → nextMonth = 12, nextYear = 2025
    // - Nếu reportMonth = 12 → nextMonth = 1, nextYear = 2026
    
    // Gọi aggregator với nextMonth và nextYear
    dto.NextMonthPlans = await _dataAggregator.GetNextMonthPlansAsync(
        clubId, reportMonth, reportYear, nextMonth, nextYear
    );
}
```

**Giải thích:**
- Xử lý đặc biệt cho tháng 12 → Tháng 1 năm sau
- Truyền cả `reportMonth` và `nextMonth` vào aggregator

---

## 📋 Ví dụ cụ thể

### **Scenario: Báo cáo tháng 11/2025**

```
reportMonth = 11
reportYear = 2025
nextMonth = 12
nextYear = 2025
```

### **Query sẽ lấy:**

```sql
SELECT * FROM Activities
WHERE ClubId = 1
  AND MONTH(StartTime) = 12        -- Tháng 12
  AND YEAR(StartTime) = 2025       -- Năm 2025
  AND Type IN ('LargeEvent', 'MediumEvent', 'SmallEvent')
ORDER BY StartTime
```

### **Kết quả:**

```
Activity 1:
- Title: "Sự kiện Giáng sinh 2025"
- StartTime: 2025-12-20 14:00:00
- Type: LargeEvent
- MaxParticipants: 200

Activity 2:
- Title: "Workshop cuối năm"
- StartTime: 2025-12-28 09:00:00
- Type: MediumEvent
- MaxParticipants: 50
```

---

## 🔄 Workflow tổng thể

```
1. Tạo báo cáo tháng 11/2025
   ↓
2. Tính nextMonth = 12, nextYear = 2025
   ↓
3. Query Activities:
   - WHERE StartTime.Month = 12
   - WHERE StartTime.Year = 2025
   - WHERE Type = Event/Competition
   ↓
4. Lấy được các sự kiện đã được tạo trước:
   - Sự kiện Giáng sinh (20/12)
   - Workshop cuối năm (28/12)
   ↓
5. Hiển thị trong báo cáo phần "Kế hoạch tháng 12"
```

---

## 🎯 Điểm quan trọng

### **1. Activities phải được tạo trước**

Để xuất hiện trong báo cáo, các sự kiện tháng sau **PHẢI ĐÃ ĐƯỢC TẠO** trong hệ thống với:
- `StartTime` rơi vào tháng sau
- `Status` có thể là: Planned, Approved, etc.

**Ví dụ:**
```csharp
// Tạo sự kiện tháng 12 (từ tháng 11)
var newActivity = new Activity
{
    ClubId = 1,
    Title = "Sự kiện Giáng sinh",
    StartTime = new DateTime(2025, 12, 20, 14, 0, 0),  // 20/12/2025
    Type = ActivityType.LargeEvent,
    Status = "Planned",
    MaxParticipants = 200
};
```

### **2. Registrations vs Attendances**

**Planned Events (Tháng sau):**
- Dùng `Registrations` (người đăng ký)
- Vì sự kiện chưa diễn ra → Chưa có Attendances

**Completed Events (Tháng hiện tại):**
- Dùng `Attendances` (người thực tế tham gia)
- Vì sự kiện đã diễn ra → Có dữ liệu điểm danh

```csharp
// Tháng sau: Lấy từ Registrations
.Include(a => a.Registrations)
    .ThenInclude(r => r.User)

// Tháng hiện tại: Lấy từ Attendances
.Include(a => a.Attendances)
    .ThenInclude(att => att.User)
```

### **3. Communication Plans**

Kế hoạch truyền thông được lưu riêng trong bảng `CommunicationPlans`:

```csharp
var communicationPlans = await _communicationPlanRepo
    .GetByClubAndMonthAsync(clubId, nextMonth, nextYear);
```

**Bảng:** `CommunicationPlans` và `CommunicationItems`
- Lưu kế hoạch đăng bài, truyền thông
- Có `ScheduledDate` để lọc theo tháng

---

## 📊 So sánh: Tháng hiện tại vs Tháng sau

| Aspect | Tháng hiện tại | Tháng sau |
|--------|----------------|-----------|
| **Nguồn dữ liệu** | Activities (completed) | Activities (planned) |
| **Điều kiện** | `StartTime.Month = reportMonth` | `StartTime.Month = nextMonth` |
| **Người tham gia** | `Attendances` (đã điểm danh) | `Registrations` (đã đăng ký) |
| **Status** | Completed, Cancelled | Planned, Approved |
| **Đánh giá** | Có (ActivityEvaluations) | Chưa có |
| **Timeline** | Có (ActivitySchedules) | Có thể có (nếu đã lên lịch) |

---

## 💡 Tóm tắt

**Câu trả lời ngắn gọn:**

Dữ liệu sự kiện tháng sau được lấy từ bảng `Activities` với điều kiện:
```csharp
WHERE StartTime.Month == nextMonth 
  AND StartTime.Year == nextYear
  AND Type IN (Event, Competition, ...)
```

**Điều kiện tiên quyết:**
- CLB phải tạo trước các sự kiện tháng sau
- Sự kiện có `StartTime` rơi vào tháng sau
- Dữ liệu người tham gia lấy từ `Registrations` (chưa có Attendances)

**Workflow:**
1. Tạo báo cáo tháng X
2. Tính nextMonth = X + 1
3. Query Activities có StartTime trong tháng X + 1
4. Hiển thị trong phần "Kế hoạch tháng sau"

Đơn giản và logic! 🎯
