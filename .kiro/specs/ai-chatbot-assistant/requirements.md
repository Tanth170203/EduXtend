# Requirements Document

## Introduction

Hệ thống AI Chatbot Assistant là một tính năng hỗ trợ sinh viên tìm kiếm và nhận đề xuất về các câu lạc bộ (CLB) và hoạt động phù hợp với sở thích, năng lực và mục tiêu cá nhân của họ. Chatbot sử dụng Gemini AI để phân tích thông tin sinh viên và đưa ra các gợi ý thông minh, cá nhân hóa thông qua giao diện chat tương tác.

## Glossary

- **AI Chatbot System**: Hệ thống trò chuyện tự động sử dụng trí tuệ nhân tạo để tương tác với sinh viên
- **Gemini AI**: Dịch vụ AI của Google được tích hợp để xử lý ngôn ngữ tự nhiên và đưa ra đề xuất
- **Student Profile**: Thông tin cá nhân của sinh viên bao gồm chuyên ngành, sở thích, kỹ năng
- **Club Recommendation**: Đề xuất câu lạc bộ phù hợp dựa trên phân tích AI
- **Activity Recommendation**: Đề xuất hoạt động phù hợp dựa trên phân tích AI
- **Chat Session**: Phiên trò chuyện giữa sinh viên và AI Chatbot
- **HTTP Cookie Authentication**: Phương thức xác thực người dùng thông qua cookie
- **WebFE Application**: Ứng dụng web frontend của hệ thống EduXtend
- **WebAPI Application**: Ứng dụng web API backend của hệ thống EduXtend

## Requirements

### Requirement 1

**User Story:** Là một sinh viên, tôi muốn mở cửa sổ chat AI Assistant từ bất kỳ trang nào trong hệ thống, để có thể nhận hỗ trợ tìm kiếm CLB và hoạt động mọi lúc mọi nơi.

#### Acceptance Criteria

1. THE WebFE Application SHALL display a floating chat button on all pages accessible to authenticated students
2. WHEN a student clicks the floating chat button, THE WebFE Application SHALL open a chat modal window with the AI Assistant interface
3. THE chat modal window SHALL display the title "AI Assistant - Hỗ trợ tìm CLB & Hoạt động" at the top
4. THE chat modal window SHALL include a close button (X) that dismisses the modal WHEN clicked
5. THE WebFE Application SHALL maintain the chat session state WHILE the modal is open and closed during the same page session

### Requirement 2

**User Story:** Là một sinh viên, tôi muốn thấy giao diện chào mừng với các gợi ý nhanh khi mở chatbot, để dễ dàng bắt đầu cuộc trò chuyện.

#### Acceptance Criteria

1. WHEN the chat modal opens for the first time in a session, THE AI Chatbot System SHALL display a welcome message "Xin chào! 👋"
2. THE AI Chatbot System SHALL display an introduction message "Tôi là AI Assistant của EduXtend. Tôi có thể giúp bạn:"
3. THE AI Chatbot System SHALL display three quick action buttons: "🔍 Tìm CLB phù hợp", "📅 Xem hoạt động", and "💡 Tìm hiểu thêm"
4. WHEN a student clicks a quick action button, THE AI Chatbot System SHALL send the corresponding predefined message to start the conversation
5. THE chat interface SHALL include a text input field with placeholder "Nhập tin nhắn của bạn..." at the bottom

### Requirement 3

**User Story:** Là một sinh viên, tôi muốn gửi tin nhắn và nhận phản hồi từ AI, để có thể hỏi về CLB và hoạt động phù hợp với mình.

#### Acceptance Criteria

1. WHEN a student types a message and clicks the send button, THE WebFE Application SHALL send the message to the WebAPI Application via HTTP request
2. THE WebAPI Application SHALL authenticate the student using HTTP cookie authentication before processing the chat request
3. THE WebAPI Application SHALL send the student message along with relevant student profile context to the Gemini AI service
4. WHEN Gemini AI returns a response, THE WebAPI Application SHALL return the AI response to the WebFE Application
5. THE WebFE Application SHALL display both student messages and AI responses in the chat history with appropriate styling and timestamps

### Requirement 4

**User Story:** Là một sinh viên, tôi muốn AI hiểu thông tin cá nhân của mình (chuyên ngành, sở thích) để nhận được đề xuất CLB phù hợp, mà không cần phải nhập lại thông tin mỗi lần.

#### Acceptance Criteria

1. WHEN processing a chat request, THE WebAPI Application SHALL retrieve the authenticated student's profile information from the database
2. THE WebAPI Application SHALL include student major, interests, skills, and current club memberships in the context sent to Gemini AI
3. THE Gemini AI service SHALL analyze the student profile context to provide personalized club recommendations
4. THE AI Chatbot System SHALL provide club recommendations that match the student's major, interests, or skill development goals
5. THE AI response SHALL include specific club names, descriptions, and reasons why each club is suitable for the student

### Requirement 5

**User Story:** Là một sinh viên, tôi muốn AI đề xuất các hoạt động sắp tới phù hợp với mình, để có thể tham gia các sự kiện thú vị.

#### Acceptance Criteria

1. WHEN a student asks about activities, THE WebAPI Application SHALL retrieve upcoming activities from the database
2. THE WebAPI Application SHALL filter activities based on the student's interests, major, and current club memberships
3. THE WebAPI Application SHALL send the filtered activity list along with student context to Gemini AI
4. THE Gemini AI service SHALL analyze and recommend the most suitable activities with explanations
5. THE AI response SHALL include activity names, dates, locations, and personalized reasons for each recommendation

### Requirement 6

**User Story:** Là một quản trị viên hệ thống, tôi muốn cấu hình API key và các tham số của Gemini AI trong appsettings.json, để dễ dàng quản lý và thay đổi cấu hình mà không cần sửa code.

#### Acceptance Criteria

1. THE WebAPI Application SHALL read Gemini AI configuration from the appsettings.json file at startup
2. THE appsettings.json file SHALL contain a section "GeminiAI" with properties: "ApiKey", "Model", "Temperature", and "MaxTokens"
3. THE WebAPI Application SHALL use the configured API key to authenticate with the Gemini AI service
4. THE WebAPI Application SHALL apply the configured model, temperature, and max tokens parameters when making requests to Gemini AI
5. IF the Gemini AI configuration is missing or invalid, THE WebAPI Application SHALL log an error and return a user-friendly error message to the chat interface

### Requirement 7

**User Story:** Là một sinh viên, tôi muốn lịch sử chat của mình được lưu trong phiên làm việc, để có thể xem lại các đề xuất trước đó mà không bị mất thông tin khi cuộc trò chuyện tiếp diễn.

#### Acceptance Criteria

1. THE WebFE Application SHALL store chat messages in browser session storage WHILE the user session is active
2. WHEN the chat modal is closed and reopened, THE WebFE Application SHALL restore the chat history from session storage
3. THE chat history SHALL include both student messages and AI responses in chronological order
4. WHEN the user logs out or closes the browser, THE WebFE Application SHALL clear the chat history from session storage
5. THE WebFE Application SHALL display a maximum of 50 messages in the chat history to maintain performance

### Requirement 8

**User Story:** Là một sinh viên, tôi muốn thấy trạng thái "đang gõ" khi AI đang xử lý câu hỏi của mình, để biết rằng hệ thống đang hoạt động.

#### Acceptance Criteria

1. WHEN a student sends a message, THE WebFE Application SHALL display a typing indicator in the chat interface
2. THE typing indicator SHALL show an animation with text "AI đang suy nghĩ..."
3. WHEN the AI response is received from the WebAPI Application, THE WebFE Application SHALL remove the typing indicator
4. IF the API request takes longer than 30 seconds, THE WebFE Application SHALL display a timeout message and remove the typing indicator
5. THE send button SHALL be disabled WHILE the AI is processing a request to prevent multiple simultaneous requests

### Requirement 9

**User Story:** Là một sinh viên, tôi muốn nhận được thông báo lỗi rõ ràng khi có sự cố với AI chatbot, để biết cần làm gì tiếp theo.

#### Acceptance Criteria

1. IF the Gemini AI service returns an error, THE WebAPI Application SHALL log the error details and return a user-friendly error message
2. IF the API request fails due to network issues, THE WebFE Application SHALL display the message "Không thể kết nối đến AI Assistant. Vui lòng thử lại sau."
3. IF the student is not authenticated, THE WebAPI Application SHALL return a 401 Unauthorized status and THE WebFE Application SHALL redirect to the login page
4. IF the Gemini AI quota is exceeded, THE WebAPI Application SHALL return the message "AI Assistant tạm thời quá tải. Vui lòng thử lại sau ít phút."
5. THE error messages SHALL be displayed in the chat interface with a distinct error styling

### Requirement 10

**User Story:** Là một quản trị viên hệ thống, tôi muốn hệ thống ghi log các tương tác với AI chatbot, để có thể theo dõi việc sử dụng và khắc phục sự cố.

#### Acceptance Criteria

1. THE WebAPI Application SHALL log each chat request with student ID, timestamp, and message content
2. THE WebAPI Application SHALL log each Gemini AI response with timestamp and token usage
3. THE WebAPI Application SHALL log all errors related to AI chatbot operations with error details and stack traces
4. THE log entries SHALL include correlation IDs to track the full request-response cycle
5. THE WebAPI Application SHALL NOT log sensitive information such as API keys or personal student data beyond necessary identifiers
