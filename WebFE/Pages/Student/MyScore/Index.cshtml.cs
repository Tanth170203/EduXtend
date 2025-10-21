using BusinessObject.DTOs.MovementRecord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.Student.MyScore
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public StudentMovementSummaryDto? Summary { get; set; }
        public int StudentId { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

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
                // Get current user ID from authentication
                StudentId = GetCurrentUserId();
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/movement-records/student/{StudentId}/summary");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Summary = JsonSerializer.Deserialize<StudentMovementSummaryDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // No records yet - this is okay
                    Summary = null;
                }
                else
                {
                    _logger.LogError("Failed to load movement summary: {StatusCode}", response.StatusCode);
                    ErrorMessage = "Unable to load movement score.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movement summary");
                ErrorMessage = "Error loading data.";
            }

            return Page();
        }

        private int GetCurrentUserId()
        {
            try
            {
                // Try to get from JWT claims first
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }

                // Fallback: try to get from JWT token in cookie
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt))
                        {
                            return userIdFromJwt;
                        }
                    }
                }

                _logger.LogWarning("Could not determine current user ID, defaulting to 1");
                return 1; // Fallback to 1 if cannot determine
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID, defaulting to 1");
                return 1; // Fallback to 1 on error
            }
        }
    }
}


