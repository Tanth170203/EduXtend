namespace Services.Emails;

public interface IEmailService
{
    Task SendPaymentReminderEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate, int daysUntilDue);
    Task SendPaymentConfirmationEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, string paymentMethod);
    Task SendNewFundCollectionEmailAsync(string toEmail, string studentName, string clubName, string fundCollectionTitle, decimal amount, DateTime dueDate);
}
