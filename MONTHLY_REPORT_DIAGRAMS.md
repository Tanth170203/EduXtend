# Monthly Report - Software Design Diagrams

## 1. Class Diagram

```mermaid
classDiagram
    direction TB
    
    %% Controllers
    class MonthlyReportController {
        -IMonthlyReportService _service
        -IMonthlyReportPdfService _pdfService
        -ILogger _logger
        +GetAllReports(clubId) IActionResult
        +GetReport(id) IActionResult
        +CreateReport(dto) IActionResult
        +UpdateReport(id, dto) IActionResult
        +SubmitReport(id) IActionResult
        +ExportToPdf(id) IActionResult
    }
    
    class MonthlyReportApprovalController {
        -IMonthlyReportApprovalService _approvalService
        -ILogger _logger
        +ApproveReport(id) IActionResult
        +RejectReport(id, dto) IActionResult
    }
    
    %% Service Interfaces
    class IMonthlyReportService {
        <<interface>>
        +GetAllReportsAsync(clubId) Task~List~MonthlyReportListDto~~
        +GetAllReportsForAdminAsync() Task~List~MonthlyReportListDto~~
        +GetReportByIdAsync(reportId) Task~MonthlyReportDto~
        +GetReportWithFreshDataAsync(reportId) Task~MonthlyReportDto~
        +CreateMonthlyReportAsync(clubId, month, year) Task~int~
        +UpdateReportAsync(reportId, dto) Task
        +SubmitReportAsync(reportId, userId) Task
    }
    
    class IMonthlyReportApprovalService {
        <<interface>>
        +ApproveReportAsync(reportId, adminId) Task
        +RejectReportAsync(reportId, adminId, reason) Task
    }
    
    class IMonthlyReportDataAggregator {
        <<interface>>
        +GetSchoolEventsAsync(clubId, month, year) Task~List~SchoolEventDto~~
        +GetSupportActivitiesAsync(clubId, month, year) Task~List~SupportActivityDto~~
        +GetCompetitionsAsync(clubId, month, year) Task~List~CompetitionDto~~
        +GetInternalMeetingsAsync(clubId, month, year) Task~List~InternalMeetingDto~~
        +GetNextMonthPlansAsync(clubId, reportMonth, reportYear, nextMonth, nextYear) Task~NextMonthPlansDto~
    }
    
    class IMonthlyReportPdfService {
        <<interface>>
        +ExportToPdfAsync(reportId) Task~byte[]~
    }
    
    %% Service Implementations
    class MonthlyReportService {
        -IMonthlyReportRepository _reportRepo
        -IMonthlyReportDataAggregator _dataAggregator
        -INotificationService _notificationService
        -IEmailService _emailService
        -IMonthlyReportPdfService _pdfService
        -ILogger _logger
        -EduXtendContext _context
        +GetAllReportsAsync(clubId) Task~List~MonthlyReportListDto~~
        +GetAllReportsForAdminAsync() Task~List~MonthlyReportListDto~~
        +GetReportByIdAsync(reportId) Task~MonthlyReportDto~
        +GetReportWithFreshDataAsync(reportId) Task~MonthlyReportDto~
        +CreateMonthlyReportAsync(clubId, month, year) Task~int~
        +UpdateReportAsync(reportId, dto) Task
        +SubmitReportAsync(reportId, userId) Task
        -ValidateMonthSequence(reportMonth, reportYear) string
        -BuildMonthlyReportDto(plan, includeAggregatedData) Task~MonthlyReportDto~
    }
    
    class MonthlyReportApprovalService {
        -IMonthlyReportRepository _reportRepo
        -INotificationService _notificationService
        -EduXtendContext _context
        +ApproveReportAsync(reportId, adminId) Task
        +RejectReportAsync(reportId, adminId, reason) Task
        -GetClubManagerAsync(clubId) Task~User~
    }
    
    class MonthlyReportDataAggregator {
        -IActivityRepository _activityRepo
        -IActivityMemberEvaluationRepository _memberEvalRepo
        -ICommunicationPlanRepository _communicationPlanRepo
        -EduXtendContext _context
        -ILogger _logger
        +GetSchoolEventsAsync(clubId, month, year) Task~List~SchoolEventDto~~
        +GetSupportActivitiesAsync(clubId, month, year) Task~List~SupportActivityDto~~
        +GetCompetitionsAsync(clubId, month, year) Task~List~CompetitionDto~~
        +GetInternalMeetingsAsync(clubId, month, year) Task~List~InternalMeetingDto~~
        +GetNextMonthPlansAsync(clubId, reportMonth, reportYear, nextMonth, nextYear) Task~NextMonthPlansDto~
        -GetStudentCode(userId) string
        -GetSupportMembersAsync(activityId, clubId) Task~List~SupportMemberDto~~
        -GetActivityTimelineAsync(activityId) Task~string~
    }
    
    class MonthlyReportPdfService {
        -IMonthlyReportRepository _reportRepo
        -IMonthlyReportDataAggregator _dataAggregator
        -EduXtendContext _context
        +ExportToPdfAsync(reportId) Task~byte[]~
        -BuildMonthlyReportDtoAsync(reportId) Task~MonthlyReportDto~
        -ComposeDocument(container, report) void
        -ComposeHeader(column, report) void
        -ComposePartA(column, report) void
        -ComposePartB(column, report) void
        -ComposeSignature(column, report) void
    }
    
    %% Repository
    class IMonthlyReportRepository {
        <<interface>>
        +GetAllByClubIdAsync(clubId) Task~List~Plan~~
        +GetByIdAsync(id) Task~Plan~
        +CreateAsync(plan) Task~Plan~
        +UpdateAsync(plan) Task~Plan~
        +GetByClubAndMonthAsync(clubId, month, year) Task~Plan~
    }
    
    class MonthlyReportRepository {
        -EduXtendContext _ctx
        +GetAllByClubIdAsync(clubId) Task~List~Plan~~
        +GetByIdAsync(id) Task~Plan~
        +CreateAsync(plan) Task~Plan~
        +UpdateAsync(plan) Task~Plan~
        +GetByClubAndMonthAsync(clubId, month, year) Task~Plan~
    }
    
    %% Models
    class Plan {
        +int Id
        +int ClubId
        +Club Club
        +string Title
        +string Description
        +string Status
        +DateTime CreatedAt
        +DateTime SubmittedAt
        +int ApprovedById
        +User ApprovedBy
        +DateTime ApprovedAt
        +string ReportType
        +int ReportMonth
        +int ReportYear
        +string ReportActivityIds
        +string ReportSnapshot
        +string RejectionReason
        +string EventMediaUrls
        +string NextMonthPurposeAndSignificance
        +string ClubResponsibilities
    }
    
    %% DTOs
    class MonthlyReportDto {
        +int Id
        +int ClubId
        +string ClubName
        +string DepartmentName
        +string Status
        +int ReportMonth
        +int ReportYear
        +int NextMonth
        +int NextYear
        +HeaderDto Header
        +CurrentMonthActivitiesDto CurrentMonthActivities
        +NextMonthPlansDto NextMonthPlans
        +FooterDto Footer
        +DateTime CreatedAt
        +DateTime SubmittedAt
        +DateTime ApprovedAt
        +string RejectionReason
    }
    
    class MonthlyReportListDto {
        +int Id
        +int ClubId
        +string ClubName
        +string Status
        +int ReportMonth
        +int ReportYear
        +DateTime CreatedAt
        +DateTime SubmittedAt
    }
    
    class CurrentMonthActivitiesDto {
        +List~SchoolEventDto~ SchoolEvents
        +List~SupportActivityDto~ SupportActivities
        +List~CompetitionDto~ Competitions
        +List~InternalMeetingDto~ InternalMeetings
    }
    
    class NextMonthPlansDto {
        +PurposeDto Purpose
        +List~PlannedEventDto~ PlannedEvents
        +List~PlannedCompetitionDto~ PlannedCompetitions
        +List~CommunicationItemDto~ CommunicationPlan
        +BudgetDto Budget
        +FacilityDto Facility
        +ClubResponsibilitiesDto Responsibilities
    }
    
    %% External Services
    class INotificationService {
        <<interface>>
        +SendNotificationAsync(userId, type, message, referenceId) Task
        +CreateAsync(notification) Task
    }
    
    class IEmailService {
        <<interface>>
        +SendMonthlyReportSubmissionEmailAsync(...) Task
    }
    
    %% Relationships
    MonthlyReportController --> IMonthlyReportService
    MonthlyReportController --> IMonthlyReportPdfService
    MonthlyReportApprovalController --> IMonthlyReportApprovalService
    
    IMonthlyReportService <|.. MonthlyReportService
    IMonthlyReportApprovalService <|.. MonthlyReportApprovalService
    IMonthlyReportDataAggregator <|.. MonthlyReportDataAggregator
    IMonthlyReportPdfService <|.. MonthlyReportPdfService
    IMonthlyReportRepository <|.. MonthlyReportRepository
    
    MonthlyReportService --> IMonthlyReportRepository
    MonthlyReportService --> IMonthlyReportDataAggregator
    MonthlyReportService --> INotificationService
    MonthlyReportService --> IEmailService
    MonthlyReportService --> IMonthlyReportPdfService
    
    MonthlyReportApprovalService --> IMonthlyReportRepository
    MonthlyReportApprovalService --> INotificationService
    
    MonthlyReportPdfService --> IMonthlyReportRepository
    MonthlyReportPdfService --> IMonthlyReportDataAggregator
    
    MonthlyReportRepository --> Plan
    MonthlyReportService ..> MonthlyReportDto : creates
    MonthlyReportService ..> MonthlyReportListDto : creates
    MonthlyReportDataAggregator ..> CurrentMonthActivitiesDto : creates
    MonthlyReportDataAggregator ..> NextMonthPlansDto : creates
    
    MonthlyReportDto *-- CurrentMonthActivitiesDto
    MonthlyReportDto *-- NextMonthPlansDto
```



---

## 2. Sequence Diagrams

### 2.1. Create Monthly Report

```mermaid
sequenceDiagram
    autonumber
    participant CM as ClubManager
    participant API as MonthlyReportController
    participant SVC as MonthlyReportService
    participant REPO as MonthlyReportRepository
    participant DB as Database
    participant AGG as MonthlyReportDataAggregator
    
    CM->>+API: POST /api/monthly-reports
    Note over API: CreateMonthlyReportDto {clubId, month, year}
    
    API->>API: Validate user authentication
    API->>API: Validate input (clubId, month, year)
    
    API->>+SVC: CreateMonthlyReportAsync(clubId, month, year)
    
    SVC->>SVC: ValidateMonthSequence(month, year)
    
    SVC->>+REPO: GetByClubAndMonthAsync(clubId, month, year)
    REPO->>+DB: SELECT * FROM Plans WHERE...
    DB-->>-REPO: null (no duplicate)
    REPO-->>-SVC: null
    
    SVC->>+DB: Get Club info
    DB-->>-SVC: Club entity
    
    SVC->>SVC: Create new Plan entity
    Note over SVC: Status = "Draft"<br/>ReportType = "Monthly"
    
    SVC->>+REPO: CreateAsync(plan)
    REPO->>+DB: INSERT INTO Plans
    DB-->>-REPO: Created Plan
    REPO-->>-SVC: Plan with Id
    
    SVC-->>-API: reportId
    
    API->>+SVC: GetReportWithFreshDataAsync(reportId)
    SVC->>+REPO: GetByIdAsync(reportId)
    REPO->>+DB: SELECT * FROM Plans
    DB-->>-REPO: Plan
    REPO-->>-SVC: Plan
    
    SVC->>+AGG: GetSchoolEventsAsync(clubId, month, year)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-SVC: List~SchoolEventDto~
    
    SVC->>+AGG: GetSupportActivitiesAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-SVC: List~SupportActivityDto~
    
    SVC->>+AGG: GetCompetitionsAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-SVC: List~CompetitionDto~
    
    SVC->>+AGG: GetInternalMeetingsAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-SVC: List~InternalMeetingDto~
    
    SVC->>+AGG: GetNextMonthPlansAsync(...)
    AGG->>+DB: Query Plans & Activities
    DB-->>-AGG: Data
    AGG-->>-SVC: NextMonthPlansDto
    
    SVC->>SVC: BuildMonthlyReportDto(plan, true)
    SVC-->>-API: MonthlyReportDto
    
    API-->>-CM: 201 Created + MonthlyReportDto
```

### 2.2. Submit Monthly Report for Approval

```mermaid
sequenceDiagram
    autonumber
    participant CM as ClubManager
    participant API as MonthlyReportController
    participant SVC as MonthlyReportService
    participant PDF as MonthlyReportPdfService
    participant REPO as MonthlyReportRepository
    participant DB as Database
    participant NOTIF as NotificationService
    participant EMAIL as EmailService
    
    CM->>+API: POST /api/monthly-reports/{id}/submit
    
    API->>API: Validate user authentication
    API->>API: Get userId from claims
    
    API->>+SVC: SubmitReportAsync(reportId, userId)
    
    SVC->>+DB: Get Plan with Club
    DB-->>-SVC: Plan entity
    
    SVC->>SVC: Validate Status == "Draft" or "Rejected"
    
    SVC->>SVC: Update Status = "PendingApproval"
    SVC->>SVC: Set SubmittedAt = DateTime.UtcNow
    
    SVC->>+REPO: UpdateAsync(plan)
    REPO->>+DB: UPDATE Plans
    DB-->>-REPO: Updated
    REPO-->>-SVC: Plan
    
    SVC->>+DB: Get submitter User
    DB-->>-SVC: User (submitterName)
    
    SVC->>+PDF: ExportToPdfAsync(reportId)
    activate PDF
    PDF->>PDF: Generate PDF document
    PDF-->>-SVC: byte[] pdfData
    deactivate PDF
    
    SVC->>+DB: Query Admin users
    DB-->>-SVC: List~User~ admins
    
    loop For each Admin
        SVC->>+NOTIF: SendNotificationAsync(adminId, "MonthlyReportSubmitted", message, reportId)
        NOTIF->>+DB: INSERT INTO Notifications
        DB-->>-NOTIF: Created
        NOTIF-->>-SVC: Success
        
        SVC->>+EMAIL: SendMonthlyReportSubmissionEmailAsync(...)
        activate EMAIL
        Note over EMAIL: Email with PDF attachment
        EMAIL-->>-SVC: Success
        deactivate EMAIL
    end
    
    SVC-->>-API: void
    
    API-->>-CM: 200 OK "Báo cáo đã được nộp thành công"
```

### 2.3. Approve Monthly Report (Admin)

```mermaid
sequenceDiagram
    autonumber
    participant Admin as Admin
    participant API as MonthlyReportApprovalController
    participant SVC as MonthlyReportApprovalService
    participant REPO as MonthlyReportRepository
    participant DB as Database
    participant NOTIF as NotificationService
    
    Admin->>+API: POST /api/monthly-reports/{id}/approve
    
    API->>API: Validate Admin role
    API->>API: Get adminId from claims
    
    API->>+SVC: ApproveReportAsync(reportId, adminId)
    
    SVC->>+REPO: GetByIdAsync(reportId)
    REPO->>+DB: SELECT * FROM Plans
    DB-->>-REPO: Plan
    REPO-->>-SVC: Plan
    
    SVC->>SVC: Validate Status == "PendingApproval"
    
    SVC->>SVC: Update Status = "Approved"
    SVC->>SVC: Set ApprovedById = adminId
    SVC->>SVC: Set ApprovedAt = DateTime.Now
    
    SVC->>+REPO: UpdateAsync(plan)
    REPO->>+DB: UPDATE Plans
    DB-->>-REPO: Updated
    REPO-->>-SVC: Plan
    
    SVC->>SVC: GetClubManagerAsync(clubId)
    SVC->>+DB: Query ClubMembers WHERE RoleInClub = "Manager"
    DB-->>-SVC: ClubMember with User
    
    SVC->>+NOTIF: CreateAsync(notification)
    Note over NOTIF: "Báo cáo được phê duyệt"
    NOTIF->>+DB: INSERT INTO Notifications
    DB-->>-NOTIF: Created
    NOTIF-->>-SVC: Success
    
    SVC-->>-API: void
    
    API-->>-Admin: 200 OK "Báo cáo đã được phê duyệt thành công"
```

### 2.4. Reject Monthly Report (Admin)

```mermaid
sequenceDiagram
    autonumber
    participant Admin as Admin
    participant API as MonthlyReportApprovalController
    participant SVC as MonthlyReportApprovalService
    participant REPO as MonthlyReportRepository
    participant DB as Database
    participant NOTIF as NotificationService
    
    Admin->>+API: POST /api/monthly-reports/{id}/reject
    Note over API: RejectMonthlyReportDto {reason}
    
    API->>API: Validate Admin role
    API->>API: Validate reason is not empty
    API->>API: Get adminId from claims
    
    API->>+SVC: RejectReportAsync(reportId, adminId, reason)
    
    SVC->>SVC: Validate reason is not empty
    
    SVC->>+REPO: GetByIdAsync(reportId)
    REPO->>+DB: SELECT * FROM Plans
    DB-->>-REPO: Plan
    REPO-->>-SVC: Plan
    
    SVC->>SVC: Validate Status == "PendingApproval"
    
    SVC->>SVC: Update Status = "Rejected"
    SVC->>SVC: Set RejectionReason = reason
    SVC->>SVC: Clear ApprovedById, ApprovedAt
    
    SVC->>+REPO: UpdateAsync(plan)
    REPO->>+DB: UPDATE Plans
    DB-->>-REPO: Updated
    REPO-->>-SVC: Plan
    
    SVC->>SVC: GetClubManagerAsync(clubId)
    SVC->>+DB: Query ClubMembers WHERE RoleInClub = "Manager"
    DB-->>-SVC: ClubMember with User
    
    SVC->>+NOTIF: CreateAsync(notification)
    Note over NOTIF: "Báo cáo bị từ chối" + reason
    NOTIF->>+DB: INSERT INTO Notifications
    DB-->>-NOTIF: Created
    NOTIF-->>-SVC: Success
    
    SVC-->>-API: void
    
    API-->>-Admin: 200 OK "Báo cáo đã bị từ chối"
```

### 2.5. Get Monthly Report with Fresh Data

```mermaid
sequenceDiagram
    autonumber
    participant User as User
    participant API as MonthlyReportController
    participant SVC as MonthlyReportService
    participant REPO as MonthlyReportRepository
    participant AGG as MonthlyReportDataAggregator
    participant DB as Database
    
    User->>+API: GET /api/monthly-reports/{id}
    
    API->>API: Validate user authentication
    
    API->>+SVC: GetReportWithFreshDataAsync(reportId)
    
    SVC->>+REPO: GetByIdAsync(reportId)
    REPO->>+DB: SELECT * FROM Plans INCLUDE Club, ApprovedBy
    DB-->>-REPO: Plan
    REPO-->>-SVC: Plan
    
    SVC->>SVC: Validate Plan exists
    SVC->>SVC: Calculate nextMonth, nextYear
    
    SVC->>+DB: Get Club with Category
    DB-->>-SVC: Club
    
    SVC->>+DB: Get ClubManager
    DB-->>-SVC: User (manager)
    
    rect rgb(240, 248, 255)
        Note over SVC,AGG: Part A: Current Month Activities
        
        SVC->>+AGG: GetSchoolEventsAsync(clubId, month, year)
        activate AGG
        AGG->>+DB: Query Activities WHERE Type IN (LargeEvent, MediumEvent, SmallEvent)
        DB-->>-AGG: Activities with Attendances, Evaluation
        AGG->>AGG: Map to SchoolEventDto
        AGG-->>-SVC: List~SchoolEventDto~
        deactivate AGG
        
        SVC->>+AGG: GetSupportActivitiesAsync(clubId, month, year)
        activate AGG
        AGG->>+DB: Query Activities WHERE Type = SchoolCollaboration
        DB-->>-AGG: Activities
        AGG-->>-SVC: List~SupportActivityDto~
        deactivate AGG
        
        SVC->>+AGG: GetCompetitionsAsync(clubId, month, year)
        activate AGG
        AGG->>+DB: Query Activities WHERE Type IN (SchoolCompetition, ProvincialCompetition, NationalCompetition)
        DB-->>-AGG: Activities
        AGG-->>-SVC: List~CompetitionDto~
        deactivate AGG
        
        SVC->>+AGG: GetInternalMeetingsAsync(clubId, month, year)
        activate AGG
        AGG->>+DB: Query Activities WHERE Type IN (ClubMeeting, ClubTraining, ClubWorkshop)
        DB-->>-AGG: Activities
        AGG-->>-SVC: List~InternalMeetingDto~
        deactivate AGG
    end
    
    rect rgb(255, 248, 240)
        Note over SVC,AGG: Part B: Next Month Plans
        
        SVC->>+AGG: GetNextMonthPlansAsync(clubId, reportMonth, reportYear, nextMonth, nextYear)
        activate AGG
        AGG->>+DB: Get Plan for editable sections
        DB-->>-AGG: Plan
        AGG->>AGG: Parse Purpose, Responsibilities from JSON
        AGG->>+DB: Query planned Activities for nextMonth
        DB-->>-AGG: Activities
        AGG->>+DB: Query CommunicationPlans
        DB-->>-AGG: CommunicationPlans
        AGG-->>-SVC: NextMonthPlansDto
        deactivate AGG
    end
    
    SVC->>SVC: BuildMonthlyReportDto(plan, true)
    SVC-->>-API: MonthlyReportDto
    
    API-->>-User: 200 OK + MonthlyReportDto
```

### 2.6. Export Monthly Report to PDF

```mermaid
sequenceDiagram
    autonumber
    participant User as User
    participant API as MonthlyReportController
    participant SVC as MonthlyReportService
    participant PDF as MonthlyReportPdfService
    participant AGG as MonthlyReportDataAggregator
    participant REPO as MonthlyReportRepository
    participant DB as Database
    
    User->>+API: GET /api/monthly-reports/{id}/pdf
    
    API->>API: Validate user authentication
    
    API->>+PDF: ExportToPdfAsync(reportId)
    
    PDF->>PDF: BuildMonthlyReportDtoAsync(reportId)
    
    PDF->>+REPO: GetByIdAsync(reportId)
    REPO->>+DB: SELECT * FROM Plans
    DB-->>-REPO: Plan
    REPO-->>-PDF: Plan
    
    PDF->>+AGG: GetSchoolEventsAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-PDF: List~SchoolEventDto~
    
    PDF->>+AGG: GetSupportActivitiesAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-PDF: List~SupportActivityDto~
    
    PDF->>+AGG: GetCompetitionsAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-PDF: List~CompetitionDto~
    
    PDF->>+AGG: GetInternalMeetingsAsync(...)
    AGG->>+DB: Query Activities
    DB-->>-AGG: Activities
    AGG-->>-PDF: List~InternalMeetingDto~
    
    PDF->>+AGG: GetNextMonthPlansAsync(...)
    AGG->>+DB: Query Plans & Activities
    DB-->>-AGG: Data
    AGG-->>-PDF: NextMonthPlansDto
    
    PDF->>PDF: MonthlyReportDto ready
    
    rect rgb(240, 255, 240)
        Note over PDF: Generate PDF using QuestPDF
        activate PDF
        PDF->>PDF: Document.Create()
        PDF->>PDF: ComposeHeader(report)
        PDF->>PDF: ComposePartA(report)
        Note over PDF: School Events, Support Activities,<br/>Competitions, Internal Meetings
        PDF->>PDF: ComposePartB(report)
        Note over PDF: Purpose, Planned Events,<br/>Competitions, Communication Plan,<br/>Budget, Facility, Responsibilities
        PDF->>PDF: ComposeSignature(report)
        PDF->>PDF: document.GeneratePdf()
        deactivate PDF
    end
    
    PDF-->>-API: byte[] pdfData
    
    API->>+SVC: GetReportByIdAsync(id)
    SVC->>+REPO: GetByIdAsync(id)
    REPO->>+DB: SELECT * FROM Plans
    DB-->>-REPO: Plan
    REPO-->>-SVC: Plan
    SVC-->>-API: MonthlyReportDto (for filename)
    
    API->>API: Generate filename
    Note over API: BAO_CAO_HOAT_DONG_THANG_{month}_VA_KE_HOACH_HOAT_DONG_THANG_{nextMonth}_{clubName}.pdf
    
    API-->>-User: File(pdfBytes, "application/pdf", fileName)
```

---

## 3. State Diagram - Monthly Report Status

```mermaid
stateDiagram-v2
    [*] --> Draft : Create Report
    
    Draft --> PendingApproval : Submit
    Draft --> Draft : Update
    
    PendingApproval --> Approved : Admin Approve
    PendingApproval --> Rejected : Admin Reject
    
    Rejected --> PendingApproval : Re-submit
    Rejected --> Rejected : Update
    
    Approved --> [*]
    
    note right of Draft
        ClubManager can:
        - Edit report content
        - Add media URLs
        - Update next month plans
    end note
    
    note right of PendingApproval
        Waiting for Admin review
        - Email sent to all Admins
        - In-app notification created
    end note
    
    note right of Rejected
        ClubManager receives:
        - Notification with reason
        - Can edit and re-submit
    end note
    
    note right of Approved
        ClubManager receives:
        - Approval notification
        - Report is finalized
    end note
```

---

## 4. Component Diagram

```mermaid
flowchart TB
    subgraph Presentation["Presentation Layer"]
        MRC[MonthlyReportController]
        MRAC[MonthlyReportApprovalController]
    end
    
    subgraph Services["Service Layer"]
        MRS[MonthlyReportService]
        MRAS[MonthlyReportApprovalService]
        MRDA[MonthlyReportDataAggregator]
        MRPDF[MonthlyReportPdfService]
        NS[NotificationService]
        ES[EmailService]
    end
    
    subgraph Repository["Repository Layer"]
        MRR[MonthlyReportRepository]
        AR[ActivityRepository]
        AMER[ActivityMemberEvaluationRepository]
        CPR[CommunicationPlanRepository]
    end
    
    subgraph Data["Data Layer"]
        DB[(EduXtendContext)]
        Plan[Plan Model]
        Activity[Activity Model]
        Notification[Notification Model]
    end
    
    subgraph External["External Services"]
        SMTP[SMTP Server]
        QuestPDF[QuestPDF Library]
    end
    
    MRC --> MRS
    MRC --> MRPDF
    MRAC --> MRAS
    
    MRS --> MRR
    MRS --> MRDA
    MRS --> NS
    MRS --> ES
    MRS --> MRPDF
    
    MRAS --> MRR
    MRAS --> NS
    
    MRDA --> AR
    MRDA --> AMER
    MRDA --> CPR
    
    MRPDF --> MRR
    MRPDF --> MRDA
    MRPDF --> QuestPDF
    
    MRR --> DB
    AR --> DB
    AMER --> DB
    CPR --> DB
    NS --> DB
    
    ES --> SMTP
    
    DB --> Plan
    DB --> Activity
    DB --> Notification
```

---

## 5. ER Diagram - Monthly Report Related Entities

```mermaid
erDiagram
    Plan ||--o{ Activity : "references"
    Plan }o--|| Club : "belongs to"
    Plan }o--o| User : "approved by"
    
    Club ||--o{ ClubMember : "has"
    ClubMember }o--|| Student : "is"
    Student }o--|| User : "has account"
    
    Activity ||--o{ ActivityAttendance : "has"
    Activity ||--o| ActivityEvaluation : "has"
    Activity ||--o{ ActivitySchedule : "has"
    ActivitySchedule ||--o{ ActivityScheduleAssignment : "has"
    ActivityScheduleAssignment ||--o{ ActivityMemberEvaluation : "evaluated by"
    
    Club ||--o{ CommunicationPlan : "has"
    CommunicationPlan ||--o{ CommunicationItem : "contains"
    
    User ||--o{ Notification : "receives"
    
    Plan {
        int Id PK
        int ClubId FK
        string Title
        string Description
        string Status
        string ReportType
        int ReportMonth
        int ReportYear
        datetime CreatedAt
        datetime SubmittedAt
        int ApprovedById FK
        datetime ApprovedAt
        string RejectionReason
        string EventMediaUrls
        string NextMonthPurposeAndSignificance
        string ClubResponsibilities
    }
    
    Club {
        int Id PK
        string Name
        int CategoryId FK
    }
    
    Activity {
        int Id PK
        int ClubId FK
        string Title
        string Description
        ActivityType Type
        datetime StartTime
        datetime EndTime
        string Location
        int MaxParticipants
        string ImageUrl
    }
    
    ActivityEvaluation {
        int Id PK
        int ActivityId FK
        int ExpectedParticipants
        int ActualParticipants
        string Reason
        decimal CommunicationScore
        decimal OrganizationScore
        decimal HostScore
        decimal SpeakerScore
        decimal Success
        string Limitations
        string ImprovementMeasures
    }
    
    Notification {
        int Id PK
        string Title
        string Message
        string Scope
        int TargetUserId FK
        int CreatedById FK
        bool IsRead
        datetime CreatedAt
    }
```

---

## Ghi chú

### Các trạng thái của Monthly Report:
1. **Draft**: Báo cáo mới tạo, ClubManager có thể chỉnh sửa
2. **PendingApproval**: Đã nộp, đang chờ Admin phê duyệt
3. **Approved**: Đã được Admin phê duyệt
4. **Rejected**: Bị Admin từ chối, ClubManager có thể chỉnh sửa và nộp lại

### Các thành phần chính:
- **MonthlyReportService**: Xử lý logic nghiệp vụ chính (CRUD, submit)
- **MonthlyReportApprovalService**: Xử lý phê duyệt/từ chối (Admin only)
- **MonthlyReportDataAggregator**: Tổng hợp dữ liệu từ nhiều nguồn (Activities, Evaluations, etc.)
- **MonthlyReportPdfService**: Xuất báo cáo ra file PDF
- **MonthlyReportRepository**: Truy cập dữ liệu Plan (Monthly Report)
