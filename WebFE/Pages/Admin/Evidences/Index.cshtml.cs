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
        public string? CurrentFilter { get; set; }

        // Statistics Properties
        public int TotalEvidences => Evidences.Count;
        public int PendingCount => Evidences.Count(e => e.Status == "Pending");
        public int ApprovedCount => Evidences.Count(e => e.Status == "Approved");
        public int RejectedCount => Evidences.Count(e => e.Status == "Rejected");
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
        /// Load evidences based on filter
        /// </summary>
        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            CurrentFilter = filter ?? "all";

            try
            {
                using var httpClient = CreateHttpClient();
                await LoadEvidencesAsync(httpClient, CurrentFilter);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while loading evidences");
                ErrorMessage = "Không thể kết nối đến server. Vui lòng thử lại sau.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading evidences");
                ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại.";
            }

            return Page();
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
                    ErrorMessage = "Không thể tải danh sách minh chứng. Vui lòng thử lại.";
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
                ErrorMessage = "ID không hợp lệ.";
                return RedirectToPage();
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Đã xóa minh chứng thành công!";
                    _logger.LogInformation("Deleted evidence {EvidenceId}", id);
                }
                else
                {
                    _logger.LogWarning("Delete failed: {StatusCode}", response.StatusCode);
                    ErrorMessage = response.StatusCode == HttpStatusCode.NotFound
                        ? "Không tìm thấy minh chứng cần xóa."
                        : "Không thể xóa minh chứng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting evidence {EvidenceId}", id);
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
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
                ErrorMessage = "Chưa chọn minh chứng nào.";
                return RedirectToPage();
            }

            try
            {
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
                        ReviewedById = 1, // TODO: Get from current user
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
                    SuccessMessage = $"✅ Đã duyệt thành công {successCount} minh chứng!";
                }

                if (failCount > 0)
                {
                    ErrorMessage = $"⚠️ Có {failCount} minh chứng không thể duyệt.";
                }

                _logger.LogInformation("Bulk approved evidences: Success={Success}, Failed={Failed}",
                    successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approve");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
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