using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebFE.Middleware
{
    /// <summary>
    /// Middleware to validate JWT from cookie and protect pages based on roles
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Skip for public pages
            if (IsPublicPage(path))
            {
                await _next(context);
                return;
            }

            // Extract and validate token
            if (context.Request.Cookies.TryGetValue("AccessToken", out var token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        
                        // Check if token is expired
                        if (jwt.ValidTo < DateTime.UtcNow)
                        {
                            _logger.LogWarning("Token expired, redirecting to login");
                            context.Response.Redirect("/Auth/Login");
                            return;
                        }

                        // Extract roles from token
                        var allClaims = jwt.Claims.ToList();
                        _logger.LogInformation("JWT Claims for user: {@Claims}", 
                            allClaims.Select(c => new { c.Type, c.Value }));

                        var roles = jwt.Claims
                            .Where(c => c.Type == ClaimTypes.Role || 
                                       c.Type == "role" || 
                                       c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            .Select(c => c.Value)
                            .ToList();

                        _logger.LogInformation("Extracted roles: [{Roles}] for path: {Path}", 
                            string.Join(", ", roles), path);

                        // Check role-based access
                        if (!HasAccess(path, roles))
                        {
                            _logger.LogWarning("User with roles [{Roles}] attempted to access {Path}", string.Join(", ", roles), path);
                            context.Response.Redirect("/Error?code=403");
                            return;
                        }

                        // Set user context (optional - for server-side rendering)
                        SetUserContext(context, jwt);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid token format, redirecting to login");
                        context.Response.Redirect("/Auth/Login");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating JWT token");
                    context.Response.Redirect("/Auth/Login");
                    return;
                }
            }
            else
            {
                // No token found for protected page
                if (IsProtectedPage(path))
                {
                    _logger.LogInformation("No token found for protected page {Path}, redirecting to login", path);
                    context.Response.Redirect("/Auth/Login");
                    return;
                }
            }

            await _next(context);
        }

        private bool IsPublicPage(string path)
        {
            var publicPages = new[]
            {
                "/",
                "/index",
                "/auth/login",
                "/error",
                "/privacy",
                "/lib/",
                "/css/",
                "/js/",
                "/images/",
                "/favicon.ico",
                "/_framework/"
            };

            return publicPages.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsProtectedPage(string path)
        {
            var protectedPrefixes = new[] { "/admin/", "/club/", "/student/" };
            return protectedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasAccess(string path, List<string> userRoles)
        {
            // Admin pages - only Admin
            if (path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            }

            // Club pages - Admin, ClubManager, ClubMember
            if (path.StartsWith("/club/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any(r => 
                    r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("ClubManager", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("ClubMember", StringComparison.OrdinalIgnoreCase));
            }

            // Student pages - All authenticated users
            if (path.StartsWith("/student/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any();
            }

            // Default: allow if authenticated
            return userRoles.Any();
        }

        private void SetUserContext(HttpContext context, JwtSecurityToken jwt)
        {
            // Create claims identity from JWT
            var claims = jwt.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);
            
            context.User = principal;
        }
    }

    public static class JwtAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}
