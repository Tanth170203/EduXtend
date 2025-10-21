using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Semesters
{
    public class AddModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AddModel> _logger;
        private readonly IConfiguration _configuration;

        public AddModel(
            IHttpClientFactory httpClientFactory,
            ILogger<AddModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty]
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(4);

        [BindProperty]
        public bool IsActive { get; set; } = false;

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

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Thông tin không hợp lệ.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Tên học kỳ không được để trống.";
                return Page();
            }

            if (EndDate <= StartDate)
            {
                ErrorMessage = "Ngày kết thúc phải sau ngày bắt đầu.";
                return Page();
            }

            try
            {
                using var httpClient = CreateHttpClient();

                var createDto = new
                {
                    name = Name,
                    startDate = StartDate.ToString("yyyy-MM-dd"),
                    endDate = EndDate.ToString("yyyy-MM-dd"),
                    isActive = IsActive
                };

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating semester: {Json}", json);

                var response = await httpClient.PostAsync("/api/semesters", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Đã tạo học kỳ thành công!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Create failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Không thể tạo học kỳ: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating semester");
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                return Page();
            }
        }
    }
}

