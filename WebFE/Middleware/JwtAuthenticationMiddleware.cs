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
            
            _logger.LogWarning("JwtAuthMiddleware: Processing path {Path}", path);

            // Skip static files and public pages early
            if (IsPublicPage(path))
            {
                // Still try to set user context if token exists
                if (context.Request.Cookies.TryGetValue("AccessToken", out var publicToken))
                {
                    TrySetUserContext(context, publicToken);
                }
                await _next(context);
                return;
            }

            // For protected pages, require valid token
            if (!context.Request.Cookies.TryGetValue("AccessToken", out var token) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JwtAuthMiddleware: No token found for protected path {Path}", path);
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized - No token");
                    return;
                }
                context.Response.Redirect("/Auth/Login");
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("JwtAuthMiddleware: Invalid token format for path {Path}", path);
                    if (IsAjaxRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized - Invalid token");
                        return;
                    }
                    context.Response.Redirect("/Auth/Login");
                    return;
                }

                var jwt = handler.ReadJwtToken(token);
                
                // Check if token is expired
                if (jwt.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("JwtAuthMiddleware: Token expired for path {Path}", path);
                    if (IsAjaxRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized - Token expired");
                        return;
                    }
                    context.Response.Redirect("/Auth/Login");
                    return;
                }

                // Token is valid, set user context
                SetUserContext(context, jwt);

                // Get all roles from token
                var roles = jwt.Claims
                    .Where(c => c.Type == ClaimTypes.Role || 
                               c.Type == "role" || 
                               c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList();

                _logger.LogWarning("JwtAuthMiddleware: User roles [{Roles}] accessing {Path}", string.Join(", ", roles), path);

                // Check role-based access
                if (!HasAccess(path, roles))
                {
                    _logger.LogWarning("JwtAuthMiddleware: ACCESS DENIED for user with roles [{Roles}] to {Path}", string.Join(", ", roles), path);
                    
                    if (IsAjaxRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Access Denied - Insufficient permissions");
                        return;
                    }
                    
                    context.Response.Redirect("/AccessDenied");
                    return;
                }

                _logger.LogWarning("JwtAuthMiddleware: Access GRANTED for path {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JwtAuthMiddleware: Error validating JWT token for path {Path}", path);
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized - Token error");
                    return;
                }
                context.Response.Redirect("/Auth/Login");
                return;
            }

            await _next(context);
        }

        private void TrySetUserContext(HttpContext context, string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    if (jwt.ValidTo >= DateTime.UtcNow)
                    {
                        SetUserContext(context, jwt);
                    }
                }
            }
            catch { /* Ignore errors for public pages */ }
        }

        private bool IsPublicPage(string path)
        {
            // Exact match pages
            var exactMatchPages = new[]
            {
                "/",
                "/index",
                "/error",
                "/accessdenied",
                "/privacy"
            };

            // Prefix match pages (paths that start with these)
            var prefixMatchPages = new[]
            {
                "/auth/",
                "/news",
                "/lib/",
                "/css/",
                "/js/",
                "/images/",
                "/img/",
                "/favicon.ico",
                "/_framework/",
                "/.well-known/"
            };

            // Check exact matches first
            if (exactMatchPages.Contains(path, StringComparer.OrdinalIgnoreCase))
                return true;

            // Check prefix matches
            return prefixMatchPages.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsProtectedPage(string path)
        {
            // All pages that require authentication
            var protectedPrefixes = new[] 
            { 
                "/admin/",      // Admin dashboard
                "/club/",       // Legacy club pages
                "/clubs/",      // Club pages (MemberDashboard, ClubScore, etc.)
                "/student/",    // Student pages
                "/clubmanager/", // Club manager pages
                "/activities/", // Activity pages (some may need auth)
                "/profile",     // User profile
                "/settings"     // User settings
            };
            return protectedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasAccess(string path, List<string> userRoles)
        {
            // Admin pages - only Admin role
            if (path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            }

            // ClubManager pages - ClubManager and Admin roles
            if (path.StartsWith("/clubmanager/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any(r => 
                    r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("ClubManager", StringComparison.OrdinalIgnoreCase));
            }

            // Club pages (both /club/ and /clubs/) - All authenticated users
            // Note: Specific membership checks are done at page level
            if (path.StartsWith("/club/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                // Allow all authenticated users - membership verification is done at page level
                return userRoles.Any();
            }

            // Student pages - All authenticated users
            if (path.StartsWith("/student/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any();
            }

            // Activities pages - All authenticated users
            if (path.StartsWith("/activities/", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any();
            }

            // Profile and Settings - All authenticated users
            if (path.StartsWith("/profile", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
            {
                return userRoles.Any();
            }

            // Default: allow if authenticated
            return userRoles.Any();
        }

        private void SetUserContext(HttpContext context, JwtSecurityToken jwt)
        {
            var claims = jwt.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;
        }

        private bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.Headers["Accept"].ToString().Contains("application/json");
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
