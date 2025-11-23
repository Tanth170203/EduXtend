using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BusinessObject.Models;
using Utils;

namespace Services.FundCollections;

/// <summary>
/// Background service that automatically completes fund collections
/// - When all payments are collected
/// - When due date has passed
/// Creates Transaction (Income) and marks as completed
/// Runs every day at 10:00 AM
/// </summary>
public class FundCollectionAutoCompleteService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FundCollectionAutoCompleteService> _logger;

    public FundCollectionAutoCompleteService(
        IServiceProvider serviceProvider,
        ILogger<FundCollectionAutoCompleteService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fund Collection Auto Complete Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation($"Next fund collection auto-complete check scheduled at {nextRun:yyyy-MM-dd HH:mm:ss} UTC");

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessAutoCompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Fund Collection Auto Complete Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Wait 1 hour before retry
            }
        }
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        // Run at 10:00 AM UTC every day
        var nextRun = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0, DateTimeKind.Utc);
        
        if (now >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }
        
        return nextRun;
    }

    private async Task ProcessAutoCompleteAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("Starting fund collection auto-complete check");

            var today = DateTime.UtcNow.Date;

            // Get active fund collections that should be completed
            var fundCollections = await context.FundCollectionRequests
                .Include(f => f.Club)
                .Include(f => f.Payments)
                .Where(f => f.Status == "active" && 
                           (f.DueDate.Date < today || // Past due date
                            f.Payments.All(p => p.Status == "paid"))) // All payments collected
                .ToListAsync();

            _logger.LogInformation($"Found {fundCollections.Count} fund collections to auto-complete");

            foreach (var fundCollection in fundCollections)
            {
                try
                {
                    await CompleteFundCollectionAsync(context, fundCollection);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to complete fund collection {fundCollection.Id}");
                }
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Fund collection auto-complete check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fund collection auto-complete process");
            throw;
        }
    }

    private async Task CompleteFundCollectionAsync(EduXtendContext context, FundCollectionRequest fundCollection)
    {
        // Calculate total collected amount
        var totalCollected = fundCollection.Payments
            .Where(p => p.Status == "paid")
            .Sum(p => p.Amount);

        if (totalCollected <= 0)
        {
            _logger.LogInformation($"Fund collection {fundCollection.Id} has no collected payments, skipping");
            return;
        }

        // Create PaymentTransaction (Income)
        var transaction = new PaymentTransaction
        {
            ClubId = fundCollection.ClubId,
            Type = "Income",
            Amount = totalCollected,
            Title = $"Fund Collection: {fundCollection.Title}",
            Description = $"Auto-completed fund collection. Total collected from {fundCollection.Payments.Count(p => p.Status == "paid")} members.",
            Category = "member_fees",
            Method = "Multiple",
            Status = "completed",
            TransactionDate = DateTimeHelper.Now,
            CreatedAt = DateTimeHelper.Now,
            CreatedById = 1 // System user
        };

        context.PaymentTransactions.Add(transaction);

        // Update fund collection status
        fundCollection.Status = "completed";
        fundCollection.UpdatedAt = DateTimeHelper.Now;

        var reason = fundCollection.Payments.All(p => p.Status == "paid") 
            ? "All payments collected" 
            : "Due date passed";

        _logger.LogInformation(
            $"Completed fund collection {fundCollection.Id} '{fundCollection.Title}' " +
            $"for club {fundCollection.Club?.Name}. " +
            $"Total collected: {totalCollected:N0} VND. Reason: {reason}");
    }
}
