using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Text.Json;
using BusinessObject.DTOs.Club;

namespace WebFE.Pages.ClubManager
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // Dashboard Statistics
        public int ClubId { get; set; }
        public string ClubName { get; set; } = "My Club";
        public int TotalMembers { get; set; }
        public int UpcomingActivities { get; set; }
        public int PendingRequests { get; set; }
        public string? ClubDescription { get; set; }
        public string? ClubLogoUrl { get; set; }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get user info from JWT token
                var token = Request.Cookies["AccessToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return Redirect("/Auth/Login");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                
                // Get user roles
                var roles = jwt.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                    .Select(c => c.Value)
                    .ToList();

                // Check if user is ClubManager
                if (!roles.Contains("ClubManager", StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User without ClubManager role attempted to access ClubManager dashboard");
                    return Redirect("/Error?code=403");
                }

                // Get UserId from token
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("Cannot get UserId from token");
                    return Redirect("/Auth/Login");
                }

                // Load club data for this manager
                await LoadClubDataAsync(userId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club manager dashboard");
                // Set default values on error
                ClubName = "My Club";
                TotalMembers = 0;
                UpcomingActivities = 0;
                PendingRequests = 0;
                return Page();
            }
        }

        private async Task LoadClubDataAsync(int userId)
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Call API to get club managed by this user
                var response = await httpClient.GetAsync("/api/club/my-managed-club");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var club = JsonSerializer.Deserialize<ClubDetailDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (club != null)
                    {
                        ClubId = club.Id;
                        ClubName = club.Name;
                        ClubDescription = club.Description;
                        ClubLogoUrl = club.LogoUrl;
                        TotalMembers = club.MemberCount;
                        
                        // Get recruitment status for pending requests count
                        var statusResponse = await httpClient.GetAsync($"/api/club/{ClubId}/recruitment-status");
                        if (statusResponse.IsSuccessStatusCode)
                        {
                            var statusContent = await statusResponse.Content.ReadAsStringAsync();
                            var status = JsonSerializer.Deserialize<BusinessObject.DTOs.Club.RecruitmentStatusDto>(statusContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            if (status != null)
                            {
                                PendingRequests = status.PendingRequestCount;
                            }
                        }
                        
                        // TODO: Load activities count
                        UpcomingActivities = 0;
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load club data: {StatusCode}", response.StatusCode);
                    // Use placeholder data if API fails
                    ClubName = "My Club";
                    TotalMembers = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club data from API");
                // Use placeholder data on error
                ClubName = "My Club";
                TotalMembers = 0;
            }
        }
    }
}

