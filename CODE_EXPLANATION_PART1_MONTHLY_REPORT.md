# 📊 GIẢI THÍCH CODE: MONTHLY REPORT

## 🎯 Tổng quan

Monthly Report là chức năng **TỰ ĐỘNG** tạo báo cáo hoạt động tháng hiện tại và kế hoạch tháng sau cho CLB.

---

## 1️⃣ BACKGROUND SERVICE - Tự động tạo báo cáo

### **File:** `WebAPI/BackgroundServices/MonthlyReportGenerationService.cs`

```csharp
public class MonthlyReportGenerationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = CalculateNextRunTime(now);
            var delay = nextRun - now;
            
            // Đợi đến 00:00 ngày 1 hàng tháng
            await Task.Delay(delay, stoppingToken);
            
            // Tạo báo cáo cho tất cả CLB
            await GenerateMonthlyReportsAsync();
        }
    }
}
```

**Giải thích:**
- `BackgroundService`: Chạy liên tục trong background
- `ExecuteAsync()`: Vòng lặp chính
- `CalculateNextRunTime()`: Tính thời điểm chạy tiếp theo (00:00 ngày 1)
- `Task.Delay()`: Đợi đến thời điểm đó
- `GenerateMonthlyReportsAsync()`: Tạo báo cáo

### **Logic tính thời gian chạy:**

```csharp
private DateTime CalculateNextRunTime(DateTime now)
{
    // Nếu hôm nay là ngày 1 và trước 00:30 → Chạy ngay
    if (now.Day == 1 && now.Hour == 0 && now.Minute < 30)
    {
        return now;
    }

    // Nếu không → Tính ngày 1 tháng sau
    var nextMonth = now.Month == 12 ? 1 : now.Month + 1;
    var nextYear = now.Month == 12 ? now.Year + 1 : now.Year;
    
    return new DateTime(nextYear, nextMonth, 1, 0, 0, 0);
}
```

**Giải thích:**
- Nếu đang là ngày 1 lúc 00:00-00:30 → Chạy luôn
- Nếu không → Tính ngày 1 tháng sau lúc 00:00
- Xử lý đặc biệt tháng 12 → Sang năm mới

### **Logic tạo báo cáo:**

```csharp
private async Task GenerateMonthlyReportsAsync()
{
    // 1. Lấy tất cả CLB đang hoạt động
    var clubs = await clubRepository.SearchClubsAsync(
        null, null, isActive: true
    );
    
    var now = DateTime.Now;
    var reportMonth = now.Month;  // Tháng hiện tại
    var reportYear = now.Year;
    
    foreach (var club in clubs)
    {
        // 2. Kiểm tra báo cáo đã tồn tại chưa
        var existingReports = await monthlyReportService
            .GetAllReportsAsync(club.Id);
        
        var reportExists = existingReports.Any(r => 
            r.ReportMonth == reportMonth && 
            r.ReportYear == reportYear
        );
        
        if (reportExists)
        {
            // Đã có → Bỏ qua
            skipCount++;
            continue;
        }
        
        // 3. Tạo báo cáo mới
        var reportId = await monthlyReportService
            .CreateMonthlyReportAsync(
                club.Id, 
                reportMonth, 
                reportYear
            );
        
        successCount++;
    }
}
```

**Giải thích:**
1. Lấy danh sách CLB active
2. Với mỗi CLB:
   - Check xem báo cáo tháng này đã có chưa
   - Nếu chưa → Tạo mới
   - Nếu có rồi → Skip
3. Log kết quả (success, skip, error)

---

## 2️⃣ SERVICE - Logic nghiệp vụ

### **File:** `Services/MonthlyReports/MonthlyReportService.cs`

### **Tạo báo cáo:**

```csharp
public async Task<int> CreateMonthlyReportAsync(
    int clubId, int month, int year)
{
    // 1. Validate tháng
    var validationError = ValidateMonthSequence(month, year);
    if (!string.IsNullOrEmpty(validationError))
    {
        throw new InvalidOperationException(validationError);
    }

    // 2. Check duplicate
    var existing = await _reportRepo.GetByClubAndMonthAsync(
        clubId, month, year
    );
    if (existing != null)
    {
        throw new InvalidOperationException(
            "Monthly report already exists"
        );
    }

    // 3. Tạo Plan record
    var plan = new Plan
    {
        ClubId = clubId,
        Title = $"Báo cáo tháng {month}/{year}",
        Description = $"Báo cáo hoạt động tháng {month}...",
        Status = "Draft",           // Trạng thái ban đầu
        ReportType = "Monthly",     // Loại báo cáo
        ReportMonth = month,
        ReportYear = year,
        CreatedAt = DateTime.UtcNow
    };

    var created = await _reportRepo.CreateAsync(plan);
    return created.Id;
}
```

**Giải thích:**
1. **Validate:** Check tháng hợp lệ (1-12)
2. **Check duplicate:** Đảm bảo chưa có báo cáo tháng này
3. **Tạo Plan:** Lưu vào bảng Plans với:
   - `ReportType = "Monthly"` → Đánh dấu là báo cáo tháng
   - `Status = "Draft"` → Chờ Club Manager chỉnh sửa
   - `ReportMonth`, `ReportYear` → Tháng/năm báo cáo

### **Lấy dữ liệu báo cáo:**

```csharp
public async Task<MonthlyReportDto> GetReportWithFreshDataAsync(
    int reportId)
{
    // 1. Lấy Plan record
    var plan = await _reportRepo.GetByIdAsync(reportId);
    
    // 2. Build DTO với dữ liệu FRESH
    return await BuildMonthlyReportDto(plan, 
        includeAggregatedData: true);
}
```

**Giải thích:**
- `includeAggregatedData: true` → Lấy dữ liệu mới nhất từ DB
- Không cache, luôn fresh data

---

## 3️⃣ DATA AGGREGATOR - Tổng hợp dữ liệu

### **File:** `Services/MonthlyReports/MonthlyReportDataAggregator.cs`

### **Lấy School Events:**

```csharp
public async Task<List<SchoolEventDto>> GetSchoolEventsAsync(
    int clubId, int month, int year)
{
    // 1. Query Activities
    var activities = await _context.Activities
        .Where(a => a.ClubId == clubId
            && a.StartTime.Month == month
            && a.StartTime.Year == year
            && (a.Type == ActivityType.LargeEvent 
                || a.Type == ActivityType.MediumEvent 
                || a.Type == ActivityType.SmallEvent))
        .Include(a => a.Attendances)      // Người tham gia
        .Include(a => a.Evaluation)       // Đánh giá
        .ToListAsync();

    // 2. Với mỗi activity, build DTO
    foreach (var activity in activities)
    {
        // 2.1 Lấy người tham gia
        var participants = activity.Attendances
            .Where(att => att.IsPresent)  // Chỉ lấy người có mặt
            .Select(att => new ParticipantDto {
                FullName = att.User.FullName,
                StudentCode = GetStudentCode(att.User.Id),
                Rating = att.ParticipationScore
            })
            .ToList();

        // 2.2 Lấy thành viên hỗ trợ
        var supportMembers = await GetSupportMembersAsync(
            activity.Id, clubId
        );

        // 2.3 Lấy đánh giá
        var evaluation = BuildEvaluation(activity.Evaluation);

        // 2.4 Lấy timeline
        var timeline = await GetActivityTimelineAsync(activity.Id);

        // 2.5 Build SchoolEventDto
        schoolEvents.Add(new SchoolEventDto {
            EventName = activity.Title,
            EventDate = activity.StartTime,
            Participants = participants,
            SupportMembers = supportMembers,
            Evaluation = evaluation,
            Timeline = timeline
        });
    }

    return schoolEvents;
}
```

**Giải thích từng bước:**

1. **Query Activities:**
   - Lọc theo ClubId, Month, Year
   - Chỉ lấy type = Event (Large/Medium/Small)
   - Include Attendances và Evaluation

2. **Build DTO cho mỗi activity:**
   - **Participants:** Từ `ActivityAttendances` (IsPresent = true)
   - **SupportMembers:** Từ `ActivityMemberEvaluations`
   - **Evaluation:** Từ `ActivityEvaluations`
   - **Timeline:** Từ `ActivitySchedules`

3. **Return:** List các SchoolEventDto

### **Lấy Support Members:**

```csharp
private async Task<List<SupportMemberDto>> GetSupportMembersAsync(
    int activityId, int clubId)
{
    // 1. Lấy evaluations
    var memberEvaluations = await _memberEvalRepo
        .GetByActivityIdAsync(activityId);

    var supportMembers = new List<SupportMemberDto>();

    foreach (var eval in memberEvaluations)
    {
        // 2. Lấy assignment (người được phân công)
        var assignment = await _context.ActivityScheduleAssignments
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => 
                a.Id == eval.ActivityScheduleAssignmentId
            );

        if (assignment?.User != null)
        {
            supportMembers.Add(new SupportMemberDto {
                FullName = assignment.User.FullName,
                StudentCode = GetStudentCode(assignment.User.Id),
                Position = assignment.Role,
                Rating = (decimal)eval.AverageScore
            });
        }
    }

    return supportMembers;
}
```

**Giải thích:**
- Lấy từ `ActivityMemberEvaluations` (đánh giá thành viên)
- Join với `ActivityScheduleAssignments` (phân công)
- Lấy thông tin User và điểm đánh giá

---

## 4️⃣ CONTROLLER - API Endpoints

### **File:** `WebAPI/Controllers/MonthlyReportController.cs`

### **GET - Lấy danh sách báo cáo:**

```csharp
[HttpGet]
public async Task<IActionResult> GetAllReports(
    [FromQuery] int? clubId)
{
    var userId = GetCurrentUserId();
    
    // Admin không cần clubId → Lấy tất cả
    if (!clubId.HasValue)
    {
        var isAdmin = User.IsInRole("Admin");
        if (isAdmin)
        {
            var allReports = await _service
                .GetAllReportsForAdminAsync();
            return Ok(new { 
                data = allReports, 
                count = allReports.Count 
            });
        }
    }

    // Club Manager → Lấy theo clubId
    var reports = await _service.GetAllReportsAsync(clubId.Value);
    return Ok(new { data = reports, count = reports.Count });
}
```

**Giải thích:**
- Admin: Lấy tất cả báo cáo
- Club Manager: Lấy báo cáo của CLB mình

### **POST - Tạo báo cáo thủ công:**

```csharp
[HttpPost]
[Authorize(Roles = "ClubManager,Admin")]
public async Task<IActionResult> CreateReport(
    [FromBody] CreateMonthlyReportDto dto)
{
    // 1. Validate
    if (dto.Month < 1 || dto.Month > 12)
        return BadRequest("Invalid month");

    // 2. Tạo báo cáo
    var reportId = await _service.CreateMonthlyReportAsync(
        dto.ClubId, dto.Month, dto.Year
    );

    // 3. Lấy dữ liệu fresh
    var report = await _service
        .GetReportWithFreshDataAsync(reportId);
    
    return CreatedAtAction(
        nameof(GetReport), 
        new { id = reportId }, 
        report
    );
}
```

**Giải thích:**
- Cho phép tạo báo cáo thủ công (ngoài tự động)
- Validate month (1-12)
- Trả về báo cáo với dữ liệu đầy đủ

---

## 🔄 WORKFLOW TỔNG THỂ

```
1. Background Service (00:00 ngày 1)
   ↓
2. Lấy danh sách CLB active
   ↓
3. Với mỗi CLB:
   - Check báo cáo đã tồn tại?
   - Nếu chưa → Tạo Plan (Status: Draft)
   ↓
4. Club Manager:
   - Xem báo cáo (GET /api/monthly-reports/{id})
   - Data Aggregator tổng hợp dữ liệu FRESH
   - Chỉnh sửa phần editable
   - Submit (POST /api/monthly-reports/{id}/submit)
   ↓
5. Admin:
   - Xem danh sách báo cáo chờ duyệt
   - Approve/Reject
   ↓
6. Notification:
   - Gửi thông báo cho Admin khi submit
   - Gửi thông báo cho Club Manager khi approve/reject
```

---

## 💡 ĐIỂM QUAN TRỌNG

1. **Dữ liệu luôn FRESH:** Không cache, query trực tiếp từ DB
2. **Tự động tạo:** Background service chạy 00:00 ngày 1
3. **Editable sections:** Club Manager chỉ sửa được 3 phần
4. **Status workflow:** Draft → PendingApproval → Approved/Rejected
5. **Notification:** Tự động gửi khi có sự kiện

Đây là chức năng phức tạp nhất của Trần Hữu Tân! 🎉
