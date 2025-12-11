using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Services.Emails;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _webBaseUrl;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
        _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@eduxtend.com";
        _fromName = _configuration["EmailSettings:FromName"] ?? "EduXtend System";
        _webBaseUrl = _configuration["AppSettings:WebBaseUrl"] ?? "https://localhost:3001";
    }

    public async Task SendPaymentReminderEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        var subject = daysUntilDue > 0 
            ? $"[{clubName}] Nhắc nhở: Thanh toán '{fundCollectionTitle}' sắp đến hạn"
            : $"[{clubName}] Khẩn cấp: Thanh toán '{fundCollectionTitle}' đã quá hạn";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .club-badge {{ background: rgba(255,255,255,0.2); padding: 5px 15px; border-radius: 20px; display: inline-block; margin-bottom: 10px; }}
        .alert {{ background: {(daysUntilDue > 0 ? "#fff3cd" : "#f8d7da")}; border-left: 4px solid {(daysUntilDue > 0 ? "#ffc107" : "#dc3545")}; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #007bff; }}
        .button {{ display: inline-block; background: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='club-badge'>📚 {clubName}</div>
            <h1>🔔 Nhắc nhở thanh toán</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{studentName}</strong>,</p>
            
            <div class='alert'>
                <strong>{(daysUntilDue > 0 ? $"⏰ Còn {daysUntilDue} ngày đến hạn thanh toán" : $"⚠️ Đã quá hạn {Math.Abs(daysUntilDue)} ngày")}</strong>
            </div>

            <p>Đây là email nhắc nhở về khoản thanh toán của bạn:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Câu lạc bộ:</span>
                    <span class='detail-value'>{clubName}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Khoản thu:</span>
                    <span class='detail-value'>{fundCollectionTitle}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Số tiền:</span>
                    <span class='amount'>{amount:N0} VND</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Hạn thanh toán:</span>
                    <span class='detail-value'>{dueDate:dd/MM/yyyy HH:mm}</span>
                </div>
            </div>

            <p>Vui lòng thanh toán trước hạn để tránh bị trễ hạn.</p>

            <center>
                <a href='{_webBaseUrl}/Student/MyPayments' class='button'>Thanh toán ngay</a>
            </center>

            <div class='footer'>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPaymentConfirmationEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, string paymentMethod)
    {
        var subject = $"Xác nhận thanh toán '{fundCollectionTitle}' thành công";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .success {{ background: #d1fae5; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #10b981; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Thanh toán thành công</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{studentName}</strong>,</p>
            
            <div class='success'>
                <strong>✓ Thanh toán của bạn đã được xác nhận thành công!</strong>
            </div>

            <p>Chi tiết thanh toán:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Khoản thu:</span>
                    <span class='detail-value'>{fundCollectionTitle}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Số tiền:</span>
                    <span class='amount'>{amount:N0} VND</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Phương thức:</span>
                    <span class='detail-value'>{paymentMethod}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Thời gian:</span>
                    <span class='detail-value'>{DateTime.Now:dd/MM/yyyy HH:mm}</span>
                </div>
            </div>

            <p>Cảm ơn bạn đã thanh toán đúng hạn!</p>

            <div class='footer'>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendNewFundCollectionEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate)
    {
        var subject = $"Thông báo: Khoản thu mới '{fundCollectionTitle}'";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info {{ background: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #3b82f6; }}
        .button {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📢 Khoản thu mới</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{studentName}</strong>,</p>
            
            <div class='info'>
                <strong>ℹ️ Câu lạc bộ của bạn đã tạo một khoản thu mới</strong>
            </div>

            <p>Chi tiết khoản thu:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Khoản thu:</span>
                    <span class='detail-value'>{fundCollectionTitle}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Số tiền:</span>
                    <span class='amount'>{amount:N0} VND</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Hạn thanh toán:</span>
                    <span class='detail-value'>{dueDate:dd/MM/yyyy HH:mm}</span>
                </div>
            </div>

            <p>Vui lòng thanh toán trước hạn để tránh bị trễ hạn.</p>

            <center>
                <a href='{_webBaseUrl}/Student/MyPayments' class='button'>Thanh toán ngay</a>
            </center>

            <div class='footer'>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendMonthlyReportSubmissionEmailAsync(
        string toEmail,
        string adminName,
        string clubName,
        int reportMonth,
        int reportYear,
        string submitterName,
        DateTime submittedAt,
        int reportId,
        byte[] pdfAttachment)
    {
        var monthName = GetVietnameseMonthName(reportMonth);
        var subject = $"[{clubName}] Báo cáo tháng {reportMonth}/{reportYear} đã được nộp";
        var reportUrl = $"{_webBaseUrl}/Admin/MonthlyReports/Details/{reportId}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .club-badge {{ background: rgba(255,255,255,0.2); padding: 5px 15px; border-radius: 20px; display: inline-block; margin-bottom: 10px; }}
        .info {{ background: #e0e7ff; border-left: 4px solid #6366f1; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; }}
        .button {{ display: inline-block; background: #6366f1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .attachment-note {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='club-badge'>📋 {clubName}</div>
            <h1>📊 Báo cáo tháng mới</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{adminName}</strong>,</p>
            
            <div class='info'>
                <strong>📝 Một báo cáo tháng mới đã được nộp và đang chờ phê duyệt</strong>
            </div>

            <p>Chi tiết báo cáo:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Câu lạc bộ:</span>
                    <span class='detail-value'>{clubName}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Kỳ báo cáo:</span>
                    <span class='detail-value'>Tháng {reportMonth}/{reportYear}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Người nộp:</span>
                    <span class='detail-value'>{submitterName}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Thời gian nộp:</span>
                    <span class='detail-value'>{submittedAt:dd/MM/yyyy HH:mm}</span>
                </div>
            </div>

            <div class='attachment-note'>
                <strong>📎 File đính kèm:</strong> Báo cáo PDF đã được đính kèm trong email này. Bạn có thể xem trước nội dung báo cáo mà không cần đăng nhập vào hệ thống.
            </div>

            <p>Vui lòng xem xét và phê duyệt báo cáo trong hệ thống:</p>

            <center>
                <a href='{reportUrl}' class='button'>Xem báo cáo trong hệ thống</a>
            </center>

            <div class='footer'>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        // Generate filename with sanitized club name
        var sanitizedClubName = SanitizeFileName(clubName);
        var attachmentFileName = $"MonthlyReport_{sanitizedClubName}_{reportMonth}_{reportYear}.pdf";

        await SendEmailWithAttachmentAsync(toEmail, subject, body, pdfAttachment, attachmentFileName);
    }

    private static string GetVietnameseMonthName(int month)
    {
        return month switch
        {
            1 => "Một",
            2 => "Hai",
            3 => "Ba",
            4 => "Tư",
            5 => "Năm",
            6 => "Sáu",
            7 => "Bảy",
            8 => "Tám",
            9 => "Chín",
            10 => "Mười",
            11 => "Mười Một",
            12 => "Mười Hai",
            _ => month.ToString()
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove or replace invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = fileName;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        // Replace spaces with underscores for cleaner filenames
        sanitized = sanitized.Replace(' ', '_');
        return sanitized;
    }

    private async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentContent, string attachmentFileName)
    {
        try
        {
            // Skip if no SMTP configuration
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine($"Email not sent (no SMTP config): {subject} to {toEmail}");
                return;
            }

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            // Add PDF attachment
            if (attachmentContent != null && attachmentContent.Length > 0)
            {
                var attachmentStream = new MemoryStream(attachmentContent);
                var attachment = new Attachment(attachmentStream, attachmentFileName, "application/pdf");
                mailMessage.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Email sent successfully: {subject} to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] MonthlyReportEmailNotification: Failed to send email - Recipient: {toEmail}, Error: {ex.Message}");
            // Don't throw - email failure shouldn't break the application
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Skip if no SMTP configuration
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine($"Email not sent (no SMTP config): {subject} to {toEmail}");
                return;
            }

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine($"Email sent successfully: {subject} to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
            // Don't throw - email failure shouldn't break the application
        }
    }

    public async Task SendInterviewNotificationEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes)
    {
        var subject = $"[{clubName}] Thông báo lịch phỏng vấn";

        var locationHtml = interviewType == "Online"
            ? $"<a href='{location}' style='color: #007bff; text-decoration: none; font-weight: bold;'>🔗 Tham gia Google Meet</a>"
            : $"<span style='color: #212529;'>📍 {location}</span>";

        var interviewTypeText = interviewType == "Online" ? "trực tuyến (Online)" : "trực tiếp (Offline)";
        var interviewIcon = interviewType == "Online" ? "💻" : "🏢";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .club-badge {{ background: rgba(255,255,255,0.2); padding: 5px 15px; border-radius: 20px; display: inline-block; margin-bottom: 10px; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info {{ background: #e0e7ff; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; text-align: right; }}
        .interview-type {{ display: inline-block; background: {(interviewType == "Online" ? "#e0f2fe" : "#fef3c7")}; color: {(interviewType == "Online" ? "#0369a1" : "#92400e")}; padding: 5px 15px; border-radius: 20px; font-weight: bold; }}
        .notes {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='club-badge'>📚 {clubName}</div>
            <h1>{interviewIcon} Lịch phỏng vấn mới</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{applicantName}</strong>,</p>
            
            <div class='info'>
                <strong>🎉 Chúc mừng! Bạn đã được mời tham gia phỏng vấn</strong>
            </div>

            <p>Câu lạc bộ <strong>{clubName}</strong> đã sắp xếp lịch phỏng vấn cho bạn. Vui lòng xem chi tiết bên dưới:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Hình thức:</span>
                    <span class='detail-value'><span class='interview-type'>{interviewTypeText}</span></span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Thời gian:</span>
                    <span class='detail-value'><strong>{scheduledDate:dd/MM/yyyy HH:mm}</strong></span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>{(interviewType == "Online" ? "Link tham gia:" : "Địa điểm:")}</span>
                    <span class='detail-value'>{locationHtml}</span>
                </div>
            </div>

            {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
            <div class='notes'>
                <strong>📝 Ghi chú:</strong><br/>
                {notes}
            </div>
            ")}

            <p><strong>Lưu ý quan trọng:</strong></p>
            <ul>
                <li>Vui lòng có mặt đúng giờ</li>
                {(interviewType == "Online" ? "<li>Kiểm tra kết nối internet và thiết bị trước khi tham gia</li>" : "<li>Mang theo giấy tờ tùy thân nếu cần</li>")}
                <li>Chuẩn bị tinh thần tốt nhất cho buổi phỏng vấn</li>
            </ul>

            <center>
                <a href='{_webBaseUrl}/Student/MyApplications' class='button'>Xem chi tiết đơn ứng tuyển</a>
            </center>

            <div class='footer'>
                <p>Chúc bạn thành công!</p>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendInterviewUpdateEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes)
    {
        var subject = $"[{clubName}] Cập nhật lịch phỏng vấn";

        var locationHtml = interviewType == "Online"
            ? $"<a href='{location}' style='color: #007bff; text-decoration: none; font-weight: bold;'>🔗 Tham gia Google Meet</a>"
            : $"<span style='color: #212529;'>📍 {location}</span>";

        var interviewTypeText = interviewType == "Online" ? "trực tuyến (Online)" : "trực tiếp (Offline)";
        var interviewIcon = interviewType == "Online" ? "💻" : "🏢";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .club-badge {{ background: rgba(255,255,255,0.2); padding: 5px 15px; border-radius: 20px; display: inline-block; margin-bottom: 10px; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #e9ecef; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: bold; color: #6c757d; }}
        .detail-value {{ color: #212529; text-align: right; }}
        .interview-type {{ display: inline-block; background: {(interviewType == "Online" ? "#e0f2fe" : "#fef3c7")}; color: {(interviewType == "Online" ? "#0369a1" : "#92400e")}; padding: 5px 15px; border-radius: 20px; font-weight: bold; }}
        .notes {{ background: #e0f2fe; border-left: 4px solid #0284c7; padding: 15px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #f59e0b; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='club-badge'>📚 {clubName}</div>
            <h1>🔔 Cập nhật lịch phỏng vấn</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{applicantName}</strong>,</p>
            
            <div class='warning'>
                <strong>⚠️ Lịch phỏng vấn của bạn đã được cập nhật</strong>
            </div>

            <p>Câu lạc bộ <strong>{clubName}</strong> đã thay đổi thông tin lịch phỏng vấn. Vui lòng xem chi tiết mới bên dưới:</p>

            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Hình thức:</span>
                    <span class='detail-value'><span class='interview-type'>{interviewTypeText}</span></span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Thời gian mới:</span>
                    <span class='detail-value'><strong>{scheduledDate:dd/MM/yyyy HH:mm}</strong></span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>{(interviewType == "Online" ? "Link tham gia:" : "Địa điểm mới:")}</span>
                    <span class='detail-value'>{locationHtml}</span>
                </div>
            </div>

            {(string.IsNullOrWhiteSpace(notes) ? "" : $@"
            <div class='notes'>
                <strong>📝 Ghi chú:</strong><br/>
                {notes}
            </div>
            ")}

            <p><strong>Lưu ý quan trọng:</strong></p>
            <ul>
                <li>Vui lòng có mặt đúng giờ theo lịch mới</li>
                {(interviewType == "Online" ? "<li>Kiểm tra kết nối internet và thiết bị trước khi tham gia</li>" : "<li>Lưu ý địa điểm mới để không bị nhầm lẫn</li>")}
                <li>Chuẩn bị tinh thần tốt nhất cho buổi phỏng vấn</li>
            </ul>

            <center>
                <a href='{_webBaseUrl}/Student/MyApplications' class='button'>Xem chi tiết đơn ứng tuyển</a>
            </center>

            <div class='footer'>
                <p>Chúc bạn thành công!</p>
                <p>Email này được gửi tự động từ hệ thống EduXtend</p>
                <p>Vui lòng không trả lời email này</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
