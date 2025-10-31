using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Evidences
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(
            IHttpClientFactory httpClientFactory,
            ILogger<IndexModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        // Data Properties
        public List<EvidenceDto> Evidences { get; set; } = new();
        public List<EvidenceDto> AllEvidences { get; set; } = new(); // All evidences for statistics
        public string? CurrentFilter { get; set; }

        // Statistics Properties (based on AllEvidences, not filtered Evidences)
        public int TotalEvidences => AllEvidences.Count;
        public int PendingCount => AllEvidences.Count(e => e.Status == "Pending");
        public int ApprovedCount => AllEvidences.Count(e => e.Status == "Approved");
        public int RejectedCount => AllEvidences.Count(e => e.Status == "Rejected");
        public double ApprovalRate => TotalEvidences > 0
            ? Math.Round((double)ApprovedCount / TotalEvidences * 100, 1)
            : 0;

        // Messages
        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Create HTTP Client with cookies for authentication
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Copy cookies from request
            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(
                    new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001"),
                    new Cookie(cookie.Key, cookie.Value)
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );

            return client;
        }

        /// <summary>
        /// Get current UserId from JWT claims
        /// </summary>
        private int GetCurrentUserId()
        {
            try
            {
                // Try to get UserId from JWT claims first
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var userId) && userId > 0)
                {
                    _logger.LogInformation("✅ Got UserId from JWT claim: {UserId}", userId);
                    return userId;
                }

                _logger.LogWarning("⚠️ UserId claim not found in HttpContext.User. Trying fallback method...");

                // Fallback: try to read JWT token from cookie if claims not available
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            var jwt = handler.ReadJwtToken(token);
                            var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                            if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt) && userIdFromJwt > 0)
                            {
                                _logger.LogInformation("✅ Got UserId from JWT token cookie: {UserId}", userIdFromJwt);
                                return userIdFromJwt;
                            }
                        }
                    }
                    catch (Exception jwtEx)
                    {
                        _logger.LogError(jwtEx, "Error parsing JWT token from cookie");
                    }
                }

                _logger.LogError("❌ Could not determine UserId");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current UserId");
                return 0;
            }
        }

        /// <summary>
        /// Load evidences based on filter
        /// </summary>
        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            CurrentFilter = filter ?? "all";

            try
            {
                using var httpClient = CreateHttpClient();
                // Load all evidences first for statistics
                await LoadAllEvidencesAsync(httpClient);
                // Then load filtered evidences for display
                await LoadEvidencesAsync(httpClient, CurrentFilter);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while loading evidences");
                ErrorMessage = "Unable to connect to the server. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading evidences");
                ErrorMessage = "An error occurred while loading data. Please try again.";
            }

            return Page();
        }

        /// <summary>
        /// Load all evidences from API for statistics
        /// </summary>
        private async Task LoadAllEvidencesAsync(HttpClient httpClient)
        {
            try
            {
                _logger.LogInformation("Loading all evidences for statistics");

                var response = await httpClient.GetAsync("/api/evidences");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var evidences = JsonSerializer.Deserialize<List<EvidenceDto>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (evidences != null)
                    {
                        AllEvidences = evidences.ToList();
                        _logger.LogInformation("Loaded {Count} total evidences for statistics",
                            AllEvidences.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load all evidences for statistics: {StatusCode}",
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all evidences for statistics");
                // Don't throw - statistics can be empty if this fails
            }
        }

        /// <summary>
        /// Load evidences from API based on filter
        /// </summary>
        private async Task LoadEvidencesAsync(HttpClient httpClient, string filter)
        {
            try
            {
                string endpoint = filter switch
                {
                    "pending" => "/api/evidences/pending",
                    "approved" => "/api/evidences/status/Approved",
                    "rejected" => "/api/evidences/status/Rejected",
                    _ => "/api/evidences"
                };

                _logger.LogInformation("Loading evidences from: {Endpoint}", endpoint);

                var response = await httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var evidences = JsonSerializer.Deserialize<List<EvidenceDto>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (evidences != null)
                    {
                        // Sort by submitted date (newest first)
                        Evidences = evidences.OrderByDescending(e => e.SubmittedAt).ToList();
                        _logger.LogInformation("Loaded {Count} evidences with filter '{Filter}'",
                            Evidences.Count, filter);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to load evidences: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    ErrorMessage = "Unable to load the evidence list. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidences");
                throw;
            }
        }


        /// <summary>
        /// Delete evidence
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "Invalid ID.";
                return RedirectToPage();
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Evidence deleted successfully!";
                    _logger.LogInformation("Deleted evidence {EvidenceId}", id);
                }
                else
                {
                    _logger.LogWarning("Delete failed: {StatusCode}", response.StatusCode);
                    ErrorMessage = response.StatusCode == HttpStatusCode.NotFound
                        ? "Evidence to delete not found."
                        : "Unable to delete evidence. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting evidence {EvidenceId}", id);
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage(new { filter = CurrentFilter });
        }

        /// <summary>
        /// Bulk approve evidences (for future implementation)
        /// </summary>
        public async Task<IActionResult> OnPostBulkApproveAsync(int[] evidenceIds, double defaultPoints)
        {
            if (evidenceIds == null || evidenceIds.Length == 0)
            {
                ErrorMessage = "No evidences selected.";
                return RedirectToPage();
            }

            try
            {
                // ===== GET ReviewedById FROM CURRENT USER =====
                int reviewedById = GetCurrentUserId();
                if (reviewedById <= 0)
                {
                    ErrorMessage = "❌ Error: Unable to determine reviewer. Please log out and log in again.";
                    _logger.LogError("Bulk approve failed: ReviewedById is invalid ({ReviewedById})", reviewedById);
                    return RedirectToPage(new { filter = "pending" });
                }

                int successCount = 0;
                int failCount = 0;

                using var httpClient = CreateHttpClient();

                foreach (var id in evidenceIds)
                {
                    var reviewDto = new ReviewEvidenceDto
                    {
                        Id = id,
                        Status = "Approved",
                        Points = defaultPoints,
                        ReviewedById = reviewedById, // ✅ NOW USING CURRENT USER INSTEAD OF HARDCODED 1
                        ReviewerComment = "Bulk approved"
                    };

                    var json = JsonSerializer.Serialize(reviewDto);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"/api/evidences/{id}/review", content);

                    if (response.IsSuccessStatusCode)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    SuccessMessage = $"✅ Successfully approved {successCount} evidences!";
                }

                if (failCount > 0)
                {
                    ErrorMessage = $"⚠️ {failCount} evidences failed to approve.";
                }

                _logger.LogInformation("Bulk approved evidences: Success={Success}, Failed={Failed}, ReviewedById={ReviewedById}",
                    successCount, failCount, reviewedById);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approve");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage(new { filter = "pending" });
        }

        /// <summary>
        /// Get evidence statistics
        /// </summary>
        public async Task<IActionResult> OnGetStatisticsAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/evidences/stats/pending-count");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, int>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return new JsonResult(result);
                }

                return new JsonResult(new { count = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return new JsonResult(new { count = 0 });
            }
        }
    }
}
