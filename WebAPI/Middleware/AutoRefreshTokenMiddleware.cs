using BusinessObject.DTOs.GGLogin;
using Microsoft.Extensions.Options;
using Services.GGLogin;
using System.IdentityModel.Tokens.Jwt;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Middleware to automatically refresh access token when it's about to expire
    /// </summary>
    public class AutoRefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AutoRefreshTokenMiddleware> _logger;
        private const int REFRESH_THRESHOLD_MINUTES = 5; // Refresh if token expires in less than 5 minutes

        public AutoRefreshTokenMiddleware(RequestDelegate next, ILogger<AutoRefreshTokenMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IOptions<JwtOptions> jwtOptions)
        {
            // Skip for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Check if access token needs refresh
            if (context.Request.Cookies.TryGetValue("AccessToken", out var accessToken) &&
                context.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(accessToken))
                    {
                        var jwt = handler.ReadJwtToken(accessToken);
                        var expiresAt = jwt.ValidTo;
                        var timeUntilExpiry = expiresAt - DateTime.UtcNow;

                        // If token expires in less than threshold, refresh it
                        if (timeUntilExpiry.TotalMinutes < REFRESH_THRESHOLD_MINUTES && timeUntilExpiry.TotalSeconds > 0)
                        {
                            _logger.LogInformation("Token expiring soon, auto-refreshing...");
                            
                            var user = await tokenService.ValidateRefreshTokenAsync(refreshToken);
                            if (user != null)
                            {
                                var newAccessToken = tokenService.GenerateAccessToken(user);
                                var newRefreshToken = await tokenService.GenerateAndSaveRefreshTokenAsync(user, "Web");

                                // Update cookies
                                SetAuthCookies(context, newAccessToken, newRefreshToken, jwtOptions.Value);
                                
                                _logger.LogInformation("Token auto-refreshed successfully for user {UserId}", user.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-refresh token, continuing with existing token");
                    // Don't block the request, let it continue
                }
            }

            await _next(context);
        }

        private void SetAuthCookies(HttpContext context, string accessToken, string refreshToken, JwtOptions jwtOptions)
        {
            context.Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, // Fixed: Changed from None to Lax
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenLifetimeMinutes),
                IsEssential = true
            });

            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, // Fixed: Changed from None to Lax
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenLifetimeDays),
                IsEssential = true
            });
        }

        private bool IsPublicEndpoint(string path)
        {
            var publicPaths = new[]
            {
                "/api/auth/google",
                "/api/auth/refresh",
                "/api/auth/token-status",
                "/swagger",
                "/health"
            };

            return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class AutoRefreshTokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseAutoRefreshToken(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AutoRefreshTokenMiddleware>();
        }
    }
}
