using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebAPI.Constants;

namespace WebAPI.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Check if rate limit was exceeded (status code 429)
            if (context.Response.StatusCode == 429)
            {
                // Check if this is a chatbot endpoint
                if (context.Request.Path.StartsWithSegments("/api/chatbot"))
                {
                    _logger.LogWarning("Rate limit exceeded for chatbot endpoint. IP: {IpAddress}, Path: {Path}",
                        context.Connection.RemoteIpAddress, context.Request.Path);

                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        message = ChatbotErrorMessages.RateLimitExceeded,
                        success = false,
                        errorCode = "RATE_LIMIT_EXCEEDED",
                        timestamp = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(errorResponse);
                    await context.Response.WriteAsync(json);
                }
            }
        }
    }

    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomRateLimitResponse(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitMiddleware>();
        }
    }
}
