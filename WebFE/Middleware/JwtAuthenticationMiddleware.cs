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
                        
                        if (jwt.ValidTo < DateTime.UtcNow)
                        {
                            context.Response.Redirect("/Auth/Login");
                            return;
                        }

                        var roles = jwt.Claims
                            .Where(c => c.Type == ClaimTypes.Role || 
                                       c.Type == "role" || 
                                       c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            .Select(c => c.Value)
                            .ToList();

                        if (!HasAccess(path, roles))
                        {
                            _logger.LogWarning("Access denied for user with roles [{Roles}] to {Path}", string.Join(", ", roles), path);
                            context.Response.Redirect("/Error?code=403");
                            return;
                        }

                        SetUserContext(context, jwt);
                    }
                    else
                    {
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
            else if (IsProtectedPage(path))
            {
                context.Response.Redirect("/Auth/Login");
                return;
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
