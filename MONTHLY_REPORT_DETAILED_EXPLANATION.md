# 📊 GIẢI THÍCH CHI TIẾT CHỨC NĂNG MONTHLY REPORT

## 🎯 Tổng quan

Monthly Report (Báo cáo tháng) là một báo cáo tổng hợp **TỰ ĐỘNG** về hoạt động của CLB trong tháng hiện tại và kế hoạch cho tháng tiếp theo.

---

## 📋 CẤU TRÚC BÁO CÁO

Báo cáo được chia thành các phần chính:

### 1. **HEADER** (Tiêu đề)
- Tên phòng ban
- Tiêu đề chính: "BÁO CÁO HOẠT ĐỘNG THÁNG X"
- Tiêu đề phụ: "VÀ KẾ HOẠCH THÁNG Y"
- Tên CLB
- Địa điểm: FPT University HCM
- Ngày báo cáo
- Người tạo (Club Manager)

### 2. **PART A: HOẠT ĐỘNG THÁNG HIỆN TẠI** (Tự động)
Gồm 4 loại hoạt động:

#### A.1. School Events (Sự kiện của trường)
#### A.2. Support Activities (Hoạt động hỗ trợ)
#### A.3. Competitions (Cuộc thi)
#### A.4. Internal Meetings (Họp nội bộ)

### 3. **PART B: KẾ HOẠCH THÁNG TIẾP THEO** (Một phần tự động, một phần thủ công)
- Mục đích và ý nghĩa (Editable)
- Sự kiện dự kiến
- Cuộc thi dự kiến
- Kế hoạch truyền thông
- Ngân sách
- Cơ sở vật chất
- Trách nhiệm của CLB (Editable)

### 4. **FOOTER** (Chân trang)
- Người tạo báo cáo
- Người phê duyệt (nếu có)

---

## 🔄 CÁCH LẤY DỮ LIỆU CHO TỪNG PHẦN

### 📌 **A.1. SCHOOL EVENTS (Sự kiện trường)**

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {reportMonth}
  AND YEAR(StartTime) = {reportYear}
  AND Type IN ('LargeEvent', 'MediumEvent', 'SmallEvent')
```

**Dữ liệu lấy ra:**

1. **Thông tin cơ bản:**
   - `ActivityId` - ID hoạt động
   - `EventDate` - Ngày tổ chức (từ `Activity.StartTime`)
   - `EventName` - Tên sự kiện (từ `Activity.Title`)

2. **Danh sách người tham gia** (từ bảng `ActivityAttendances`):
   ```csharp
   Participants = activity.Attendances
       .Where(att => att.IsPresent)  // Chỉ lấy người có mặt
       .Select(att => new ParticipantDto {
           FullName = att.User.FullName,
           StudentCode = GetStudentCode(att.User.Id),
           PhoneNumber = att.User.PhoneNumber,
           Rating = att.ParticipationScore  // Điểm tham gia
       })
   ```

3. **Thành viên hỗ trợ** (từ bảng `ActivityMemberEvaluations`):
   - Lấy từ `ActivityScheduleAssignments` (người được phân công)
   - Kèm theo điểm đánh giá từ `ActivityMemberEvaluation`
   ```csharp
   SupportMembers = {
       FullName,
       StudentCode,
       PhoneNumber,
       Position,  // Vai trò (từ Assignment.Role)
       Rating     // Điểm đánh giá (từ Evaluation.AverageScore)
   }
   ```

4. **Đánh giá sự kiện** (từ bảng `ActivityEvaluations`):
   ```csharp
   Evaluation = {
       ExpectedCount,           // Số người dự kiến
       ActualCount,            // Số người thực tế
       ReasonIfLess,           // Lý do nếu ít hơn
       CommunicationScore,     // Điểm truyền thông
       OrganizationScore,      // Điểm tổ chức
       McHostEvaluation,       // Đánh giá MC/Host
       SpeakerPerformerEvaluation, // Đánh giá diễn giả
       Achievements,           // Thành tựu
       Limitations,            // Hạn chế
       ProposedSolutions       // Giải pháp đề xuất
   }
   ```

5. **Timeline** (từ bảng `ActivitySchedules`):
   ```csharp
   Timeline = "08:00 - 09:00: Khai mạc\n09:00 - 11:00: Phần chính\n..."
   ```

6. **Media URLs** (từ `Activity.ImageUrl`)

---

### 📌 **A.2. SUPPORT ACTIVITIES (Hoạt động hỗ trợ)**

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {reportMonth}
  AND YEAR(StartTime) = {reportYear}
  AND Type = 'SchoolCollaboration'
```

**Dữ liệu lấy ra:**

1. **Thông tin hoạt động:**
   - `EventContent` - Nội dung (từ `Activity.Title`)
   - `DepartmentName` - Tên phòng ban (từ `Activity.Description`)
   - `EventTime` - Thời gian
   - `Location` - Địa điểm
   - `ImageUrl` - Hình ảnh

2. **Danh sách sinh viên hỗ trợ:**
   ```csharp
   SupportStudents = activity.Attendances
       .Where(att => att.IsPresent)
       .Select(att => new SupportStudentDto {
           FullName,
           StudentCode,
           EventName,
           EventTime,
           Rating  // Điểm đánh giá
       })
   ```

---

### 📌 **A.3. COMPETITIONS (Cuộc thi)**

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {reportMonth}
  AND YEAR(StartTime) = {reportYear}
  AND Type IN ('SchoolCompetition', 'ProvincialCompetition', 'NationalCompetition')
```

**Dữ liệu lấy ra:**

1. **Thông tin cuộc thi:**
   - `CompetitionName` - Tên cuộc thi
   - `OrganizingUnit` - Đơn vị tổ chức (từ `Activity.Description`)

2. **Danh sách thí sinh:**
   ```csharp
   Participants = activity.Attendances
       .Where(att => att.IsPresent)
       .Select(att => new CompetitionParticipantDto {
           FullName,
           StudentCode,
           Email,
           Achievement,  // Thành tích (TODO: cần thêm field)
           Note
       })
   ```

---

### 📌 **A.4. INTERNAL MEETINGS (Họp nội bộ)**

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {reportMonth}
  AND YEAR(StartTime) = {reportYear}
  AND Type IN ('ClubMeeting', 'ClubTraining', 'ClubWorkshop')
```

**Dữ liệu lấy ra:**
```csharp
InternalMeetings = {
    MeetingTime,        // Thời gian họp
    Location,           // Địa điểm
    ParticipantCount,   // Số người tham gia (đếm từ Attendances)
    Content,            // Nội dung (từ Activity.Description)
    ImageUrl            // Hình ảnh
}
```

---

### 📌 **B. KẾ HOẠCH THÁNG TIẾP THEO**

#### **B.1. Mục đích và ý nghĩa** (EDITABLE - Thủ công)

**Nguồn dữ liệu:** Bảng `Plans`

```csharp
// Lưu trong field: Plan.NextMonthPurposeAndSignificance (JSON)
Purpose = {
    PurposeText,        // Mục đích
    SignificanceText    // Ý nghĩa
}
```

**Club Manager có thể chỉnh sửa phần này!**

---

#### **B.2. Sự kiện dự kiến** (TỰ ĐỘNG)

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {nextMonth}
  AND YEAR(StartTime) = {nextYear}
  AND Type IN ('LargeEvent', 'MediumEvent', 'SmallEvent')
```

**Dữ liệu lấy ra:**
```csharp
PlannedEvents = {
    EventName,              // Tên sự kiện
    EventContent,           // Nội dung
    OrganizationTime,       // Thời gian tổ chức
    Location,               // Địa điểm
    ExpectedStudents,       // Số sinh viên dự kiến (từ Activity.MaxParticipants)
    RegistrationUrl,        // URL đăng ký (TODO)
    Timeline,               // Lịch trình (TODO)
    Guests                  // Khách mời (TODO)
}
```

---

#### **B.3. Cuộc thi dự kiến** (TỰ ĐỘNG)

**Nguồn dữ liệu:** Bảng `Activities`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND MONTH(StartTime) = {nextMonth}
  AND YEAR(StartTime) = {nextYear}
  AND Type IN ('SchoolCompetition', 'ProvincialCompetition', 'NationalCompetition')
```

**Dữ liệu lấy ra:**
```csharp
PlannedCompetitions = {
    CompetitionName,    // Tên cuộc thi
    AuthorizedUnit,     // Đơn vị cho phép
    CompetitionTime,    // Thời gian
    Location,           // Địa điểm
    Participants        // Danh sách thí sinh (từ ActivityRegistrations)
}
```

---

#### **B.4. Kế hoạch truyền thông** (TỰ ĐỘNG)

**Nguồn dữ liệu:** Bảng `CommunicationPlans` và `CommunicationItems`

**Điều kiện lọc:**
```sql
WHERE ClubId = {clubId}
  AND Month = {nextMonth}
  AND Year = {nextYear}
```

**Dữ liệu lấy ra:**
```csharp
CommunicationPlan = {
    Content,            // Nội dung truyền thông
    Time,               // Thời gian đăng (ScheduledDate)
    ResponsiblePerson,  // Người phụ trách
    NeedSupport         // Cần hỗ trợ? (từ Notes)
}
```

---

#### **B.5. Ngân sách** (MANUAL - Cần nhập thủ công)

**Nguồn dữ liệu:** Bảng `Plans` (field `ReportSnapshot` hoặc cần bảng riêng)

```csharp
Budget = {
    SchoolFunding = [      // Kinh phí từ trường
        { Item, Amount }
    ],
    ClubFunding = [        // Kinh phí từ CLB
        { Item, Amount }
    ]
}
```

**Hiện tại: Chưa có bảng riêng, cần nhập thủ công hoặc lưu trong JSON**

---

#### **B.6. Cơ sở vật chất** (MANUAL - Cần nhập thủ công)

**Nguồn dữ liệu:** Bảng `Plans` (field `ReportSnapshot` hoặc cần bảng riêng)

```csharp
Facility = {
    Items = [
        { Name, Quantity, Source }
    ]
}
```

**Hiện tại: Chưa có bảng riêng, cần nhập thủ công hoặc lưu trong JSON**

---

#### **B.7. Trách nhiệm của CLB** (EDITABLE - Thủ công)

**Nguồn dữ liệu:** Bảng `Plans`

```csharp
// Lưu trong field: Plan.ClubResponsibilities (JSON)
Responsibilities = {
    CustomText  // Nội dung tự do do Club Manager nhập
}
```

**Club Manager có thể chỉnh sửa phần này!**

---

## 🗄️ LƯU TRỮ DỮ LIỆU

### **Bảng Plans** (Lưu Monthly Report)

```csharp
public class Plan {
    // Basic info
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Title { get; set; }
    public string Status { get; set; }  // Draft, PendingApproval, Approved, Rejected
    
    // Monthly Report specific
    public string? ReportType { get; set; }  // "Monthly"
    public int? ReportMonth { get; set; }    // 1-12
    public int? ReportYear { get; set; }     // 2025
    
    // EDITABLE SECTIONS (Club Manager có thể sửa)
    public string? EventMediaUrls { get; set; }  // JSON array
    public string? NextMonthPurposeAndSignificance { get; set; }  // JSON
    public string? ClubResponsibilities { get; set; }  // JSON
    
    // Metadata
    public string? ReportActivityIds { get; set; }  // JSON array [123,124,125]
    public string? ReportSnapshot { get; set; }     // Summary data
    public string? RejectionReason { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedById { get; set; }
}
```

---

## 🔄 QUY TRÌNH TẠO VÀ CẬP NHẬT BÁO CÁO

### **1. Tạo báo cáo (Tự động hoặc thủ công)**

```csharp
// Background Service tự động tạo vào 00:00 ngày 1 hàng tháng
var plan = new Plan {
    ClubId = clubId,
    Title = $"Báo cáo tháng {month}/{year}",
    Description = $"Báo cáo hoạt động tháng {month} và kế hoạch tháng {nextMonth}",
    Status = "Draft",
    ReportType = "Monthly",
    ReportMonth = month,
    ReportYear = year,
    CreatedAt = DateTime.UtcNow
};
```

### **2. Lấy dữ liệu báo cáo (Khi xem chi tiết)**

```csharp
// GET /api/monthly-reports/{id}
public async Task<MonthlyReportDto> GetReportWithFreshDataAsync(int reportId)
{
    var plan = await _reportRepo.GetByIdAsync(reportId);
    
    // Build DTO with FRESH data from database
    var dto = new MonthlyReportDto {
        // Header
        Header = BuildHeader(plan),
        
        // Part A: Current Month (TỰ ĐỘNG từ Activities)
        CurrentMonthActivities = new CurrentMonthActivitiesDto {
            SchoolEvents = await _dataAggregator.GetSchoolEventsAsync(clubId, reportMonth, reportYear),
            SupportActivities = await _dataAggregator.GetSupportActivitiesAsync(clubId, reportMonth, reportYear),
            Competitions = await _dataAggregator.GetCompetitionsAsync(clubId, reportMonth, reportYear),
            InternalMeetings = await _dataAggregator.GetInternalMeetingsAsync(clubId, reportMonth, reportYear)
        },
        
        // Part B: Next Month (MIX: Tự động + Editable)
        NextMonthPlans = await _dataAggregator.GetNextMonthPlansAsync(
            clubId, reportMonth, reportYear, nextMonth, nextYear
        ),
        
        // Footer
        Footer = BuildFooter(plan)
    };
    
    return dto;
}
```

**Lưu ý quan trọng:**
- Dữ liệu **LUÔN ĐƯỢC LẤY FRESH** từ database khi xem báo cáo
- Không lưu snapshot cố định (trừ khi cần thiết)
- Điều này đảm bảo báo cáo luôn cập nhật với dữ liệu mới nhất

### **3. Chỉnh sửa báo cáo (Club Manager)**

```csharp
// PUT /api/monthly-reports/{id}
public async Task UpdateReportAsync(int reportId, UpdateMonthlyReportDto dto)
{
    var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == reportId);
    
    // Chỉ cho phép sửa khi status = Draft hoặc Rejected
    if (plan.Status != "Draft" && plan.Status != "Rejected") {
        throw new InvalidOperationException("Cannot update approved report");
    }
    
    // Cập nhật các phần EDITABLE
    if (dto.EventMediaUrls != null) {
        plan.EventMediaUrls = dto.EventMediaUrls;  // JSON array
    }
    
    if (dto.NextMonthPurposeAndSignificance != null) {
        plan.NextMonthPurposeAndSignificance = dto.NextMonthPurposeAndSignificance;  // JSON
    }
    
    if (dto.ClubResponsibilities != null) {
        plan.ClubResponsibilities = dto.ClubResponsibilities;  // JSON
    }
    
    await _reportRepo.UpdateAsync(plan);
}
```

### **4. Nộp báo cáo**

```csharp
// POST /api/monthly-reports/{id}/submit
public async Task SubmitReportAsync(int reportId, int userId)
{
    var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == reportId);
    
    // Chuyển status
    plan.Status = "PendingApproval";
    plan.SubmittedAt = DateTime.UtcNow;
    
    await _reportRepo.UpdateAsync(plan);
    
    // Gửi notification cho tất cả Admin
    var admins = await _context.Users
        .Where(u => u.Role.RoleName == "Admin" && u.IsActive)
        .ToListAsync();
    
    foreach (var admin in admins) {
        await _notificationService.SendNotificationAsync(
            admin.Id,
            "MonthlyReportSubmitted",
            $"Báo cáo tháng {plan.ReportMonth}/{plan.ReportYear} từ CLB {plan.Club.Name} đã được nộp",
            reportId
        );
    }
}
```

---

## 📊 LUỒNG DỮ LIỆU (DATA FLOW)

```
┌─────────────────────────────────────────────────────────────┐
│                    MONTHLY REPORT                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────────┐
        │         Plans Table (Storage)          │
        │  - ReportType = "Monthly"              │
        │  - ReportMonth, ReportYear             │
        │  - EventMediaUrls (JSON)               │
        │  - NextMonthPurposeAndSignificance     │
        │  - ClubResponsibilities                │
        └───────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────────┐
        │    MonthlyReportDataAggregator         │
        │    (Lấy dữ liệu từ nhiều nguồn)        │
        └───────────────────────────────────────┘
                            │
        ┌───────────────────┴───────────────────┐
        │                                        │
        ▼                                        ▼
┌──────────────────┐                  ┌──────────────────┐
│  PART A (Auto)   │                  │  PART B (Mix)    │
│  Current Month   │                  │  Next Month      │
└──────────────────┘                  └──────────────────┘
        │                                        │
        ├─> Activities                           ├─> Activities (Next Month)
        │   - SchoolEvents                       │   - PlannedEvents
        │   - SupportActivities                  │   - PlannedCompetitions
        │   - Competitions                       │
        │   - InternalMeetings                   ├─> CommunicationPlans
        │                                        │
        ├─> ActivityAttendances                  ├─> Plans (Editable)
        │   - Participants                       │   - Purpose
        │   - Ratings                            │   - Responsibilities
        │                                        │
        ├─> ActivityEvaluations                  └─> Manual Input
        │   - Scores                                 - Budget
        │   - Feedback                               - Facility
        │
        ├─> ActivityMemberEvaluations
        │   - SupportMembers
        │   - Ratings
        │
        └─> ActivitySchedules
            - Timeline
```

---

## 🎯 ĐIỂM QUAN TRỌNG

### ✅ **Dữ liệu TỰ ĐỘNG (Không cần nhập)**
1. Tất cả hoạt động tháng hiện tại (Part A)
2. Danh sách người tham gia
3. Điểm đánh giá
4. Timeline sự kiện
5. Hoạt động dự kiến tháng sau
6. Kế hoạch truyền thông

### ✏️ **Dữ liệu EDITABLE (Club Manager có thể sửa)**
1. Media URLs (hình ảnh, video sự kiện)
2. Mục đích và ý nghĩa tháng sau
3. Trách nhiệm của CLB

### 📝 **Dữ liệu MANUAL (Cần nhập thủ công - TODO)**
1. Ngân sách chi tiết
2. Cơ sở vật chất
3. Thành tích cuộc thi (Achievement)
4. URL đăng ký sự kiện
5. Danh sách khách mời

---

## 🔍 QUERY EXAMPLES

### Lấy tất cả School Events của tháng 11/2025:
```sql
SELECT a.*, 
       att.*, 
       eval.*
FROM Activities a
LEFT JOIN ActivityAttendances att ON a.Id = att.ActivityId
LEFT JOIN ActivityEvaluations eval ON a.Id = eval.ActivityId
WHERE a.ClubId = 1
  AND MONTH(a.StartTime) = 11
  AND YEAR(a.StartTime) = 2025
  AND a.Type IN ('LargeEvent', 'MediumEvent', 'SmallEvent')
ORDER BY a.StartTime
```

### Lấy kế hoạch truyền thông tháng 12/2025:
```sql
SELECT cp.*, ci.*
FROM CommunicationPlans cp
JOIN CommunicationItems ci ON cp.Id = ci.CommunicationPlanId
WHERE cp.ClubId = 1
  AND cp.Month = 12
  AND cp.Year = 2025
ORDER BY ci.ScheduledDate
```

---

## 📈 PERFORMANCE OPTIMIZATION

### Các bảng cần index:
```sql
-- Activities
CREATE INDEX IX_Activities_ClubId_StartTime_Type 
ON Activities(ClubId, StartTime, Type);

-- ActivityAttendances
CREATE INDEX IX_ActivityAttendances_ActivityId_IsPresent 
ON ActivityAttendances(ActivityId, IsPresent);

-- Plans
CREATE INDEX IX_Plans_ClubId_ReportType_ReportMonth_ReportYear 
ON Plans(ClubId, ReportType, ReportMonth, ReportYear);

-- CommunicationPlans
CREATE INDEX IX_CommunicationPlans_ClubId_Month_Year 
ON CommunicationPlans(ClubId, Month, Year);
```

---

## 🚀 FUTURE IMPROVEMENTS

1. **Thêm bảng Budget** để quản lý ngân sách chi tiết
2. **Thêm bảng Facility** để quản lý cơ sở vật chất
3. **Thêm field Achievement** trong Activities/Attendances
4. **Thêm field RegistrationUrl** trong Activities
5. **Thêm bảng Guests** để quản lý khách mời
6. **Cache dữ liệu** để tăng performance khi xem báo cáo nhiều lần
7. **Snapshot mechanism** để lưu trữ báo cáo đã approved (không thay đổi)

---

## 📞 CONTACT

Nếu có thắc mắc về cách lấy dữ liệu hoặc cần thêm field mới, vui lòng liên hệ team development.
