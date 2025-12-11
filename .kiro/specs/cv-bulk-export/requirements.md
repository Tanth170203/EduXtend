# Requirements Document

## Introduction

Tính năng này cho phép Club Manager tự động trích xuất thông tin từ các file CV (PDF/Word) của các đơn đăng ký chưa có lịch phỏng vấn và xuất tất cả thông tin đó ra một file Excel tổng hợp. Hệ thống sẽ phân tích nội dung bên trong mỗi file CV để lấy ra các thông tin như tên, profile, học vấn, kinh nghiệm, kỹ năng, v.v., sau đó tổng hợp tất cả vào Excel với mỗi ứng viên trên một dòng và mỗi loại thông tin trên một cột.

## Glossary

- **System**: Hệ thống quản lý câu lạc bộ EduXtend
- **Club Manager**: Người quản lý câu lạc bộ có quyền xem và xử lý đơn đăng ký
- **Join Request**: Đơn đăng ký tham gia câu lạc bộ của sinh viên
- **CV File**: File PDF hoặc Word chứa hồ sơ xin gia nhập câu lạc bộ của sinh viên
- **CV Parsing**: Quá trình tự động đọc và trích xuất thông tin có cấu trúc từ file CV
- **Interview**: Lịch phỏng vấn được tạo cho một đơn đăng ký
- **Excel File**: File định dạng .xlsx chứa dữ liệu dạng bảng
- **Unscheduled Join Request**: Đơn đăng ký có status là "Pending" và chưa có Interview record tương ứng
- **CV Parser**: Thành phần phần mềm thực hiện việc phân tích và trích xuất dữ liệu từ CV
- **Extracted Data**: Dữ liệu đã được trích xuất từ CV bao gồm thông tin cá nhân, học vấn, kinh nghiệm, kỹ năng

## Requirements

### Requirement 1

**User Story:** Là một Club Manager, tôi muốn hệ thống tự động trích xuất thông tin từ các file CV của đơn đăng ký chưa có lịch phỏng vấn, để tôi không phải mở từng file CV một để xem thông tin.

#### Acceptance Criteria

1. WHEN a Club Manager yêu cầu trích xuất CV THEN the System SHALL tải xuống tất cả các CV file từ các Join Request có status "Pending" và không có Interview record
2. WHEN tải xuống CV file THEN the System SHALL hỗ trợ các định dạng PDF và DOCX
3. WHEN một CV file không tải được THEN the System SHALL ghi log lỗi và tiếp tục xử lý các CV còn lại
4. WHEN tất cả CV file đã được tải THEN the System SHALL bắt đầu quá trình phân tích từng file

### Requirement 2

**User Story:** Là một Club Manager, tôi muốn hệ thống phân tích và trích xuất các thông tin quan trọng từ CV, để tôi có thể có cái nhìn tổng quan về ứng viên mà không cần đọc toàn bộ CV.

#### Acceptance Criteria

1. WHEN phân tích một CV file THEN the System SHALL trích xuất thông tin cá nhân bao gồm họ tên, email, số điện thoại
2. WHEN phân tích một CV file THEN the System SHALL trích xuất phần profile hoặc mục tiêu nghề nghiệp
3. WHEN phân tích một CV file THEN the System SHALL trích xuất thông tin học vấn bao gồm trường, chuyên ngành, thời gian học
4. WHEN phân tích một CV file THEN the System SHALL trích xuất kinh nghiệm làm việc hoặc hoạt động bao gồm vị trí, tổ chức, thời gian
5. WHEN phân tích một CV file THEN the System SHALL trích xuất danh sách kỹ năng
6. WHEN một CV file không thể phân tích được THEN the System SHALL ghi nhận lỗi và để trống các trường thông tin tương ứng

### Requirement 3

**User Story:** Là một Club Manager, tôi muốn xuất tất cả thông tin đã trích xuất từ CV ra file Excel, để tôi có thể xem xét tất cả ứng viên trong một bảng tổng hợp.

#### Acceptance Criteria

1. WHEN tất cả CV đã được phân tích THEN the System SHALL tạo file Excel với mỗi ứng viên trên một dòng
2. WHEN tạo file Excel THEN the System SHALL bao gồm các cột: STT, Mã sinh viên (từ database), Họ tên (từ CV), Email (từ CV), Số điện thoại (từ CV), Profile, Học vấn, Kinh nghiệm, Kỹ năng, Link CV gốc, Ngày nộp đơn
3. WHEN một thông tin không trích xuất được THEN the System SHALL để trống ô tương ứng trong Excel
4. WHEN file Excel được tạo THEN the System SHALL tự động tải file về máy của Club Manager với tên file có định dạng "CV_Extracted_[TenClub]_[NgayThang].xlsx"
5. WHEN không có đơn đăng ký nào THEN the System SHALL hiển thị thông báo và không tạo file Excel

### Requirement 4

**User Story:** Là một Club Manager, tôi chỉ muốn xem và xuất các đơn đăng ký thuộc câu lạc bộ của mình, để đảm bảo tính bảo mật và phân quyền đúng.

#### Acceptance Criteria

1. WHEN a Club Manager truy cập chức năng THEN the System SHALL chỉ hiển thị các đơn đăng ký thuộc câu lạc bộ mà người dùng đang quản lý
2. WHEN a Club Manager không có quyền quản lý câu lạc bộ nào THEN the System SHALL từ chối truy cập và hiển thị thông báo lỗi phân quyền
3. WHEN xuất file Excel THEN the System SHALL chỉ bao gồm các đơn đăng ký của câu lạc bộ được quản lý bởi Club Manager đó
4. WHEN a Club Manager quản lý nhiều câu lạc bộ THEN the System SHALL cho phép chọn câu lạc bộ cụ thể để xuất danh sách

### Requirement 5

**User Story:** Là một Club Manager, tôi muốn file Excel được định dạng rõ ràng và dễ đọc, để tôi có thể nhanh chóng xem xét và so sánh thông tin các ứng viên.

#### Acceptance Criteria

1. WHEN file Excel được tạo THEN the System SHALL định dạng dòng tiêu đề với font chữ đậm và màu nền
2. WHEN file Excel được tạo THEN the System SHALL tự động điều chỉnh độ rộng cột phù hợp với nội dung
3. WHEN file Excel chứa link CV THEN the System SHALL tạo hyperlink có thể click được trong cột Link CV
4. WHEN file Excel được tạo THEN the System SHALL sắp xếp các dòng theo thứ tự ngày nộp đơn từ mới đến cũ
5. WHEN các cột chứa text dài như Profile, Học vấn, Kinh nghiệm THEN the System SHALL bật text wrapping để hiển thị đầy đủ nội dung

### Requirement 6

**User Story:** Là một Club Manager, tôi muốn hệ thống xử lý và thông báo tiến trình rõ ràng, để tôi biết quá trình đang diễn ra như thế nào và không lo lắng khi phải chờ đợi.

#### Acceptance Criteria

1. WHEN bắt đầu quá trình trích xuất CV THEN the System SHALL hiển thị loading indicator với thông báo số lượng CV đang xử lý
2. WHEN đang xử lý từng CV THEN the System SHALL cập nhật tiến trình theo phần trăm hoặc số lượng đã xử lý
3. WHEN quá trình hoàn thành THEN the System SHALL hiển thị thông báo thành công với số lượng CV đã xử lý thành công
4. WHEN có CV không xử lý được THEN the System SHALL hiển thị cảnh báo với danh sách các CV bị lỗi
5. WHEN quá trình thất bại hoàn toàn THEN the System SHALL hiển thị thông báo lỗi cụ thể và ghi log để debug


### Requirement 7

**User Story:** Là một Club Manager, tôi muốn hệ thống xử lý chính xác các định dạng CV khác nhau, để đảm bảo thông tin được trích xuất đúng bất kể ứng viên sử dụng template CV nào.

#### Acceptance Criteria

1. WHEN phân tích CV định dạng PDF THEN the System SHALL trích xuất text từ PDF và phân tích cấu trúc
2. WHEN phân tích CV định dạng DOCX THEN the System SHALL trích xuất text từ Word document và phân tích cấu trúc
3. WHEN CV có nhiều trang THEN the System SHALL xử lý tất cả các trang và tổng hợp thông tin
4. WHEN CV có định dạng đặc biệt như bảng hoặc cột THEN the System SHALL cố gắng trích xuất text theo đúng thứ tự logic
5. WHEN CV chứa ký tự tiếng Việt có dấu THEN the System SHALL xử lý đúng encoding và hiển thị chính xác trong Excel

### Requirement 8

**User Story:** Là một Club Manager, tôi muốn có thể xem trước kết quả trích xuất trước khi xuất Excel, để tôi có thể kiểm tra và điều chỉnh nếu cần.

#### Acceptance Criteria

1. WHEN quá trình trích xuất hoàn thành THEN the System SHALL hiển thị bảng preview với dữ liệu đã trích xuất
2. WHEN xem preview THEN the System SHALL cho phép Club Manager cuộn xem tất cả các dòng và cột
3. WHEN xem preview THEN the System SHALL hiển thị rõ các ô trống hoặc thông tin không trích xuất được
4. WHEN xem preview THEN the System SHALL cung cấp nút "Xuất Excel" để tải file cuối cùng
5. WHEN Club Manager không hài lòng với kết quả THEN the System SHALL cho phép hủy và thử lại
