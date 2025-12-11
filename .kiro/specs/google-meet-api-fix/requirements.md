# Requirements Document

## Introduction

Hệ thống hiện tại gặp lỗi "Invalid conference type value" khi tạo link Google Meet thông qua Google Calendar API. Lỗi này xảy ra do cấu hình không đúng của conference type hoặc thiếu quyền cần thiết cho service account. Tính năng này sẽ khắc phục lỗi và đảm bảo việc tạo Google Meet link hoạt động ổn định.

## Glossary

- **Google Meet Service**: Service tạo và quản lý Google Meet links thông qua Google Calendar API
- **Service Account**: Tài khoản dịch vụ Google được sử dụng để xác thực API calls
- **Conference Type**: Loại hội nghị được chỉ định khi tạo event với Google Calendar API
- **Domain-Wide Delegation**: Quyền cho phép service account hoạt động thay mặt users trong Google Workspace domain
- **Calendar API**: Google Calendar API v3 được sử dụng để tạo events và conference data

## Requirements

### Requirement 1

**User Story:** Là một developer, tôi muốn xác định nguyên nhân chính xác của lỗi "Invalid conference type value", để có thể áp dụng giải pháp phù hợp.

#### Acceptance Criteria

1. WHEN the Google Meet Service attempts to create a meeting THEN the system SHALL use the correct conference solution type value according to Google Calendar API documentation
2. WHEN using a service account THEN the system SHALL verify that domain-wide delegation is properly configured
3. WHEN the API returns an error THEN the system SHALL log detailed error information including the conference type value being used
4. THE system SHALL validate that the service account has Calendar API enabled in Google Cloud Console
5. THE system SHALL check that the service account has the necessary OAuth scopes: https://www.googleapis.com/auth/calendar

### Requirement 2

**User Story:** Là một developer, tôi muốn thử các giải pháp thay thế để tạo Google Meet link, để đảm bảo tính năng hoạt động ngay cả khi một phương pháp gặp vấn đề.

#### Acceptance Criteria

1. WHEN the primary method fails THEN the Google Meet Service SHALL attempt alternative conference type values
2. THE Google Meet Service SHALL support both "hangoutsMeet" and "eventHangout" conference types
3. WHEN all automatic methods fail THEN the Google Meet Service SHALL provide a fallback mechanism
4. THE Google Meet Service SHALL implement retry logic with exponential backoff for transient errors
5. WHEN a method succeeds THEN the Google Meet Service SHALL cache the working configuration for future use

### Requirement 3

**User Story:** Là một system administrator, tôi muốn có hướng dẫn rõ ràng về cách cấu hình service account, để đảm bảo Google Meet integration hoạt động đúng.

#### Acceptance Criteria

1. THE system SHALL provide documentation on creating and configuring a Google Cloud service account
2. THE documentation SHALL include steps for enabling Google Calendar API
3. THE documentation SHALL explain how to set up domain-wide delegation if using Google Workspace
4. THE documentation SHALL list all required OAuth scopes
5. THE documentation SHALL include troubleshooting steps for common errors

### Requirement 4

**User Story:** Là một developer, tôi muốn có error handling tốt hơn khi Google Meet link creation fails, để người dùng nhận được thông báo rõ ràng và hệ thống có thể fallback gracefully.

#### Acceptance Criteria

1. WHEN Google Meet link creation fails THEN the Google Meet Service SHALL throw a specific exception with clear error message
2. WHEN the error is due to configuration issues THEN the error message SHALL indicate what configuration is missing or incorrect
3. WHEN the error is due to API limits or temporary issues THEN the error message SHALL suggest retry or alternative options
4. THE Interview Service SHALL catch Google Meet errors and allow interview creation to continue with manual meeting link entry
5. THE system SHALL log all Google Meet API errors with sufficient detail for debugging

### Requirement 5

**User Story:** Là một developer, tôi muốn test Google Meet integration một cách độc lập, để xác minh cấu hình đúng trước khi deploy.

#### Acceptance Criteria

1. THE system SHALL provide a test endpoint or method to verify Google Meet configuration
2. WHEN the test is run THEN the system SHALL attempt to create a test meeting and return the result
3. WHEN the test succeeds THEN the system SHALL return the generated meeting link and delete the test event
4. WHEN the test fails THEN the system SHALL return detailed error information
5. THE test SHALL validate all configuration parameters including service account path, calendar ID, and OAuth scopes

### Requirement 6

**User Story:** Là một developer, tôi muốn có option để sử dụng OAuth 2.0 user authentication thay vì service account, để có thêm lựa chọn authentication method.

#### Acceptance Criteria

1. THE Google Meet Service SHALL support both service account and OAuth 2.0 user authentication
2. WHEN using OAuth 2.0 THEN the system SHALL implement proper token refresh mechanism
3. THE configuration SHALL allow switching between authentication methods via appsettings
4. WHEN using OAuth 2.0 THEN the system SHALL store refresh tokens securely
5. THE system SHALL provide clear documentation on when to use each authentication method
