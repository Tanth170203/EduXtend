using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Semesters
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditModel> _logger;
        private readonly IConfiguration _configuration;

        public EditModel(
            IHttpClientFactory httpClientFactory,
            ILogger<EditModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public DateTime StartDate { get; set; }

        [BindProperty]
        public DateTime EndDate { get; set; }

        [BindProperty]
        public bool IsActive { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "ID không hợp lệ.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/semesters/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var semester = JsonSerializer.Deserialize<SemesterDto>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (semester != null)
                    {
                        Id = semester.Id;
                        Name = semester.Name ?? string.Empty;
                        StartDate = semester.StartDate;
                        EndDate = semester.EndDate;
                        IsActive = semester.IsActive;
                    }
                }
                else
                {
                    ErrorMessage = "Không tìm thấy học kỳ.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading semester");
                ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu.";
                return RedirectToPage("./Index");
            }

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

                var updateDto = new
                {
                    id = Id,
                    name = Name,
                    startDate = StartDate.ToString("yyyy-MM-dd"),
                    endDate = EndDate.ToString("yyyy-MM-dd"),
                    isActive = IsActive
                };

                var json = JsonSerializer.Serialize(updateDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Updating semester: {Json}", json);

                var response = await httpClient.PutAsync($"/api/semesters/{Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Đã cập nhật học kỳ thành công!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Update failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Không thể cập nhật học kỳ: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating semester");
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                return Page();
            }
        }
    }
}

