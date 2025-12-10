using Services.Activities;

namespace WebAPI.BackgroundServices
{
    public class ActivityAutoCompleteService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ActivityAutoCompleteService> _logger;

        public ActivityAutoCompleteService(
            IServiceProvider serviceProvider,
            ILogger<ActivityAutoCompleteService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Activity Auto-Complete Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var activityService = scope.ServiceProvider.GetRequiredService<IActivityService>();

                    // Call AutoCompleteActivitiesAsync to update activities from "Approved" to "Completed"
                    var updatedCount = await activityService.AutoCompleteActivitiesAsync();

                    if (updatedCount > 0)
                    {
                        _logger.LogInformation("Activity Auto-Complete Service: {Count} activities updated from 'Approved' to 'Completed'", updatedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Activity Auto-Complete Service");
                }

                // Check every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Activity Auto-Complete Service stopped");
        }
    }
}
