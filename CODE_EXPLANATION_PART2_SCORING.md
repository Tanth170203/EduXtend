# 📊 GIẢI THÍCH CODE: SCORING SYSTEM (Hệ thống chấm điểm)

## 🎯 Tổng quan

Hệ thống chấm điểm tự động cho Sinh viên và CLB dựa trên hoạt động, đánh giá, và minh chứng.

---

## 1️⃣ COMPREHENSIVE AUTO SCORING SERVICE

### **File:** `Services/MovementRecords/ComprehensiveAutoScoringService.cs`

```csharp
public class ComprehensiveAutoScoringService : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Chạy mỗi ngày lúc 02:00 sáng
                var now = DateTime.Now;
                var nextRun = CalculateNextRunTime(now);
                var delay = nextRun - now;
                
                await Task.Delay(delay, stoppingToken);
                
                // Chấm điểm tự động
                await AutoScoreAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-scoring");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
```

**Giải thích:**
- Chạy mỗi ngày lúc 02:00 sáng
- Tự động chấm điểm cho tất cả sinh viên và CLB
- Retry sau 1 giờ nếu có lỗi

### **Logic chấm điểm:**

```csharp
private async Task AutoScoreAllAsync()
{
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider
        .GetRequiredService<EduXtendContext>();
    
    // 1. Lấy học kỳ hiện tại
    var currentSemester = await context.Semesters
        .FirstOrDefaultAsync(s => s.IsCurrent);
    
    if (currentSemester == null) return;
    
    // 2. Lấy tất cả sinh viên active
    var students = await context.Students
        .Where(s => s.Status == StudentStatus.Active)
        .ToListAsync();
    
    // 3. Chấm điểm cho từng sinh viên
    foreach (var student in students)
    {
        await ScoreStudentAsync(
            student.Id, 
            currentSemester.Id, 
            context
        );
    }
    
    // 4. Chấm điểm cho tất cả CLB
    var clubs = await context.Clubs
        .Where(c => c.IsActive)
        .ToListAsync();
    
    foreach (var club in clubs)
    {
        await ScoreClubAsync(
            club.Id, 
            currentSemester.Id, 
            context
        );
    }
}
```

**Giải thích:**
1. Lấy học kỳ hiện tại
2. Lấy danh sách sinh viên active
3. Chấm điểm từng sinh viên
4. Chấm điểm từng CLB

---

## 2️⃣ STUDENT SCORING - Chấm điểm sinh viên

### **Logic chấm điểm sinh viên:**

```csharp
private async Task ScoreStudentAsync(
    int studentId, 
    int semesterId, 
    EduXtendContext context)
{
    // 1. Lấy tất cả tiêu chí
    var criteria = await context.MovementCriteria
        .Where(c => c.IsActive)
        .ToListAsync();
    
    // 2. Tính điểm cho từng hạng mục
    var academicScore = await CalculateAcademicScoreAsync(
        studentId, semesterId, criteria, context
    );
    
    var socialScore = await CalculateSocialScoreAsync(
        studentId, semesterId, criteria, context
    );
    
    var civicScore = await CalculateCivicScoreAsync(
        studentId, semesterId, criteria, context
    );
    
    var organizationalScore = await CalculateOrganizationalScoreAsync(
        studentId, semesterId, criteria, context
    );
    
    // 3. Tổng điểm
    var totalScore = academicScore + socialScore + 
                     civicScore + organizationalScore;
    
    // 4. Lưu hoặc cập nhật MovementRecord
    var existingRecord = await context.MovementRecords
        .FirstOrDefaultAsync(r => 
            r.StudentId == studentId && 
            r.SemesterId == semesterId
        );
    
    if (existingRecord == null)
    {
        // Tạo mới
        context.MovementRecords.Add(new MovementRecord {
            StudentId = studentId,
            SemesterId = semesterId,
            AcademicScore = academicScore,
            SocialScore = socialScore,
            CivicScore = civicScore,
            OrganizationalScore = organizationalScore,
            TotalScore = totalScore,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        // Cập nhật
        existingRecord.AcademicScore = academicScore;
        existingRecord.SocialScore = socialScore;
        existingRecord.CivicScore = civicScore;
        existingRecord.OrganizationalScore = organizationalScore;
        existingRecord.TotalScore = totalScore;
        existingRecord.UpdatedAt = DateTime.UtcNow;
    }
    
    await context.SaveChangesAsync();
}
```

**Giải thích:**
1. Lấy tất cả tiêu chí đang active
2. Tính điểm cho 4 hạng mục:
   - Academic (Học tập)
   - Social (Hoạt động xã hội)
   - Civic (Phẩm chất công dân)
   - Organizational (Công tác tổ chức)
3. Tổng điểm = Tổng 4 hạng mục
4. Lưu vào `MovementRecords`

### **Tính điểm Academic (Học tập):**

```csharp
private async Task<decimal> CalculateAcademicScoreAsync(
    int studentId, 
    int semesterId, 
    List<MovementCriterion> criteria,
    EduXtendContext context)
{
    decimal score = 0;
    
    // Lọc tiêu chí Academic
    var academicCriteria = criteria
        .Where(c => c.Category == "Academic")
        .ToList();
    
    foreach (var criterion in academicCriteria)
    {
        // Kiểm tra sinh viên có đáp ứng tiêu chí không
        var meetsRequirement = await CheckCriterionAsync(
            studentId, 
            semesterId, 
            criterion, 
            context
        );
        
        if (meetsRequirement)
        {
            score += criterion.Points;
        }
    }
    
    return score;
}
```

**Giải thích:**
- Lọc tiêu chí thuộc category "Academic"
- Với mỗi tiêu chí:
  - Check sinh viên có đáp ứng không
  - Nếu có → Cộng điểm
- Return tổng điểm Academic

### **Check tiêu chí:**

```csharp
private async Task<bool> CheckCriterionAsync(
    int studentId, 
    int semesterId, 
    MovementCriterion criterion,
    EduXtendContext context)
{
    // Ví dụ: Tiêu chí "Tham gia ít nhất 5 hoạt động"
    if (criterion.Code == "ACTIVITY_COUNT_5")
    {
        var activityCount = await context.ActivityAttendances
            .Where(a => a.UserId == GetUserId(studentId, context)
                && a.IsPresent
                && a.Activity.StartTime >= GetSemesterStart(semesterId)
                && a.Activity.StartTime <= GetSemesterEnd(semesterId))
            .CountAsync();
        
        return activityCount >= 5;
    }
    
    // Ví dụ: Tiêu chí "Có minh chứng được duyệt"
    if (criterion.Code == "EVIDENCE_APPROVED")
    {
        var hasApprovedEvidence = await context.Evidences
            .AnyAsync(e => e.StudentId == studentId
                && e.Status == "Approved"
                && e.SemesterId == semesterId);
        
        return hasApprovedEvidence;
    }
    
    // ... Các tiêu chí khác
    
    return false;
}
```

**Giải thích:**
- Mỗi tiêu chí có logic check riêng
- Ví dụ:
  - Đếm số hoạt động tham gia
  - Check có minh chứng được duyệt
  - Check điểm đánh giá
- Return true/false

---

## 3️⃣ CLUB SCORING - Chấm điểm CLB

### **Logic chấm điểm CLB:**

```csharp
private async Task ScoreClubAsync(
    int clubId, 
    int semesterId, 
    EduXtendContext context)
{
    // 1. Lấy tiêu chí cho CLB
    var criteria = await context.MovementCriteria
        .Where(c => c.IsActive && c.AppliesTo == "Club")
        .ToListAsync();
    
    // 2. Tính điểm
    decimal totalScore = 0;
    
    foreach (var criterion in criteria)
    {
        var meetsRequirement = await CheckClubCriterionAsync(
            clubId, 
            semesterId, 
            criterion, 
            context
        );
        
        if (meetsRequirement)
        {
            totalScore += criterion.Points;
        }
    }
    
    // 3. Lưu vào ClubMovementRecord
    var existingRecord = await context.ClubMovementRecords
        .FirstOrDefaultAsync(r => 
            r.ClubId == clubId && 
            r.SemesterId == semesterId
        );
    
    if (existingRecord == null)
    {
        context.ClubMovementRecords.Add(new ClubMovementRecord {
            ClubId = clubId,
            SemesterId = semesterId,
            TotalScore = totalScore,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        existingRecord.TotalScore = totalScore;
        existingRecord.UpdatedAt = DateTime.UtcNow;
    }
    
    await context.SaveChangesAsync();
}
```

**Giải thích:**
- Tương tự Student Scoring
- Lưu vào `ClubMovementRecords`
- Tiêu chí dành cho CLB (AppliesTo = "Club")

### **Check tiêu chí CLB:**

```csharp
private async Task<bool> CheckClubCriterionAsync(
    int clubId, 
    int semesterId, 
    MovementCriterion criterion,
    EduXtendContext context)
{
    // Ví dụ: "Tổ chức ít nhất 10 hoạt động"
    if (criterion.Code == "CLUB_ACTIVITY_COUNT_10")
    {
        var activityCount = await context.Activities
            .Where(a => a.ClubId == clubId
                && a.Status == "Completed"
                && a.StartTime >= GetSemesterStart(semesterId)
                && a.StartTime <= GetSemesterEnd(semesterId))
            .CountAsync();
        
        return activityCount >= 10;
    }
    
    // Ví dụ: "Có ít nhất 50 thành viên"
    if (criterion.Code == "CLUB_MEMBER_COUNT_50")
    {
        var memberCount = await context.ClubMembers
            .Where(m => m.ClubId == clubId && m.IsActive)
            .CountAsync();
        
        return memberCount >= 50;
    }
    
    return false;
}
```

**Giải thích:**
- Check các tiêu chí dành cho CLB
- Ví dụ:
  - Số hoạt động tổ chức
  - Số thành viên
  - Điểm đánh giá trung bình

---

## 4️⃣ MOVEMENT CRITERIA - Quản lý tiêu chí

### **Model:**

```csharp
public class MovementCriterion
{
    public int Id { get; set; }
    public string Code { get; set; }        // Mã tiêu chí
    public string Name { get; set; }        // Tên tiêu chí
    public string Description { get; set; } // Mô tả
    public string Category { get; set; }    // Academic/Social/Civic/Organizational
    public decimal Points { get; set; }     // Điểm
    public string AppliesTo { get; set; }   // Student/Club
    public bool IsActive { get; set; }      // Đang hoạt động?
    public int GroupId { get; set; }        // Nhóm tiêu chí
}
```

**Giải thích:**
- `Code`: Mã duy nhất để identify tiêu chí
- `Category`: Phân loại (4 hạng mục)
- `Points`: Điểm được cộng nếu đáp ứng
- `AppliesTo`: Áp dụng cho Student hay Club
- `IsActive`: Có đang sử dụng không

---

## 🔄 WORKFLOW TỔNG THỂ

```
1. Background Service (02:00 hàng ngày)
   ↓
2. Lấy học kỳ hiện tại
   ↓
3. Lấy danh sách sinh viên active
   ↓
4. Với mỗi sinh viên:
   - Lấy tiêu chí active
   - Tính điểm 4 hạng mục
   - Lưu vào MovementRecords
   ↓
5. Lấy danh sách CLB active
   ↓
6. Với mỗi CLB:
   - Lấy tiêu chí cho CLB
   - Tính tổng điểm
   - Lưu vào ClubMovementRecords
   ↓
7. Admin/Student có thể xem điểm
```

---

## 💡 ĐIỂM QUAN TRỌNG

1. **Tự động chấm:** Chạy mỗi ngày lúc 02:00
2. **4 hạng mục:** Academic, Social, Civic, Organizational
3. **Tiêu chí linh hoạt:** Có thể thêm/sửa/xóa tiêu chí
4. **Cập nhật liên tục:** Điểm được cập nhật mỗi ngày
5. **Áp dụng cho cả Student và Club**

Hệ thống này giúp đánh giá tự động và công bằng! 🎯
