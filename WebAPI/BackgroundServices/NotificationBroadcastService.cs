using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using Repositories.Notifications;
using Utils;

namespace WebAPI.BackgroundServices
{
    public class NotificationBroadcastService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBroadcastService> _logger;

        public NotificationBroadcastService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBroadcastService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Broadcast Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                    // Get unbroadcasted notifications (created in last 10 seconds)
                    var recentTime = DateTimeHelper.Now.AddSeconds(-10);
                    var notifications = await notificationRepo.GetRecentUnreadAsync(recentTime);

                    foreach (var notification in notifications)
                    {
                        if (notification.TargetUserId.HasValue)
                        {
                            await hubContext.Clients.Group($"user_{notification.TargetUserId.Value}")
                                .SendAsync("ReceiveNotification", new
                                {
                                    id = notification.Id,
                                    title = notification.Title,
                                    message = notification.Message,
                                    type = MapScopeToType(notification.Scope),
                                    scope = notification.Scope,
                                    targetClubId = notification.TargetClubId,
                                    targetRole = notification.TargetRole,
                                    createdAt = notification.CreatedAt,
                                    isRead = notification.IsRead
                                }, stoppingToken);

                            _logger.LogInformation("Broadcasted notification {NotificationId} to user {UserId}", 
                                notification.Id, notification.TargetUserId.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Notification Broadcast Service");
                }

                // Check every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("Notification Broadcast Service stopped");
        }

        private string MapScopeToType(string scope)
        {
            return scope switch
            {
                "User" => "info",
                "Club" => "info",
                "System" => "warning",
                _ => "info"
            };
        }
    }
}
