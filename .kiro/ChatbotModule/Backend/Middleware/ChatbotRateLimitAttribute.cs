using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Rate limiting attribute for chatbot endpoints
    /// Limits requests to 15 per minute per user
    /// </summary>
    public class ChatbotRateLimitAttribute : ActionFilterAttribute
    {
        private const int MaxRequests = 15;
        private const int TimeWindowSeconds = 60;

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            var logger = context.HttpContext.RequestServices.GetService<ILogger<ChatbotRateLimitAttribute>>();

            if (cache == null)
            {
                logger?.LogWarning("IMemoryCache not available, skipping rate limiting");
                await next();
                return;
            }

            // Get userId from claims
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                // If no user ID, let the request through (authorization will handle it)
                await next();
                return;
            }

            var cacheKey = $"chatbot_ratelimit_{userId}";
            
            // Get or create rate limit entry
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry))
            {
                entry = new RateLimitEntry
                {
                    RequestCount = 0,
                    WindowStart = DateTime.UtcNow
                };
            }

            // Check if we need to reset the window
            var timeSinceWindowStart = DateTime.UtcNow - entry!.WindowStart;
            if (timeSinceWindowStart.TotalSeconds >= TimeWindowSeconds)
            {
                // Reset the window
                entry = new RateLimitEntry
                {
                    RequestCount = 0,
                    WindowStart = DateTime.UtcNow
                };
            }

            // Check if limit exceeded
            if (entry.RequestCount >= MaxRequests)
            {
                var secondsRemaining = (int)(TimeWindowSeconds - timeSinceWindowStart.TotalSeconds);
                if (secondsRemaining < 0) secondsRemaining = 0;

                logger?.LogWarning(
                    "Rate limit exceeded for user {UserId}. Requests: {RequestCount}/{MaxRequests}",
                    userId, entry.RequestCount, MaxRequests);

                // Record rate limit hit in metrics
                var metricsService = context.HttpContext.RequestServices.GetService<Services.Chatbot.IChatbotMetricsService>();
                metricsService?.RecordRateLimitHit();

                context.Result = new ObjectResult(new
                {
                    message = $"Bạn đã gửi quá nhiều tin nhắn. Vui lòng đợi {secondsRemaining} giây.",
                    retryAfterSeconds = secondsRemaining
                })
                {
                    StatusCode = 429
                };

                // Add Retry-After header
                context.HttpContext.Response.Headers["Retry-After"] = secondsRemaining.ToString();

                return;
            }

            // Increment request count
            entry.RequestCount++;

            // Store in cache with expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(TimeWindowSeconds)
            };
            cache.Set(cacheKey, entry, cacheOptions);

            logger?.LogDebug(
                "Rate limit check passed for user {UserId}. Requests: {RequestCount}/{MaxRequests}",
                userId, entry.RequestCount, MaxRequests);

            await next();
        }

        private class RateLimitEntry
        {
            public int RequestCount { get; set; }
            public DateTime WindowStart { get; set; }
        }
    }
}
