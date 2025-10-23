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

            // ===== VALIDATE STATUS =====
            if (string.IsNullOrWhiteSpace(Input.Status) || 
                (Input.Status != "Approved" && Input.Status != "Rejected"))
            {
                ErrorMessage = "❌ Trạng thái duyệt không hợp lệ.";
                await OnGetAsync(Input.Id);
                return Page();
            }

            // ===== VALIDATE POINTS =====
            if (Input.Status == "Approved" && Input.Points < 0)
            {
                ErrorMessage = "❌ Điểm không được âm.";
                await OnGetAsync(Input.Id);
                return Page();
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

            try
            {
                // ===== GET ReviewedById FROM JWT =====
                int reviewedById = GetCurrentUserId();

                // ===== VALIDATE ReviewedById (FRONTEND CHECK) =====
                if (reviewedById <= 0)
                {
                    ErrorMessage = "❌ Lỗi: Không thể xác định người duyệt. Vui lòng logout và login lại để cập nhật JWT token.";
                    await OnGetAsync(Input.Id);
                    return Page();
                }

                // ===== BUILD REVIEW DTO =====
                var reviewDto = new ReviewEvidenceDto
                {
                    Id = Input.Id,
                    Status = Input.Status,
                    ReviewerComment = Input.Comment,
                    Points = Input.Status == "Approved" ? Input.Points : 0,
                    ReviewedById = reviewedById
                };

                var json = JsonSerializer.Serialize(reviewDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ===== SEND REVIEW REQUEST =====
                using var httpClient = CreateHttpClient();
                _logger.LogInformation(
                    "Sending review request: EvidenceId={EvidenceId}, Status={Status}, Points={Points}, ReviewedById={ReviewedById}",
                    Input.Id, Input.Status, reviewDto.Points, reviewedById);

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

        /// <summary>
        /// Get current UserId from JWT claims or fallback to reading JWT token from cookie
        /// </summary>
        private int GetCurrentUserId()
        {
            try
            {
                // ✅ Try to get UserId from JWT claims first
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogInformation("Got UserId from JWT claim: {UserId}", userId);
                    return userId;
                }

                // ⚠️ Fallback: try to read JWT token from cookie if claims not available
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);

                        // Log all claims for debugging
                        _logger.LogInformation("JWT Claims from cookie: {Claims}",
                            string.Join(", ", jwt.Claims.Select(c => $"{c.Type}={c.Value}")));

                        // Try to get UserId from token
                        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt))
                        {
                            _logger.LogInformation("Got UserId from JWT token cookie: {UserId}", userIdFromJwt);
                            return userIdFromJwt;
                        }
                    }
                }

                _logger.LogWarning("Could not determine UserId from any source (claims or cookie)");
                return 0; // Return 0 to indicate failure
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current UserId");
                return 0; // Return 0 on error
            }
        }
    }
}