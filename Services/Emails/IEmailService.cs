namespace Services.Emails;

public interface IEmailService
{
    Task SendPaymentReminderEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate, int daysUntilDue);
    Task SendPaymentConfirmationEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, string paymentMethod);
    Task SendNewFundCollectionEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate);
    
    /// <summary>
    /// Sends email notification to Admin when Monthly Report is submitted
    /// </summary>
    /// <param name="toEmail">Admin email address</param>
    /// <param name="adminName">Admin full name</param>
    /// <param name="clubName">Club name</param>
    /// <param name="reportMonth">Report month (1-12)</param>
    /// <param name="reportYear">Report year</param>
    /// <param name="submitterName">Name of Club Manager who submitted</param>
    /// <param name="submittedAt">Submission timestamp</param>
    /// <param name="reportId">Report ID for direct link</param>
    /// <param name="pdfAttachment">PDF file content as byte array</param>
    Task SendMonthlyReportSubmissionEmailAsync(
        string toEmail,
        string adminName,
        string clubName,
        int reportMonth,
        int reportYear,
        string submitterName,
        DateTime submittedAt,
        int reportId,
        byte[] pdfAttachment);

    /// <summary>
    /// Sends interview notification email to applicant
    /// </summary>
    Task SendInterviewNotificationEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes);

    /// <summary>
    /// Sends interview update notification email
    /// </summary>
    Task SendInterviewUpdateEmailAsync(
        string toEmail,
        string applicantName,
        string clubName,
        DateTime scheduledDate,
        string interviewType,
        string location,
        string? notes);
}
