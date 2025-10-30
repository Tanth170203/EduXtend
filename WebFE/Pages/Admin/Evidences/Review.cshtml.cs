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

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public int Id { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Comment { get; set; }
            public double Points { get; set; }
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
                TempData["ErrorMessage"] = "❌ ID không hợp lệ.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
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
                        _logger.LogInformation("Loaded evidence {EvidenceId} for review", id);
                        return Page();
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "❌ Không tìm thấy minh chứng.";
                }
                else
                {
                    TempData["ErrorMessage"] = "❌ Không thể tải minh chứng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidence {Id}", id);
                TempData["ErrorMessage"] = "❌ Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Submit review (approve/reject) for evidence
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(Input.Id);
                return Page();
            }

            // Ensure we have fresh Evidence data (CriterionId, MaxScore, etc.)
            Evidence = await LoadEvidenceAsync(Input.Id);

            // ===== VALIDATE STATUS =====
            if (string.IsNullOrWhiteSpace(Input.Status) || 
                (Input.Status != "Approved" && Input.Status != "Rejected"))
            {
                ErrorMessage = "❌ Trạng thái duyệt không hợp lệ.";
                await OnGetAsync(Input.Id);
                return Page();
            }

            // ===== VALIDATE POINTS =====
            if (Input.Status == "Approved")
            {
                if (Input.Points < 0)
                {
                    ErrorMessage = "❌ Điểm không được âm.";
                    await OnGetAsync(Input.Id);
                    return Page();
                }

                if (Input.Points <= 0)
                {
                    ErrorMessage = "❌ Vui lòng nhập điểm > 0 khi duyệt Approved.";
                    await OnGetAsync(Input.Id);
                    return Page();
                }
            }

            // ===== VALIDATE POINTS WITHIN CRITERION RANGE =====
            if (Input.Status == "Approved" && Evidence != null && Evidence.CriterionMaxScore > 0)
            {
                if (Input.Points > Evidence.CriterionMaxScore)
                {
                    ErrorMessage = $"❌ Điểm không được vượt quá {Evidence.CriterionMaxScore} (tối đa cho tiêu chí \"{Evidence.CriterionTitle}\")";
                    await OnGetAsync(Input.Id);
                    return Page();
                }
            }

            // ===== WARN IF NO CRITERION TO APPLY POINTS =====
            if (Input.Status == "Approved" && Input.Points > 0 && (Evidence == null || Evidence.CriterionId == null))
            {
                ErrorMessage = "⚠️ Evidence chưa gắn với Tiêu chí nên không thể cộng điểm tự động. Hãy gắn Tiêu chí hoặc duyệt không điểm.";
                await OnGetAsync(Input.Id);
                return Page();
            }

            try
            {
                // ===== GET ReviewedById FROM JWT =====
                int reviewedById = GetCurrentUserId();

                // ===== VALIDATE ReviewedById (FRONTEND CHECK) =====
                if (reviewedById <= 0)
                {
                    ErrorMessage = "❌ Lỗi: Không thể xác định người duyệt. Vui lòng kiểm tra JWT token. " +
                        "Nếu vấn đề vẫn tiếp tục, vui lòng logout và login lại.";
                    _logger.LogError("ReviewedById validation failed: ReviewedById={ReviewedById}", reviewedById);
                    await OnGetAsync(Input.Id);
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
                    ReviewedById = reviewedById
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
                    var statusText = Input.Status == "Approved" ? "duyệt" : "từ chối";
                    SuccessMessage = $"✅ Đã {statusText} minh chứng thành công!";

                    if (Input.Status == "Approved" && Input.Points > 0)
                    {
                        SuccessMessage += $" Cộng {Input.Points} điểm vào hồ sơ sinh viên.";
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
                        HttpStatusCode.NotFound => "❌ Không tìm thấy minh chứng cần duyệt.",
                        HttpStatusCode.BadRequest => "❌ Thông tin duyệt không hợp lệ. Chi tiết: " + errorContent,
                        HttpStatusCode.Conflict => "❌ Minh chứng đã được duyệt trước đó.",
                        _ => $"❌ Không thể duyệt minh chứng. (Lỗi: {response.StatusCode})"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing evidence {EvidenceId}", Input.Id);
                ErrorMessage = $"❌ Đã xảy ra lỗi: {ex.Message}";
            }

            await OnGetAsync(Input.Id);
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