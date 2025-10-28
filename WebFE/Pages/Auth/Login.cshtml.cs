using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace WebFE.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IHttpClientFactory httpClientFactory, ILogger<LoginModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // If already logged in, redirect based on role
            var token = Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);
                    var roles = jwt.Claims
                        .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                        .Select(c => c.Value)
                        .ToList();

                    if (roles.Contains("Admin"))
                    {
                        return Redirect("/Admin/Dashboard");
                    }
                    // All other roles (ClubManager, ClubMember, Student) go to Home
                    return Redirect("/Index");
                }
                catch
                {
                    // Invalid token, clear it and continue to login page
                    ClearAuthCookies();
                }
            }

            return Page();
        }

        /// <summary>
        /// Handle Google login callback - called from JavaScript after getting credential
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostGoogleLoginAsync([FromBody] GoogleLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Processing Google login request");

                var client = _httpClientFactory.CreateClient();
                var apiUrl = "https://localhost:5001/api/auth/google";

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Google login failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return new JsonResult(new { message = "Login failed. Please use @fpt.edu.vn email." }) 
                    { 
                        StatusCode = 400 
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogError("API returned empty response");
                    return new JsonResult(new { message = "Empty response from server" }) 
                    { 
                        StatusCode = 500 
                    };
                }

                GoogleLoginResponse? loginResult;
                try
                {
                    loginResult = JsonSerializer.Deserialize<GoogleLoginResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse API response: {Content}", responseContent);
                    return new JsonResult(new { message = "Invalid response format from server" }) 
                    { 
                        StatusCode = 500 
                    };
                }

                if (loginResult == null || string.IsNullOrEmpty(loginResult.AccessToken))
                {
                    _logger.LogError("Login API returned invalid response");
                    return new JsonResult(new { message = "Invalid response from server" }) 
                    { 
                        StatusCode = 500 
                    };
                }

                SetCookie("AccessToken", loginResult.AccessToken, 30);
                SetCookie("RefreshToken", loginResult.RefreshToken, 7 * 24 * 60);

                return new JsonResult(new { 
                    message = "Login successful", 
                    redirectUrl = loginResult?.RedirectUrl ?? "/Index"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return new JsonResult(new { message = "An error occurred during login" }) 
                { 
                    StatusCode = 500 
                };
            }
        }

        public async Task<IActionResult> OnGetLogoutAsync()
        {
            try
            {
                // Call API logout endpoint
                var token = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    var client = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/api/auth/logout");
                    request.Headers.Add("Cookie", $"AccessToken={token}");
                    
                    await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling logout API");
            }

            ClearAuthCookies();
            SuccessMessage = "Successfully logged out";
            return RedirectToPage("/Index");
        }

        private void SetCookie(string name, string value, int lifetimeMinutes)
        {
            Response.Cookies.Append(name, value, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(lifetimeMinutes),
                IsEssential = true
            });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("AccessToken", new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.None
            });

            Response.Cookies.Delete("RefreshToken", new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }

    public class GoogleLoginResponse
    {
        public string Message { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
