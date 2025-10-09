using Repositories.LoggedOutTokens;
using System.IdentityModel.Tokens.Jwt;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Middleware to check if the JWT token has been blacklisted (logged out)
    /// </summary>
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ILoggedOutTokenRepository blacklistRepo)
        {
            // Skip for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract token from cookie or Authorization header
            var token = ExtractToken(context);
            
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Check if token is blacklisted
                    if (await blacklistRepo.IsTokenBlacklistedAsync(token))
                    {
                        _logger.LogWarning("Blocked request with blacklisted token");
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked" });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking token blacklist");
                    // Continue processing - fail open
                }
            }

            await _next(context);
        }

        private string? ExtractToken(HttpContext context)
        {
            // Priority 1: Authorization header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // Priority 2: Cookie
            if (context.Request.Cookies.TryGetValue("AccessToken", out var token))
            {
                return token?.Trim();
            }

            return null;
        }

        private bool IsPublicEndpoint(string path)
        {
            var publicPaths = new[]
            {
                "/api/auth/google",
                "/api/auth/refresh",
                "/api/auth/token-status",
                "/api/auth/debug",
                "/swagger",
                "/health"
            };

            return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class TokenBlacklistMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}



