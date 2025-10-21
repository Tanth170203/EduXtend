using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Criteria
{
    public class EditGroupModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditGroupModel> _logger;
        private readonly IConfiguration _configuration;

        public EditGroupModel(
            IHttpClientFactory httpClientFactory,
            ILogger<EditGroupModel> logger,
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
        public string? Description { get; set; }

        [BindProperty]
        public int MaxScore { get; set; }

        [BindProperty]
        public string TargetType { get; set; } = "Student";

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
                var response = await httpClient.GetAsync($"/api/movement-criterion-groups/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var group = JsonSerializer.Deserialize<MovementCriterionGroupDto>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (group != null)
                    {
                        Id = group.Id;
                        Name = group.Name ?? string.Empty;
                        Description = group.Description;
                        MaxScore = group.MaxScore;
                        TargetType = group.TargetType ?? "Student";
                    }
                }
                else
                {
                    ErrorMessage = "Không tìm thấy nhóm tiêu chí.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion group");
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
                ErrorMessage = "Tên nhóm tiêu chí không được để trống.";
                return Page();
            }

            if (MaxScore <= 0)
            {
                ErrorMessage = "Điểm tối đa phải lớn hơn 0.";
                return Page();
            }

            try
            {
                using var httpClient = CreateHttpClient();

                var updateDto = new
                {
                    id = Id,
                    name = Name,
                    description = Description,
                    maxScore = MaxScore,
                    targetType = TargetType
                };

                var json = JsonSerializer.Serialize(updateDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Updating criterion group: {Json}", json);

                var response = await httpClient.PutAsync($"/api/movement-criterion-groups/{Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Đã cập nhật nhóm tiêu chí thành công!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Update failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Không thể cập nhật nhóm tiêu chí: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion group");
                ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
                return Page();
            }
        }
    }
}

