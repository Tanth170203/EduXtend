using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Notifications;

namespace Services.FundCollections;

/// <summary>
/// Background service that automatically sends payment reminders
/// - 3 days before due date
/// - 1 day before due date
/// Runs every day at 9:00 AM
/// </summary>
public class PaymentReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReminderBackgroundService> _logger;

    public PaymentReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PaymentReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Reminder Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation($"Next payment reminder check scheduled at {nextRun:yyyy-MM-dd HH:mm:ss} UTC");

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await SendAutomaticRemindersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Payment Reminder Background Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Wait 1 hour before retry
            }
        }
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        // Run at 9:00 AM UTC every day
        var nextRun = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0, DateTimeKind.Utc);
        
        if (now >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }

    private async Task SendAutomaticRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EduXtendContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            _logger.LogInformation("Starting automatic payment reminder check");

            var today = DateTime.UtcNow.Date;
            var threeDaysLater = today.AddDays(3);
            var oneDayLater = today.AddDays(1);

            // Get all active fund collection requests with pending payments
            var pendingPayments = await context.FundCollectionPayments
                .Include(p => p.FundCollectionRequest)
                    .ThenInclude(r => r.Club)
                .Include(p => p.ClubMember)
                    .ThenInclude(cm => cm.Student)
                .Where(p => 
                    p.Status == "pending" &&
                    p.FundCollectionRequest.Status == "active" &&
                    (p.FundCollectionRequest.DueDate.Date == threeDaysLater || 
                     p.FundCollectionRequest.DueDate.Date == oneDayLater))
                .ToListAsync();

            _logger.LogInformation($"Found {pendingPayments.Count} payments requiring reminders");

            foreach (var payment in pendingPayments)
            {
                try
                {
                    var dueDate = payment.FundCollectionRequest.DueDate;
                    var daysUntilDue = (dueDate.Date - today).Days;

                    // Send in-app notification only (no email)
                    await notificationService.NotifyMemberAboutPaymentReminderAsync(
                        payment.ClubMember.Student.UserId,
                        payment.FundCollectionRequest.ClubId,
                        payment.FundCollectionRequest.Title,
                        payment.Amount,
                        dueDate,
                        daysUntilDue
                    );

                    _logger.LogInformation(
                        $"Sent automatic reminder to user {payment.ClubMember.Student.UserId} " +
                        $"for payment {payment.Id} ({daysUntilDue} days until due)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send reminder for payment {payment.Id}");
                }
            }

            _logger.LogInformation("Automatic payment reminder check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic payment reminder process");
            throw;
        }
    }
}
