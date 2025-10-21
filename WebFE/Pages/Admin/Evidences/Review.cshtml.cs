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

        public ReviewModel(IHttpClientFactory httpClientFactory, ILogger<ReviewModel> logger, IConfiguration configuration)
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/evidences/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Evidence = JsonSerializer.Deserialize<EvidenceDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (Evidence != null)
                    {
                        Input.Id = Evidence.Id;
                        return Page();
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy minh chứng.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tải minh chứng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evidence {Id}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(Input.Id);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Status) || (Input.Status != "Approved" && Input.Status != "Rejected"))
            {
                ErrorMessage = "Trạng thái duyệt không hợp lệ.";
                await OnGetAsync(Input.Id);
                return Page();
            }

            if (Input.Status == "Approved" && Input.Points < 0)
            {
                ErrorMessage = "Điểm không được âm.";
                await OnGetAsync(Input.Id);
                return Page();
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int reviewedById = 0;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    reviewedById = currentUserId;
                }

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

                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync($"/api/evidences/{Input.Id}/review", content);

                if (response.IsSuccessStatusCode)
                {
                    var statusText = Input.Status == "Approved" ? "duyệt" : "từ chối";
                    TempData["SuccessMessage"] = $"✅ Đã {statusText} minh chứng thành công!";

                    if (Input.Status == "Approved" && Input.Points > 0)
                    {
                        TempData["SuccessMessage"] += $" Cộng {Input.Points} điểm vào hồ sơ sinh viên.";
                    }

                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Review evidence failed: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "Không tìm thấy minh chứng cần duyệt.",
                        HttpStatusCode.BadRequest => "Thông tin duyệt không hợp lệ.",
                        HttpStatusCode.Conflict => "Minh chứng đã được duyệt trước đó.",
                        _ => "Không thể duyệt minh chứng. Vui lòng thử lại."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing evidence {Id}", Input.Id);
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            await OnGetAsync(Input.Id);
            return Page();
        }

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
    }
}

