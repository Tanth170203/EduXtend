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
                // ✅ Get STUDENT ID (not User ID) from JWT claims
                StudentId = GetCurrentStudentId();
                
                if (StudentId <= 0)
                {
                    _logger.LogWarning("❌ StudentId is 0 or invalid! User needs to re-login.");
                    ErrorMessage = "Unable to identify student. Please logout and login again.";
                    return Page();
                }

                _logger.LogInformation("Loading movement summary for StudentId: {StudentId}", StudentId);
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/movement-records/student/{StudentId}/summary");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Summary = JsonSerializer.Deserialize<StudentMovementSummaryDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _logger.LogInformation("✅ Loaded movement summary for StudentId: {StudentId}", StudentId);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // No records yet - this is okay
                    Summary = null;
                    _logger.LogInformation("No movement records found for StudentId: {StudentId}", StudentId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to load movement summary: {StatusCode} - {Error}", response.StatusCode, errorContent);
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

        /// <summary>
        /// Get current STUDENT ID (not User ID) from JWT claims with fallback
        /// </summary>
        private int GetCurrentStudentId()
        {
            try
            {
                // ✅ FIRST: Try to get StudentId from JWT claims
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (studentIdClaim != null && int.TryParse(studentIdClaim, out var studentId) && studentId > 0)
                {
                    _logger.LogInformation("Got StudentId from claim: {StudentId}", studentId);
                    return studentId;
                }

                // ✅ FALLBACK: Try to get from JWT token in cookie
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        
                        // Try StudentId first
                        var studentIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == "StudentId")?.Value;
                        if (studentIdFromToken != null && int.TryParse(studentIdFromToken, out var studentIdFromJwt) && studentIdFromJwt > 0)
                        {
                            _logger.LogInformation("Got StudentId from JWT token: {StudentId}", studentIdFromJwt);
                            return studentIdFromJwt;
                        }

                        // Fallback to UserId if StudentId not in token
                        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt) && userIdFromJwt > 0)
                        {
                            _logger.LogWarning("Only found UserId {UserId}, StudentId not in token. User may need to re-login.", userIdFromJwt);
                            return userIdFromJwt;
                        }
                    }
                }

                _logger.LogWarning("❌ Could not determine StudentId from claims or token!");
                return 0;  // ✅ Return 0, NOT 1 - so we can detect the error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting StudentId");
                return 0;  // ✅ Return 0, NOT 1
            }
        }
    }
}


