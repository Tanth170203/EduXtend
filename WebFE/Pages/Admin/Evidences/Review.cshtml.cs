using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Evidences
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReviewModel> _logger;
        private readonly IConfiguration _configuration;

        public ReviewModel(
            IHttpClientFactory httpClientFactory,
            ILogger<ReviewModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public EvidenceDto? Evidence { get; set; }

        public List<CriterionSelectItem> Criteria { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public int Id { get; set; }
            public string Status { get; set; } = "Approved"; // Default to Approved
            public string? Comment { get; set; }
            public double Points { get; set; }
            public int? CriterionId { get; set; }
        }

        public class CriterionSelectItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public int MaxScore { get; set; }
            public string? GroupName { get; set; }
        }

        /// <summary>
        /// Create HTTP Client with API configuration
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7055");

            foreach (var cookie in Request.Cookies)
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }

            return httpClient;
        }

        /// <summary>
        /// Load evidence details for review
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "❌ Invalid ID.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
                
                // Load evidence
                var response = await httpClient.GetAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Evidence = JsonSerializer.Deserialize<EvidenceDto>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (Evidence != null)
                    {
                        Input.Id = Evidence.Id;
                        Input.CriterionId = Evidence.CriterionId;
                        
                        // Load criteria list for dropdown
                        await LoadCriteriaAsync(httpClient);
                        
                        _logger.LogInformation("Loaded evidence {EvidenceId} for review", id);
                        return Page();
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "❌ Evidence not found.";
                }
                else
                {
                    TempData["ErrorMessage"] = "❌ Unable to load evidence. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidence {Id}", id);
                TempData["ErrorMessage"] = "❌ An error occurred. Please try again.";
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Load criteria list for dropdown
        /// </summary>
        private async Task LoadCriteriaAsync(HttpClient httpClient)
        {
            try
            {
                // Get only active criteria for Student target type
                var response = await httpClient.GetAsync("/api/movement-criteria/by-target-type/Student");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Criteria API response: {Content}", content);
                    
                    // API returns array directly, not wrapped in { data: [...] }
                    var criteriaList = JsonSerializer.Deserialize<List<JsonElement>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (criteriaList != null)
                    {
                        foreach (var item in criteriaList)
                        {
                            // Only add active criteria
                            var isActive = item.TryGetProperty("isActive", out var activeVal) && activeVal.GetBoolean();
                            if (!isActive) continue;
                            
                            Criteria.Add(new CriterionSelectItem
                            {
                                Id = item.GetProperty("id").GetInt32(),
                                Title = item.GetProperty("title").GetString() ?? "",
                                MaxScore = item.TryGetProperty("maxScore", out var maxScore) ? maxScore.GetInt32() : 0,
                                GroupName = item.TryGetProperty("groupName", out var groupName) ? groupName.GetString() : null
                            });
                        }
                        
                        _logger.LogInformation("Loaded {Count} criteria for dropdown", Criteria.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load criteria: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load criteria list");
            }
        }

        /// <summary>
        /// Reload page data when validation fails
        /// </summary>
        private async Task ReloadPageDataAsync(int id)
        {
            Evidence = await LoadEvidenceAsync(id);
            using var httpClient = CreateHttpClient();
            await LoadCriteriaAsync(httpClient);
        }

        /// <summary>
        /// Submit review (approve/reject) for evidence
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadPageDataAsync(Input.Id);
                return Page();
            }

            // Ensure we have fresh Evidence data (CriterionId, MaxScore, etc.)
            Evidence = await LoadEvidenceAsync(Input.Id);

            // ===== VALIDATE STATUS =====
            if (string.IsNullOrWhiteSpace(Input.Status) ||
                (Input.Status != "Approved" && Input.Status != "Rejected"))
            {
                ErrorMessage = "❌ Invalid review status.";
                await ReloadPageDataAsync(Input.Id);
                return Page();
            }

            // ===== VALIDATE POINTS =====
            if (Input.Status == "Approved")
            {
                if (Input.Points < 0)
                {
                    ErrorMessage = "❌ Points cannot be negative.";
                    await ReloadPageDataAsync(Input.Id);
                    return Page();
                }

                if (Input.Points <= 0)
                {
                    ErrorMessage = "❌ Please enter points > 0 when approving.";
                    await ReloadPageDataAsync(Input.Id);
                    return Page();
                }
            }

            // ===== VALIDATE CRITERION WHEN APPROVING =====
            if (Input.Status == "Approved" && Input.Points > 0 && !Input.CriterionId.HasValue)
            {
                ErrorMessage = "⚠️ Please select a criterion to award points.";
                await ReloadPageDataAsync(Input.Id);
                return Page();
            }

            try
            {
                // ===== GET ReviewedById FROM JWT =====
                int reviewedById = GetCurrentUserId();

                // ===== VALIDATE ReviewedById (FRONTEND CHECK) =====
                if (reviewedById <= 0)
                {
                    ErrorMessage = "❌ Error: Unable to determine reviewer. Please check the JWT token. If the issue persists, log out and log back in.";
                    _logger.LogError("ReviewedById validation failed: ReviewedById={ReviewedById}", reviewedById);
                    await ReloadPageDataAsync(Input.Id);
                    return Page();
                }

                _logger.LogInformation("✅ ReviewedById validated successfully: {ReviewedById}", reviewedById);

                // ===== BUILD REVIEW DTO =====
                var reviewDto = new ReviewEvidenceDto
                {
                    Id = Input.Id,
                    Status = Input.Status,
                    ReviewerComment = Input.Comment,
                    Points = Input.Status == "Approved" ? Input.Points : 0,
                    ReviewedById = reviewedById,
                    CriterionId = Input.CriterionId
                };

                _logger.LogInformation("📤 Built ReviewEvidenceDto: Id={Id}, Status={Status}, Points={Points}, ReviewedById={ReviewedById}",
                    reviewDto.Id, reviewDto.Status, reviewDto.Points, reviewDto.ReviewedById);

                var json = JsonSerializer.Serialize(reviewDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ===== SEND REVIEW REQUEST =====
                using var httpClient = CreateHttpClient();
                _logger.LogInformation(
                    "📤 Sending review request to API: POST /api/evidences/{EvidenceId}/review with ReviewedById={ReviewedById}",
                    Input.Id, reviewedById);

                var response = await httpClient.PostAsync($"/api/evidences/{Input.Id}/review", content);

                if (response.IsSuccessStatusCode)
                {
                    var statusText = Input.Status == "Approved" ? "approved" : "rejected";
                    SuccessMessage = $"✅ Evidence {statusText} successfully!";

                    if (Input.Status == "Approved" && Input.Points > 0)
                    {
                        SuccessMessage += $" Awarded {Input.Points} points to the student's record.";
                    }

                    _logger.LogInformation("Evidence {EvidenceId} reviewed successfully as {Status}", Input.Id, Input.Status);
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Review evidence failed: StatusCode={StatusCode}, Error={Error}",
                        response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "❌ Evidence to review not found.",
                        HttpStatusCode.BadRequest => "❌ Invalid review data. Details: " + errorContent,
                        HttpStatusCode.Conflict => "❌ Evidence has already been reviewed.",
                        _ => $"❌ Unable to review evidence. (Error: {response.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing evidence {EvidenceId}", Input.Id);
                ErrorMessage = $"❌ An error occurred: {ex.Message}";
            }

            await ReloadPageDataAsync(Input.Id);
            return Page();
        }

        private async Task<EvidenceDto?> LoadEvidenceAsync(int id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/evidences/{id}");
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var evidence = JsonSerializer.Deserialize<EvidenceDto>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return evidence;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidence {Id} in LoadEvidenceAsync", id);
                return null;
            }
        }

        /// <summary>
        /// Get current UserId from JWT claims or fallback to reading JWT token from cookie
        /// </summary>
        private int GetCurrentUserId()
        {
            try
            {
                // ===== METHOD 1: Get UserId from HttpContext.User claims =====
                // This is the most reliable method - claims should be set by middleware
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId) && userId > 0)
                {
                    _logger.LogInformation("✅ Got UserId from HttpContext.User claims: {UserId}", userId);
                    return userId;
                }

                _logger.LogWarning("⚠️ UserId claim not found in HttpContext.User. Trying fallback method...");

                // ===== METHOD 2: Fallback - Parse JWT token from AccessToken cookie =====
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    _logger.LogInformation("Found AccessToken cookie, attempting to parse JWT...");

                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

                        if (!handler.CanReadToken(token))
                        {
                            _logger.LogWarning("⚠️ AccessToken cookie is not a valid JWT token");
                            return 0;
                        }

                        var jwt = handler.ReadJwtToken(token);

                        // Log all claims for debugging
                        var claimsList = jwt.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                        _logger.LogInformation("JWT Claims from cookie: {Claims}", string.Join("; ", claimsList));

                        // Try to get UserId from token
                        var userIdFromToken = jwt.Claims
                            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
                            .Value;

                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt) && userIdFromJwt > 0)
                        {
                            _logger.LogInformation("✅ Got UserId from JWT token cookie: {UserId}", userIdFromJwt);
                            return userIdFromJwt;
                        }

                        _logger.LogWarning("⚠️ NameIdentifier claim not found in JWT token from cookie");
                    }
                    catch (Exception jwtEx)
                    {
                        _logger.LogError(jwtEx, "❌ Error parsing JWT token from cookie");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ No AccessToken cookie found");
                }

                _logger.LogError("❌ Could not determine UserId from any source");
                return 0; // Return 0 to indicate failure - this will trigger API validation error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error in GetCurrentUserId");
                return 0;
            }
        }
    }
}
