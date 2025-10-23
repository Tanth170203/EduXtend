using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using BusinessObject.DTOs.JoinRequest;

namespace WebFE.Pages.Student
{
    public class MyApplicationsModel : PageModel
    {
        private readonly ILogger<MyApplicationsModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public MyApplicationsModel(ILogger<MyApplicationsModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public List<JoinRequestDto> Applications { get; set; } = new();

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

                // Get UserId from token
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("Cannot get UserId from token");
                    return Redirect("/Auth/Login");
                }

                // Load applications
                await LoadApplicationsAsync(userId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my applications");
                return Page();
            }
        }

        private async Task LoadApplicationsAsync(int userId)
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Get all join requests for this user
                var response = await httpClient.GetAsync($"/api/joinrequest/user/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Applications = JsonSerializer.Deserialize<List<JoinRequestDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<JoinRequestDto>();
                }
                else
                {
                    _logger.LogWarning("Failed to load applications: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applications from API");
            }
        }
    }
}

