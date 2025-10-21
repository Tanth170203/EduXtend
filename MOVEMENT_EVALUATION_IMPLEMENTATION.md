# MOVEMENT EVALUATION SYSTEM - IMPLEMENTATION GUIDE

## 📋 TỔNG QUAN

Hệ thống **Movement Evaluation** (Đánh giá Điểm Phong trào) đã được triển khai đầy đủ theo Quyết định 414 của Trường ĐH FPT, bao gồm đánh giá điểm phong trào cho sinh viên và CLB.

## 🎯 CÁC CHỨC NĂNG ĐÃ TRIỂN KHAI

### ✅ 1. QUẢN LÝ TIÊU CHÍ ĐÁNH GIÁ (Criteria Management)

**Location**: `/Admin/Criteria`

**Chức năng**:
- Tạo/Sửa/Xóa nhóm tiêu chí (MovementCriterionGroup)
- Tạo/Sửa/Xóa tiêu chí đánh giá (MovementCriterion)
- Phân loại tiêu chí: Student hoặc Club
- Bật/Tắt tiêu chí hoạt động
- Xem chi tiết từng nhóm tiêu chí

**API Endpoints**:
```
GET    /api/movement-criterion-groups
GET    /api/movement-criterion-groups/{id}
GET    /api/movement-criterion-groups/{id}/detail
POST   /api/movement-criterion-groups
PUT    /api/movement-criterion-groups/{id}
DELETE /api/movement-criterion-groups/{id}

GET    /api/movement-criteria
GET    /api/movement-criteria/{id}
GET    /api/movement-criteria/by-target-type/{type}
GET    /api/movement-criteria/active
POST   /api/movement-criteria
PUT    /api/movement-criteria/{id}
DELETE /api/movement-criteria/{id}
PATCH  /api/movement-criteria/{id}/toggle-active
```

---

### ✅ 2. QUẢN LÝ MINH CHỨNG (Evidence Management)

**Admin Location**: `/Admin/Evidences`
**Student Location**: `/Student/MyEvidences`

**Chức năng Admin**:
- Xem tất cả minh chứng (All/Pending/Approved/Rejected)
- Duyệt minh chứng: Approve/Reject
- Cho điểm phong trào khi duyệt
- Thêm ghi chú reviewer
- Xóa minh chứng

**Chức năng Student**:
- Nộp minh chứng mới
- Xem danh sách minh chứng của mình
- Xem trạng thái duyệt
- Xóa minh chứng đang chờ duyệt
- Upload file lên Google Drive và đính kèm link

**API Endpoints**:
```
GET    /api/evidences                    [Admin]
GET    /api/evidences/pending            [Admin]
GET    /api/evidences/status/{status}    [Admin]
GET    /api/evidences/student/{id}       [Student/Admin]
GET    /api/evidences/{id}               [All]
POST   /api/evidences                    [Student]
PUT    /api/evidences/{id}               [Student]
POST   /api/evidences/{id}/review        [Admin]
DELETE /api/evidences/{id}               [All]
GET    /api/evidences/stats/pending-count [Admin]
```

**Workflow**:
1. Sinh viên nộp minh chứng → Status = "Pending"
2. Admin xem và duyệt → Status = "Approved/Rejected"
3. Nếu Approved và có điểm → Tự động cộng vào MovementRecord

---

### ✅ 3. QUẢN LÝ ĐIỂM PHONG TRÀO (Movement Records)

**Admin Location**: `/Admin/MovementReports`
**Student Location**: `/Student/MyScore`

**Chức năng Admin**:
- Xem tất cả bản ghi điểm phong trào
- Lọc theo học kỳ
- Xem chi tiết điểm của từng sinh viên
- Thêm điểm thủ công
- Điều chỉnh tổng điểm
- Xem top scorers
- Thống kê điểm trung bình, cao nhất, thấp nhất

**Chức năng Student**:
- Xem tổng quan điểm phong trào của mình
- Xem điểm theo từng học kỳ
- Xem chi tiết điểm theo từng tiêu chí
- Biểu đồ tiến độ
- Đánh giá: Xuất sắc/Khá/Cần cố gắng

**API Endpoints**:
```
GET    /api/movement-records                               [Admin]
GET    /api/movement-records/student/{id}                  [Student/Admin]
GET    /api/movement-records/semester/{id}                 [Admin]
GET    /api/movement-records/{id}                          [All]
GET    /api/movement-records/{id}/detailed                 [All]
GET    /api/movement-records/student/{id}/summary          [Student/Admin]
GET    /api/movement-records/semester/{id}/top/{count}     [Admin]
POST   /api/movement-records                               [Admin]
POST   /api/movement-records/add-score                     [Admin]
PATCH  /api/movement-records/{id}/adjust-score            [Admin]
DELETE /api/movement-records/{id}                          [Admin]
```

---

### ✅ 4. TỰ ĐỘNG TÍNH ĐIỂM (Automation)

**Background Service**: `MovementScoreAutomationService`

**Chức năng**:
- Tự động chạy mỗi 6 giờ
- Tính điểm từ điểm danh hoạt động (ActivityAttendance)
- Tự động tạo MovementRecord nếu chưa có
- Cộng điểm tự động khi hoạt động hoàn thành
- Cap điểm tối đa 140 theo quy định

**Manual Calculation Service**: `IMovementScoreCalculationService`

**Methods**:
- `CalculateClubMemberScoreAsync()` - Tính điểm thành viên CLB
- `AddClubMembershipScoreAsync()` - Cộng điểm thành viên CLB
- `RecalculateStudentScoreAsync()` - Tính lại tổng điểm

**Điểm theo vai trò CLB**:
- President: 10 điểm
- VicePresident: 8 điểm
- Manager: 5 điểm
- Member: 3 điểm
- Other: 1 điểm

---

### ✅ 5. DASHBOARD STATISTICS

**Location**: `/Admin/Dashboard`

**Thống kê hiển thị**:
- **Pending Evidences**: Số minh chứng chờ duyệt (với link review)
- **Total Records**: Tổng số bản ghi điểm phong trào
- **Average Score**: Điểm trung bình của tất cả sinh viên
- **Top Scorer**: Điểm cao nhất

**Top 5 Students Table**:
- Xếp hạng top 5 sinh viên điểm cao nhất
- Hiển thị thông tin: Student, Code, Semester, Score
- Link xem chi tiết
- Badge đặc biệt cho top 3

---

## 🗂️ KIẾN TRÚC HỆ THỐNG

### Database Models

```
MovementCriterionGroup (Nhóm tiêu chí)
├── Id
├── Name
├── Description
├── MaxScore
├── TargetType (Student/Club)
└── Criteria[]

MovementCriterion (Tiêu chí)
├── Id
├── GroupId
├── Title
├── Description
├── MaxScore
├── TargetType
├── DataSource
├── IsActive
├── RecordDetails[]
└── Evidences[]

Evidence (Minh chứng)
├── Id
├── StudentId
├── ActivityId (optional)
├── CriterionId (optional)
├── Title
├── Description
├── FilePath
├── Status (Pending/Approved/Rejected)
├── ReviewerComment
├── ReviewedById
├── ReviewedAt
├── Points
└── SubmittedAt

MovementRecord (Bản ghi điểm)
├── Id
├── StudentId
├── SemesterId
├── TotalScore
├── CreatedAt
├── LastUpdated
└── Details[]

MovementRecordDetail (Chi tiết điểm)
├── Id
├── MovementRecordId
├── CriterionId
├── Score
└── AwardedAt
```

### Layers

```
WebAPI/Controllers
├── EvidenceController
├── MovementRecordController
├── MovementCriterionController
└── MovementCriterionGroupController

Services
├── EvidenceService
├── MovementRecordService
├── MovementCriterionService
├── MovementCriterionGroupService
├── MovementScoreAutomationService (Background)
└── MovementScoreCalculationService (Helper)

Repositories
├── EvidenceRepository
├── MovementRecordRepository
├── MovementRecordDetailRepository
├── MovementCriterionRepository
└── MovementCriterionGroupRepository

DTOs
├── Evidence/
│   ├── EvidenceDto
│   ├── CreateEvidenceDto
│   ├── UpdateEvidenceDto
│   ├── ReviewEvidenceDto
│   └── EvidenceFilterDto
└── MovementRecord/
    ├── MovementRecordDto
    ├── MovementRecordDetailedDto
    ├── MovementRecordDetailItemDto
    ├── CreateMovementRecordDto
    ├── AddScoreDto
    ├── AdjustScoreDto
    └── StudentMovementSummaryDto
```

---

## 📝 HƯỚNG DẪN SỬ DỤNG

### A. Cho Admin/CTSV

#### 1. Cấu hình tiêu chí đánh giá

```
1. Truy cập: /Admin/Criteria
2. Tạo nhóm tiêu chí:
   - Click "Thêm nhóm tiêu chí"
   - Nhập tên: VD "1. BÁO CÁO"
   - Chọn Target Type: Student hoặc Club
   - Nhập điểm tối đa: VD 20
   
3. Thêm tiêu chí vào nhóm:
   - Click vào nhóm vừa tạo
   - Thêm tiêu chí con
   - VD: "Hoạt động sinh hoạt CLB" - Max 20 điểm
```

#### 2. Duyệt minh chứng

```
1. Truy cập: /Admin/Evidences
2. Chọn tab "Chờ duyệt"
3. Click "Duyệt" trên minh chứng cần xem
4. Chọn:
   - Status: Approved/Rejected
   - Points: Điểm cộng (nếu Approved)
   - Comment: Ghi chú
5. Click "Xác nhận"
→ Điểm tự động cộng vào MovementRecord
```

#### 3. Xem báo cáo điểm

```
1. Truy cập: /Admin/MovementReports
2. Lọc theo học kỳ (optional)
3. Xem danh sách sinh viên và điểm
4. Click "Xem chi tiết" để xem chi tiết điểm theo tiêu chí
```

#### 4. Cộng điểm thủ công

```
POST /api/movement-records/add-score
{
  "movementRecordId": 1,
  "criterionId": 5,
  "score": 10
}
```

---

### B. Cho Sinh viên

#### 1. Nộp minh chứng

```
1. Truy cập: /Student/MyEvidences
2. Click "Nộp minh chứng mới"
3. Điền thông tin:
   - Tiêu đề: VD "Chứng nhận hiến máu"
   - Tiêu chí: Chọn từ dropdown
   - Mô tả: Mô tả chi tiết
   - Link file: Upload lên Drive và paste link
4. Click "Nộp minh chứng"
→ Chờ Admin duyệt
```

#### 2. Xem điểm phong trào

```
1. Truy cập: /Student/MyScore
2. Xem tổng quan:
   - Điểm trung bình
   - Điểm cao nhất/thấp nhất
   - Tổng học kỳ
3. Xem điểm theo từng học kỳ
4. Click "Xem chi tiết" để xem điểm theo tiêu chí
```

---

## ⚙️ CẤU HÌNH VÀ DEPLOYMENT

### 1. Dependencies đã được thêm

```csharp
// Program.cs đã được cập nhật với:

// Repositories
builder.Services.AddScoped<IEvidenceRepository, EvidenceRepository>();
builder.Services.AddScoped<IMovementRecordRepository, MovementRecordRepository>();
builder.Services.AddScoped<IMovementRecordDetailRepository, MovementRecordDetailRepository>();

// Services
builder.Services.AddScoped<IEvidenceService, EvidenceService>();
builder.Services.AddScoped<IMovementRecordService, MovementRecordService>();
builder.Services.AddScoped<IMovementScoreCalculationService, MovementScoreCalculationService>();

// Background Services
builder.Services.AddHostedService<MovementScoreAutomationService>();
```

### 2. Database Migration

**Lưu ý**: Tables đã tồn tại từ migration trước:
- MovementCriterionGroups
- MovementCriteria
- Evidences
- MovementRecords
- MovementRecordDetails

Không cần migration mới.

### 3. Seed Data mẫu

**Tạo tiêu chí mẫu theo Quyết định 414**:

```sql
-- Nhóm 1: ĐÁNH GIÁ VỀ Ý THỨC HỌC TẬP (20-35 điểm)
INSERT INTO MovementCriterionGroups (Name, Description, MaxScore, TargetType)
VALUES ('1. ĐÁNH GIÁ VỀ Ý THỨC HỌC TẬP', 'Đánh giá ý thức và kết quả học tập', 35, 'Student');

INSERT INTO MovementCriteria (GroupId, Title, MaxScore, TargetType, IsActive)
VALUES 
(1, 'Tuyên dương công khai trước lớp', 2, 'Student', 1),
(1, 'Tham gia Olympic/ACM/CPC/Robocon', 10, 'Student', 1),
(1, 'Tham gia cuộc thi cấp trường', 5, 'Student', 1);

-- Nhóm 2: HOẠT ĐỘNG CHÍNH TRỊ - XÃ HỘI (15-50 điểm)
INSERT INTO MovementCriterionGroups (Name, Description, MaxScore, TargetType)
VALUES ('2. HOẠT ĐỘNG CHÍNH TRỊ - XÃ HỘI', 'Văn hóa, văn nghệ, thể thao', 50, 'Student');

INSERT INTO MovementCriteria (GroupId, Title, MaxScore, TargetType, IsActive)
VALUES 
(2, 'Tham gia sự kiện', 5, 'Student', 1),
(2, 'Thành viên CLB', 10, 'Student', 1);

-- Nhóm 3: PHẨM CHẤT CÔNG DÂN (15-25 điểm)
INSERT INTO MovementCriterionGroups (Name, Description, MaxScore, TargetType)
VALUES ('3. PHẨM CHẤT CÔNG DÂN', 'Quan hệ với cộng đồng', 25, 'Student');

INSERT INTO MovementCriteria (GroupId, Title, MaxScore, TargetType, IsActive)
VALUES 
(3, 'Hành vi tốt được ghi nhận', 5, 'Student', 1),
(3, 'Hoạt động từ thiện, tình nguyện', 5, 'Student', 1);

-- Nhóm 4: CÔNG TÁC PHỤ TRÁCH (10-30 điểm)
INSERT INTO MovementCriterionGroups (Name, Description, MaxScore, TargetType)
VALUES ('4. CÔNG TÁC PHỤ TRÁCH', 'Lớp, đoàn thể, tổ chức', 30, 'Student');

INSERT INTO MovementCriteria (GroupId, Title, MaxScore, TargetType, IsActive)
VALUES 
(4, 'Chủ nhiệm CLB/Trưởng BTC', 10, 'Student', 1),
(4, 'Lớp trưởng, BCH Đoàn/Hội', 10, 'Student', 1);
```

---

## 🔄 WORKFLOW TỰ ĐỘNG

### 1. Khi sinh viên điểm danh hoạt động

```
Activity.Status = "Completed" && Activity.MovementPoint > 0
↓
MovementScoreAutomationService (chạy mỗi 6h)
↓
Lấy tất cả ActivityAttendance.IsPresent = true
↓
Tạo/Cập nhật MovementRecord cho học kỳ hiện tại
↓
Thêm MovementRecordDetail với điểm = Activity.MovementPoint
↓
Cập nhật TotalScore (cap tối đa 140)
```

### 2. Khi Admin duyệt minh chứng

```
Admin click Review → Status = "Approved", Points = X
↓
EvidenceService.ReviewAsync()
↓
_movementRecordService.AddScoreFromEvidenceAsync()
↓
Tạo/Cập nhật MovementRecord
↓
Thêm MovementRecordDetail
↓
Cập nhật TotalScore (cap tối đa 140)
```

---

## 📊 BÁO CÁO VÀ THỐNG KÊ

### Các thống kê có sẵn:

1. **Dashboard Statistics**:
   - Pending evidences count
   - Total movement records
   - Average movement score
   - Top 5 scorers

2. **Movement Reports**:
   - List all students by score
   - Filter by semester
   - Top scorers ranking
   - Score distribution

3. **Student Summary**:
   - Total semesters
   - Average score
   - Highest/Lowest score
   - Progress chart

---

## 🎨 UI/UX FEATURES

- **Responsive Design**: Hoạt động tốt trên mobile/tablet/desktop
- **Real-time Stats**: Cập nhật số liệu real-time
- **Progress Bars**: Hiển thị % hoàn thành
- **Badge System**: Top 3 có badge đặc biệt
- **Color Coding**: 
  - Green (≥80): Xuất sắc
  - Orange (60-79): Khá
  - Red (<60): Cần cố gắng
- **Icons**: Sử dụng Lucide Icons
- **Alerts**: Success/Error messages rõ ràng

---

## 🔐 SECURITY & AUTHORIZATION

- **Admin Only**: 
  - Create/Update/Delete Criteria
  - Review Evidence
  - Add Manual Scores
  - View All Records
  
- **Student**:
  - Submit Evidence
  - View Own Records
  - Delete Own Pending Evidence

- **Authentication**: JWT-based
- **Authorization**: Role-based (Admin, Student)

---

## 🚀 NEXT STEPS (Tùy chọn mở rộng)

1. **Export to Excel**: Xuất báo cáo điểm ra Excel
2. **Email Notifications**: Thông báo khi Evidence được duyệt
3. **File Upload**: Upload file trực tiếp thay vì link
4. **Multi-level Approval**: Workflow duyệt đa cấp
5. **Bulk Operations**: Import/Export điểm hàng loạt
6. **Analytics Dashboard**: Biểu đồ thống kê nâng cao

---

## 📞 SUPPORT

Nếu cần hỗ trợ hoặc có câu hỏi, vui lòng liên hệ team phát triển.

---

**Ngày triển khai**: October 17, 2025
**Phiên bản**: 1.0.0
**Trạng thái**: Production Ready ✅


