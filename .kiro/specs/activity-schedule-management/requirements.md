# Requirements Document

## Introduction

Hệ thống quản lý hoạt động hiện tại cần được mở rộng để hỗ trợ lập lịch trình chi tiết và phân công công việc cho các loại hoạt động phức tạp. Tính năng này cho phép người quản lý CLB và Admin tạo timeline chi tiết cho các sự kiện, cuộc thi và hoạt động phối hợp, bao gồm việc phân công nhân sự cho từng phần trong lịch trình.

## Glossary

- **Activity Management System**: Hệ thống quản lý các hoạt động của CLB và nhà trường trong EduXtend
- **Activity Schedule**: Một mục trong lịch trình hoạt động, bao gồm thời gian bắt đầu, kết thúc và nội dung
- **Activity Schedule Assignment**: Phân công nhân sự cho một mục lịch trình cụ thể
- **Club Activities (Internal)**: Các hoạt động nội bộ CLB bao gồm ClubMeeting, ClubTraining, ClubWorkshop
- **Complex Activities**: Các hoạt động phức tạp bao gồm Events (LargeEvent, MediumEvent, SmallEvent), Competitions (SchoolCompetition, ProvincialCompetition, NationalCompetition), và Collaborations (ClubCollaboration, SchoolCollaboration)
- **Club Manager**: Người quản lý CLB có quyền tạo và chỉnh sửa hoạt động của CLB
- **Admin**: Quản trị viên hệ thống có quyền tạo và chỉnh sửa tất cả hoạt động
- **Timeline**: Chuỗi các mục lịch trình được sắp xếp theo thứ tự thời gian

## Requirements

### Requirement 1

**User Story:** Là một Club Manager hoặc Admin, tôi muốn thêm lịch trình chi tiết khi tạo hoạt động phức tạp, để có thể lập kế hoạch timeline cho sự kiện

#### Acceptance Criteria

1. WHEN Club Manager hoặc Admin tạo Activity với Type là LargeEvent, MediumEvent, SmallEvent, SchoolCompetition, ProvincialCompetition, NationalCompetition, ClubCollaboration, hoặc SchoolCollaboration, THE Activity Management System SHALL hiển thị giao diện để thêm Activity Schedules
2. WHEN Club Manager hoặc Admin tạo Activity với Type là ClubMeeting, ClubTraining, hoặc ClubWorkshop, THE Activity Management System SHALL không hiển thị giao diện để thêm Activity Schedules
3. WHEN Club Manager hoặc Admin thêm một Activity Schedule, THE Activity Management System SHALL yêu cầu nhập StartTime, EndTime, Title và cho phép nhập Description và Notes
4. WHEN Club Manager hoặc Admin lưu Activity với Schedules, THE Activity Management System SHALL lưu tất cả Schedules vào bảng ActivitySchedules với ActivityId tương ứng
5. WHEN Club Manager hoặc Admin thêm nhiều Activity Schedules, THE Activity Management System SHALL tự động sắp xếp chúng theo thứ tự thời gian tăng dần

### Requirement 2

**User Story:** Là một Club Manager hoặc Admin, tôi muốn phân công nhân sự cho từng phần trong lịch trình, để mọi người biết ai chịu trách nhiệm phần nào

#### Acceptance Criteria

1. WHEN Club Manager hoặc Admin thêm một Activity Schedule, THE Activity Management System SHALL cho phép thêm một hoặc nhiều Activity Schedule Assignments cho Schedule đó
2. WHEN Club Manager hoặc Admin thêm một Assignment, THE Activity Management System SHALL cho phép chọn User từ hệ thống hoặc nhập tên tự do vào ResponsibleName
3. WHEN Club Manager hoặc Admin thêm một Assignment, THE Activity Management System SHALL cho phép nhập Role để mô tả vai trò của người được phân công
4. WHEN Club Manager hoặc Admin lưu Activity với Schedules và Assignments, THE Activity Management System SHALL lưu tất cả Assignments vào bảng ActivityScheduleAssignments với ActivityScheduleId tương ứng
5. WHEN Club Manager hoặc Admin xem một Schedule, THE Activity Management System SHALL hiển thị danh sách tất cả Assignments được phân công cho Schedule đó

### Requirement 3

**User Story:** Là một Club Manager hoặc Admin, tôi muốn chỉnh sửa lịch trình và phân công công việc của hoạt động đã tạo, để có thể cập nhật khi có thay đổi

#### Acceptance Criteria

1. WHEN Club Manager hoặc Admin mở trang Edit Activity với Type là Complex Activities, THE Activity Management System SHALL hiển thị danh sách các Schedules hiện có
2. WHEN Club Manager hoặc Admin chỉnh sửa một Schedule, THE Activity Management System SHALL cho phép cập nhật StartTime, EndTime, Title, Description và Notes
3. WHEN Club Manager hoặc Admin xóa một Schedule, THE Activity Management System SHALL xóa Schedule đó và tất cả Assignments liên quan
4. WHEN Club Manager hoặc Admin thêm Schedule mới vào Activity đã tồn tại, THE Activity Management System SHALL lưu Schedule mới với ActivityId tương ứng
5. WHEN Club Manager hoặc Admin chỉnh sửa Assignments của một Schedule, THE Activity Management System SHALL cho phép thêm, sửa hoặc xóa Assignments

### Requirement 4

**User Story:** Là một Club Manager hoặc Admin, tôi muốn xem lịch trình và phân công công việc của hoạt động, để theo dõi kế hoạch thực hiện

#### Acceptance Criteria

1. WHEN Club Manager hoặc Admin xem Details của Activity với Type là Complex Activities, THE Activity Management System SHALL hiển thị timeline đầy đủ với tất cả Schedules
2. WHEN Club Manager hoặc Admin xem một Schedule trong Details, THE Activity Management System SHALL hiển thị StartTime, EndTime, Title, Description, Notes và danh sách Assignments
3. WHEN Club Manager hoặc Admin xem một Assignment, THE Activity Management System SHALL hiển thị tên người phụ trách (User name hoặc ResponsibleName) và Role
4. WHILE Activity có Schedules, THE Activity Management System SHALL sắp xếp Schedules theo thứ tự thời gian tăng dần trong giao diện Details
5. WHEN Club Manager hoặc Admin xem Details của Activity với Type là Club Activities Internal, THE Activity Management System SHALL không hiển thị phần Schedules và Assignments

### Requirement 5

**User Story:** Là một Club Manager hoặc Admin, tôi muốn hệ thống validate dữ liệu lịch trình, để đảm bảo tính hợp lệ của timeline

#### Acceptance Criteria

1. WHEN Club Manager hoặc Admin thêm Schedule với EndTime nhỏ hơn hoặc bằng StartTime, THE Activity Management System SHALL hiển thị thông báo lỗi và không cho phép lưu
2. WHEN Club Manager hoặc Admin thêm Schedule mà không nhập Title, THE Activity Management System SHALL hiển thị thông báo lỗi và không cho phép lưu
3. WHEN Club Manager hoặc Admin thêm Assignment mà không chọn User và không nhập ResponsibleName, THE Activity Management System SHALL hiển thị thông báo lỗi và không cho phép lưu
4. WHEN Club Manager hoặc Admin lưu Activity với Schedules, THE Activity Management System SHALL validate rằng tất cả Schedules có thời gian nằm trong khoảng StartTime và EndTime của Activity
5. WHEN Club Manager hoặc Admin nhập Title vượt quá 500 ký tự hoặc Description/Notes vượt quá 1000 ký tự, THE Activity Management System SHALL hiển thị thông báo lỗi và không cho phép lưu
