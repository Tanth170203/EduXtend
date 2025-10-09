using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.Semesters;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Semesters
{
    /// <summary>
    /// Background service tự động cập nhật IsActive cho các học kỳ
    /// Chạy mỗi ngày vào 00:01 sáng
    /// </summary>
    public class SemesterAutoUpdateService : BackgroundService
    {
        private readonly ILogger<SemesterAutoUpdateService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _period = TimeSpan.FromDays(1); // Chạy mỗi ngày

        public SemesterAutoUpdateService(
            ILogger<SemesterAutoUpdateService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Semester Auto Update Service started.");

            // Chạy ngay khi start
            await UpdateSemesterStatus(stoppingToken);

            // Sau đó chạy theo schedule
            using var timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await UpdateSemesterStatus(stoppingToken);
            }

            _logger.LogInformation("Semester Auto Update Service stopped.");
        }

        private async Task UpdateSemesterStatus(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting Semester status update at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ISemesterRepository>();

                var semesters = await repository.GetAllAsync();
                var now = DateTime.UtcNow.Date;
                bool hasChanges = false;

                foreach (var semester in semesters)
                {
                    var shouldBeActive = now >= semester.StartDate.Date && now <= semester.EndDate.Date;

                    // Nếu trạng thái IsActive không đúng với thực tế
                    if (semester.IsActive != shouldBeActive)
                    {
                        semester.IsActive = shouldBeActive;
                        await repository.UpdateAsync(semester);
                        hasChanges = true;

                        _logger.LogInformation(
                            "Updated Semester '{Name}' (ID: {Id}): IsActive = {IsActive}",
                            semester.Name, semester.Id, shouldBeActive);
                    }
                }

                if (!hasChanges)
                {
                    _logger.LogInformation("No Semester status changes needed.");
                }
                else
                {
                    _logger.LogInformation("Semester status update completed successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Semester status.");
            }
        }
    }
}

