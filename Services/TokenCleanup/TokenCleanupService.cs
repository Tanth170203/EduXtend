using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.LoggedOutTokens;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Services.TokenCleanup
{
    /// <summary>
    /// Background service to automatically cleanup expired tokens from blacklist
    /// Runs every hour
    /// </summary>
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public TokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<TokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[TokenCleanup] Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TokenCleanup] Error during cleanup");
                }

                // Wait for next cleanup cycle
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("[TokenCleanup] Service stopped");
        }

        private async Task CleanupExpiredTokensAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var blacklistRepo = scope.ServiceProvider.GetRequiredService<ILoggedOutTokenRepository>();

            _logger.LogInformation("[TokenCleanup] Starting cleanup of expired tokens...");

            var removedCount = await blacklistRepo.RemoveExpiredTokensAsync();

            if (removedCount > 0)
            {
                _logger.LogInformation($"[TokenCleanup] ✅ Removed {removedCount} expired tokens");
            }
            else
            {
                _logger.LogInformation("[TokenCleanup] No expired tokens to remove");
            }
        }
    }
}
