using BusinessObject.DTOs.Evidence;
using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.Student.MyEvidences
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

        public List<EvidenceDto> Evidences { get; set; } = new();
        public List<MovementCriterionDto> Criteria { get; set; } = new();
        public int StudentId { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

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

                // Load student's evidences
                var evidenceResponse = await httpClient.GetAsync($"/api/evidences/student/{StudentId}");
                if (evidenceResponse.IsSuccessStatusCode)
                {
                    var content = await evidenceResponse.Content.ReadAsStringAsync();
                    var evidences = JsonSerializer.Deserialize<List<EvidenceDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (evidences != null)
                    {
                        Evidences = evidences;
                    }
                }

                // Load active criteria for Student
                var criteriaResponse = await httpClient.GetAsync("/api/movement-criteria/by-target-type/Student");
                if (criteriaResponse.IsSuccessStatusCode)
                {
                    var content = await criteriaResponse.Content.ReadAsStringAsync();
                    var criteria = JsonSerializer.Deserialize<List<MovementCriterionDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (criteria != null)
                    {
                        Criteria = criteria.Where(c => c.IsActive).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidences");
                ErrorMessage = "Error loading data.";
            }

            return Page();
        }


        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Evidence deleted successfully!";
                }
                else
                {
                    ErrorMessage = "Unable to delete evidence.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting evidence");
                ErrorMessage = "An error occurred.";
            }

            return RedirectToPage();
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


