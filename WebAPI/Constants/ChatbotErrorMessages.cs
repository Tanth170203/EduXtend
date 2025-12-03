namespace WebAPI.Constants
{
    public static class ChatbotErrorMessages
    {
        public const string NetworkError = "Không thể kết nối đến AI Assistant. Vui lòng thử lại sau.";
        public const string QuotaExceeded = "AI Assistant tạm thời quá tải. Vui lòng thử lại sau ít phút.";
        public const string InvalidApiKey = "Cấu hình AI Assistant không hợp lệ. Vui lòng liên hệ quản trị viên.";
        public const string Timeout = "Yêu cầu mất quá nhiều thời gian. Vui lòng thử lại.";
        public const string GenericError = "Đã xảy ra lỗi. Vui lòng thử lại sau.";
        public const string Unauthorized = "Bạn cần đăng nhập để sử dụng AI Assistant.";
        public const string InvalidMessage = "Tin nhắn không hợp lệ. Vui lòng kiểm tra lại.";
        public const string StudentNotFound = "Không tìm thấy thông tin sinh viên. Vui lòng liên hệ quản trị viên.";
        public const string RateLimitExceeded = "Bạn đã gửi quá nhiều tin nhắn. Vui lòng chờ một chút trước khi thử lại.";
    }
}
