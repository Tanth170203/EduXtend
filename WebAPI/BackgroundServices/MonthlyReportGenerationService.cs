using Repositories.Clubs;
using Services.MonthlyReports;

namespace WebAPI.BackgroundServices
{
    /// <summary>
    /// Background service that automatically generates monthly reports for all active clubs
    /// on the first day of each month at 00:00
    /// Requirements: 1.1, 1.2, 1.3, 1.4
    /// </summary>
    public class MonthlyReportGenerationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyReportGenerationService> _logger;

        public MonthlyReportGenerationService(
            IServiceProvider serviceProvider,
            ILogger<MonthlyReportGenerationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monthly Report Generation Service started");

            // On startup, immediately check and generate reports for current month if missing
            try
            {
                _logger.LogInformation("Checking for missing monthly reports on startup...");
                await GenerateMonthlyReportsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reports on startup");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    
                    // Calculate next run time: 00:00 on the 1st of next month
                    var nextRun = CalculateNextRunTime(now);
                    var delay = nextRun - now;

                    _logger.LogInformation(
                        "Monthly Report Generation Service: Next run scheduled at {NextRun} (in {Hours} hours, {Minutes} minutes)",
                        nextRun, 
                        (int)delay.TotalHours, 
                        delay.Minutes);

                    // Wait until the scheduled time
                    await Task.Delay(delay, stoppingToken);

                    // Generate reports for all active clubs
                    await GenerateMonthlyReportsAsync();
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Monthly Report Generation Service");
                    
                    // Wait 1 hour before retrying in case of error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Monthly Report Generation Service stopped");
        }

        /// <summary>
        /// Calculate the next run time: 00:00 on the 1st of next month
        /// </summary>
        private DateTime CalculateNextRunTime(DateTime now)
        {
            // If today is the 1st and it's before 00:30, run now
            if (now.Day == 1 && now.Hour == 0 && now.Minute < 30)
            {
                return now;
            }

            // Otherwise, calculate the 1st of next month at 00:00
            var nextMonth = now.Month == 12 ? 1 : now.Month + 1;
            var nextYear = now.Month == 12 ? now.Year + 1 : now.Year;
            
            return new DateTime(nextYear, nextMonth, 1, 0, 0, 0);
        }

        /// <summary>
        /// Generate monthly reports for all active clubs
        /// </summary>
        private async Task GenerateMonthlyReportsAsync()
        {
            _logger.LogInformation("Starting monthly report generation for all active clubs");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var clubRepository = scope.ServiceProvider.GetRequiredService<IClubRepository>();
                var monthlyReportService = scope.ServiceProvider.GetRequiredService<IMonthlyReportService>();

                // Get all active clubs
                var clubs = await clubRepository.SearchClubsAsync(null, null, isActive: true);
                
                if (clubs == null || clubs.Count == 0)
                {
                    _logger.LogInformation("No active clubs found");
                    return;
                }

                _logger.LogInformation("Found {Count} active clubs", clubs.Count);

                // Get current month and year for the report
                var now = DateTime.Now;
                var reportMonth = now.Month;
                var reportYear = now.Year;

                int successCount = 0;
                int skipCount = 0;
                int errorCount = 0;

                // Generate report for each club
                foreach (var club in clubs)
                {
                    try
                    {
                        // Check if report already exists for this month
                        var existingReports = await monthlyReportService.GetAllReportsAsync(club.Id);
                        var reportExists = existingReports.Any(r => 
                            r.ReportMonth == reportMonth && 
                            r.ReportYear == reportYear);

                        if (reportExists)
                        {
                            _logger.LogInformation(
                                "Report for {Month}/{Year} already exists for club {ClubName} (ID: {ClubId}), skipping",
                                reportMonth, reportYear, club.Name, club.Id);
                            skipCount++;
                            continue;
                        }

                        // Create new monthly report
                        var reportId = await monthlyReportService.CreateMonthlyReportAsync(
                            club.Id, 
                            reportMonth, 
                            reportYear);

                        _logger.LogInformation(
                            "Successfully created monthly report (ID: {ReportId}) for club {ClubName} (ID: {ClubId}) - {Month}/{Year}",
                            reportId, club.Name, club.Id, reportMonth, reportYear);
                        
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Failed to create monthly report for club {ClubName} (ID: {ClubId})",
                            club.Name, club.Id);
                        errorCount++;
                    }
                }

                _logger.LogInformation(
                    "Monthly report generation completed: {Success} created, {Skip} skipped, {Error} errors",
                    successCount, skipCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monthly report generation process");
                throw;
            }
        }
    }
}
