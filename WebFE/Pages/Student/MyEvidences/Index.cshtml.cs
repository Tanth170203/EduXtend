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
        public List<EvidenceDto> FilteredEvidences { get; set; } = new();
        public List<MovementCriterionDto> Criteria { get; set; } = new();
        public int StudentId { get; set; }

        // ✅ Filter parameters
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? Criterion { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

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
                
                if (StudentId == 0)
                {
                    _logger.LogWarning("❌ StudentId is 0! User needs to re-login with new token.");
                    ErrorMessage = "Unable to identify student. Please logout and login again.";
                    return Page();
                }
                
                _logger.LogInformation("Loading evidences for StudentId: {StudentId}", StudentId);
                
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
                        _logger.LogInformation("✅ Loaded {Count} evidences for StudentId: {StudentId}", Evidences.Count, StudentId);
                    }
                }
                else
                {
                    var errorContent = await evidenceResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to load evidences: {StatusCode} - {Error}", evidenceResponse.StatusCode, errorContent);
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
                        _logger.LogInformation("✅ Loaded {Count} active criteria", Criteria.Count);
                    }
                }

                // ✅ Apply filters
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidences");
                ErrorMessage = "Error loading data.";
            }

            return Page();
        }

        /// <summary>
        /// Apply client-side filters to evidence list
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = Evidences.AsEnumerable();

            // ✅ Filter by search term (title, description, criterion)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filtered = filtered.Where(e =>
                    (e.Title?.ToLower().Contains(searchLower) ?? false) ||
                    (e.Description?.ToLower().Contains(searchLower) ?? false) ||
                    (e.CriterionTitle?.ToLower().Contains(searchLower) ?? false)
                );
            }

            // ✅ Filter by status
            if (!string.IsNullOrWhiteSpace(Status))
            {
                filtered = filtered.Where(e => e.Status?.Equals(Status, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            // ✅ Filter by criterion (partial match)
            if (!string.IsNullOrWhiteSpace(Criterion))
            {
                var criterionLower = Criterion.ToLower();
                filtered = filtered.Where(e => e.CriterionTitle?.ToLower().Contains(criterionLower) ?? false);
            }

            // ✅ Apply sorting
            filtered = Sort switch
            {
                "date_desc" => filtered.OrderByDescending(e => e.SubmittedAt),
                "date_asc" => filtered.OrderBy(e => e.SubmittedAt),
                "point_desc" => filtered.OrderByDescending(e => e.Points),
                "point_asc" => filtered.OrderBy(e => e.Points),
                _ => filtered.OrderByDescending(e => e.SubmittedAt) // Default: newest first
            };

            FilteredEvidences = filtered.ToList();
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
                // ✅ Try to get StudentId from JWT claims first (for Student users)
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (studentIdClaim != null && int.TryParse(studentIdClaim, out var studentId))
                {
                    _logger.LogInformation("Got StudentId from claim: {StudentId}", studentId);
                    return studentId;
                }

                // Fallback: try to get from JWT token in cookie
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        
                        // Try to get StudentId from token
                        var studentIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == "StudentId")?.Value;
                        if (studentIdFromToken != null && int.TryParse(studentIdFromToken, out var studentIdFromJwt))
                        {
                            _logger.LogInformation("Got StudentId from JWT token: {StudentId}", studentIdFromJwt);
                            return studentIdFromJwt;
                        }

                        // Fallback to UserId if StudentId not found (old token)
                        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt))
                        {
                            _logger.LogWarning("Only found UserId {UserId}, StudentId not in token. User may need to re-login.", userIdFromJwt);
                            return userIdFromJwt; // This might cause issues if UserId != StudentId
                        }
                    }
                }

                _logger.LogWarning("Could not determine StudentId from any source, defaulting to 0");
                return 0; // Return 0 to indicate failure (will cause proper error messages)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting StudentId");
                return 0; // Return 0 on error
            }
        }
    }
}


