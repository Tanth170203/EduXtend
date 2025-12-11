# Requirements Document

## Introduction

Tính năng này mở rộng hệ thống lịch phỏng vấn hiện tại để hỗ trợ cả phỏng vấn trực tuyến (online) và trực tiếp (offline). Khi chọn phỏng vấn trực tuyến, hệ thống sẽ tự động tạo link Google Meet. Sau khi tạo lịch phỏng vấn, hệ thống sẽ gửi thông báo qua cả hệ thống nội bộ và email cho ứng viên.

## Glossary

- **Interview System**: Hệ thống quản lý lịch phỏng vấn của câu lạc bộ
- **Club Manager**: Người quản lý câu lạc bộ có quyền tạo và quản lý lịch phỏng vấn
- **Applicant**: Sinh viên nộp đơn xin gia nhập câu lạc bộ
- **Google Meet Link**: Đường link cuộc họp trực tuyến được tạo tự động thông qua Google Meet API
- **System Notification**: Thông báo hiển thị trong hệ thống web
- **Email Notification**: Thông báo được gửi qua email

## Requirements

### Requirement 1

**User Story:** Là một Club Manager, tôi muốn chọn hình thức phỏng vấn (online hoặc offline) khi tạo lịch, để phù hợp với điều kiện và nhu cầu của câu lạc bộ.

#### Acceptance Criteria

1. WHEN a Club Manager creates an interview schedule THEN the Interview System SHALL display two interview type options: "Online" and "Offline"
2. WHEN a Club Manager selects "Offline" THEN the Interview System SHALL display a text input field for physical location address
3. WHEN a Club Manager selects "Online" THEN the Interview System SHALL hide the physical location input field
4. WHEN a Club Manager submits the interview form with "Offline" selected THEN the Interview System SHALL validate that the location field is not empty
5. THE Interview System SHALL store the interview type (Online or Offline) in the database

### Requirement 2

**User Story:** Là một Club Manager, tôi muốn hệ thống tự động tạo link Google Meet khi chọn phỏng vấn online, để tiết kiệm thời gian và đảm bảo có link họp sẵn sàng.

#### Acceptance Criteria

1. WHEN a Club Manager submits an interview form with "Online" type selected THEN the Interview System SHALL automatically generate a Google Meet link
2. WHEN the Google Meet link is generated THEN the Interview System SHALL store the link in the location field of the interview record
3. IF the Google Meet link generation fails THEN the Interview System SHALL return an error message and prevent interview creation
4. WHEN an online interview is created THEN the Interview System SHALL include the Google Meet link in all notifications
5. THE Interview System SHALL use Google Calendar API or Google Meet API to generate unique meeting links

### Requirement 3

**User Story:** Là một Applicant, tôi muốn nhận thông báo qua hệ thống khi có lịch phỏng vấn mới, để biết ngay lập tức và không bỏ lỡ thông tin quan trọng.

#### Acceptance Criteria

1. WHEN an interview is successfully created THEN the Interview System SHALL create a system notification for the Applicant
2. WHEN creating a system notification THEN the Interview System SHALL include interview date, time, type (Online/Offline), and location or meeting link
3. WHEN the interview type is "Online" THEN the notification SHALL display the Google Meet link as a clickable element
4. WHEN the interview type is "Offline" THEN the notification SHALL display the physical address
5. THE Interview System SHALL mark the notification as unread by default

### Requirement 4

**User Story:** Là một Applicant, tôi muốn nhận email thông báo về lịch phỏng vấn, để có thể lưu trữ thông tin và nhận được nhắc nhở qua email.

#### Acceptance Criteria

1. WHEN an interview is successfully created THEN the Interview System SHALL send an email notification to the Applicant's registered email address
2. WHEN composing the email THEN the Interview System SHALL include the club name, interview date and time, interview type, and location or meeting link
3. WHEN the interview type is "Online" THEN the email SHALL include the Google Meet link as a clickable hyperlink
4. WHEN the interview type is "Offline" THEN the email SHALL include the physical address with clear formatting
5. IF the email sending fails THEN the Interview System SHALL log the error but still complete the interview creation
6. THE Interview System SHALL use a professional email template with clear formatting and branding

### Requirement 5

**User Story:** Là một Club Manager, tôi muốn cập nhật lịch phỏng vấn đã tạo và thay đổi hình thức phỏng vấn, để linh hoạt điều chỉnh khi có thay đổi kế hoạch.

#### Acceptance Criteria

1. WHEN a Club Manager updates an existing interview THEN the Interview System SHALL allow changing the interview type from Online to Offline or vice versa
2. WHEN changing from "Offline" to "Online" THEN the Interview System SHALL generate a new Google Meet link
3. WHEN changing from "Online" to "Offline" THEN the Interview System SHALL clear the Google Meet link and require a physical location input
4. WHEN an interview is updated THEN the Interview System SHALL send both system notification and email notification to the Applicant with updated information
5. THE Interview System SHALL preserve the interview history and track changes

### Requirement 6

**User Story:** Là một Applicant, tôi muốn xem chi tiết lịch phỏng vấn của mình bao gồm link Google Meet (nếu online), để dễ dàng tham gia phỏng vấn đúng thời gian.

#### Acceptance Criteria

1. WHEN an Applicant views their interview details THEN the Interview System SHALL display the interview type prominently
2. WHEN the interview type is "Online" THEN the Interview System SHALL display the Google Meet link as a clickable button or link
3. WHEN the interview type is "Offline" THEN the Interview System SHALL display the physical address with map integration (if available)
4. WHEN an Applicant clicks on a Google Meet link THEN the Interview System SHALL open the link in a new browser tab
5. THE Interview System SHALL display the interview information in a clear, user-friendly format
