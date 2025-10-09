using BusinessObject.DTOs.GGLogin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Repositories.LoggedOutTokens;
using Services.GGLogin;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ITokenService _tokenService;
        private readonly ILoggedOutTokenRepository _blacklistRepo;
        private readonly JwtOptions _jwt;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IGoogleAuthService googleAuthService,
            ITokenService tokenService,
            ILoggedOutTokenRepository blacklistRepo,
            IOptions<JwtOptions> jwtOptions,
            ILogger<AuthController> logger)
        {
            _googleAuthService = googleAuthService;
            _tokenService = tokenService;
            _blacklistRepo = blacklistRepo;
            _jwt = jwtOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Login with Google ID Token
        /// </summary>
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var user = await _googleAuthService.LoginWithGoogleAsync(request.IdToken, request.DeviceInfo ?? "Web");
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(user, request.DeviceInfo ?? "Web");

                // Validate token format
                if (!IsValidJwtFormat(accessToken))
                {
                    _logger.LogError("Generated token has invalid format");
                    return StatusCode(500, new { message = "Token generation failed" });
                }

                SetAuthCookies(accessToken, refreshToken);

                var roles = user.UserRoles.Select(r => r.Role.RoleName).ToList();
                string redirectUrl = DetermineRedirectUrl(roles);

                _logger.LogInformation("User {Email} logged in successfully with roles: {Roles}", user.Email, string.Join(", ", roles));

                // Return tokens in response body for WebFE to set cookies
                return Ok(new
                {
                    message = "Login successful",
                    redirectUrl,
                    accessToken,  // Include for WebFE
                    refreshToken, // Include for WebFE
                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        user.AvatarUrl,
                        Roles = roles
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized login attempt: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return BadRequest(new { message = "Login failed. Please try again." });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            {
                return Unauthorized(new { message = "Missing refresh token" });
            }

            try
            {
                var user = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                // ✅ No need to blacklist old token during refresh
                // Token will expire naturally after 30 minutes
                // Blacklist only needed on explicit logout

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(user, "Web");

                SetAuthCookies(newAccessToken, newRefreshToken);

                _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

                return Ok(new { message = "Token refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return BadRequest(new { message = "Token refresh failed" });
            }
        }

        /// <summary>
        /// Logout and blacklist current tokens
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();

                // Blacklist current access token
                var token = ExtractToken();
                if (!string.IsNullOrEmpty(token))
                {
                    await BlacklistTokenAsync(token, userId, "User logout");
                }

                // Revoke refresh token
                if (Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                {
                    await _tokenService.RevokeTokenAsync(refreshToken);
                }

                ClearAuthCookies();

                _logger.LogInformation("User {UserId} logged out successfully", userId);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return BadRequest(new { message = "Logout failed" });
            }
        }

        /// <summary>
        /// Get current authenticated user info
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var avatar = User.FindFirst("avatar")?.Value;
                var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                return Ok(new
                {
                    id = userId,
                    name = fullName,
                    email,
                    avatar,
                    roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Failed to get user info" });
            }
        }

        /// <summary>
        /// Debug endpoint to check cookies
        /// </summary>
        [HttpGet("debug-cookies")]
        public IActionResult DebugCookies()
        {
            var cookies = new Dictionary<string, string>();
            
            foreach (var cookie in Request.Cookies)
            {
                cookies[cookie.Key] = cookie.Value;
            }

            return Ok(new
            {
                message = "Cookie debug info",
                totalCookies = cookies.Count,
                cookies = cookies,
                hasAccessToken = Request.Cookies.ContainsKey("AccessToken"),
                hasRefreshToken = Request.Cookies.ContainsKey("RefreshToken"),
                userAgent = Request.Headers["User-Agent"].ToString(),
                origin = Request.Headers["Origin"].ToString(),
                referer = Request.Headers["Referer"].ToString()
            });
        }

        /// <summary>
        /// Check token expiration status
        /// </summary>
        [HttpGet("token-status")]
        public IActionResult GetTokenStatus()
        {
            if (!Request.Cookies.TryGetValue("AccessToken", out var token))
            {
                return Ok(new 
                { 
                    status = "No token",
                    authenticated = false,
                    message = "No AccessToken cookie found. Please login first."
                });
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                
                var exp = jwtToken.ValidTo;
                var now = DateTime.UtcNow;
                var timeRemaining = exp - now;
                
                var hasRefreshToken = Request.Cookies.ContainsKey("RefreshToken");
                var isExpired = timeRemaining.TotalSeconds <= 0;

                return Ok(new
                {
                    status = isExpired ? "Expired" : "Valid",
                    authenticated = !isExpired,
                    expiresAt = exp.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    timeRemainingSeconds = (int)timeRemaining.TotalSeconds,
                    hasRefreshToken = hasRefreshToken,
                    canAutoRefresh = hasRefreshToken && !isExpired,
                    user = new
                    {
                        id = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                        email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                        name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                        roles = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token status");
                return BadRequest(new { error = "Invalid token", message = ex.Message });
            }
        }

        #region Helper Methods

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            // Log token info for debugging
            _logger.LogInformation("Setting cookies - AccessToken length: {Length}, RefreshToken length: {RefreshLength}", 
                accessToken.Length, refreshToken.Length);
            _logger.LogInformation("AccessToken preview: {Preview}...", accessToken.Substring(0, Math.Min(50, accessToken.Length)));

            // Use Lax for same-site requests (WebFE and WebAPI on same domain)
            Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, // Changed from None to Lax
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes),
                IsEssential = true
            });

            Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, // Changed from None to Lax
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenLifetimeDays),
                IsEssential = true
            });

            _logger.LogInformation("Cookies set successfully");
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("AccessToken", new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax // Changed from None to Lax
            });

            Response.Cookies.Delete("RefreshToken", new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax // Changed from None to Lax
            });
        }

        private bool IsValidJwtFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Count(c => c == '.') != 2)
                return false;

            var handler = new JwtSecurityTokenHandler();
            return handler.CanReadToken(token);
        }

        private string? ExtractToken()
        {
            // Priority 1: Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // Priority 2: Cookie
            if (Request.Cookies.TryGetValue("AccessToken", out var token))
            {
                return token?.Trim();
            }

            return null;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out var id))
            {
                return id;
            }
            return null;
        }

        private async Task BlacklistTokenAsync(string token, int? userId, string reason)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var expiresAt = jwtToken.ValidTo;

                await _blacklistRepo.AddAsync(token, userId, expiresAt, reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to blacklist token");
                // Don't throw - blacklisting is not critical for the flow
            }
        }

        private string DetermineRedirectUrl(List<string> roles)
        {
            if (roles.Contains("Admin"))
                return "/Admin/Dashboard";
            
            if (roles.Contains("ClubManager"))
                return "/Club/Dashboard";
            
            if (roles.Contains("ClubMember"))
                return "/Club/MyClubs";
            
            return "/Index"; // Default for Student
        }

        #endregion
    }
}