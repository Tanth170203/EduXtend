using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Criteria
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public List<MovementCriterionGroupDto> CriterionGroups { get; set; } = new();
        
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

            // Copy tất cả cookie từ request FE sang handler
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
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/movement-criterion-groups");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var groups = JsonSerializer.Deserialize<List<MovementCriterionGroupDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (groups != null)
                    {
                        CriterionGroups = groups;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to load criterion groups: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion groups");
                ErrorMessage = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
            }

            return Page();
        }

        // Create Group handler
        public async Task<IActionResult> OnPostCreateGroupAsync(string Name, string? Description, int MaxScore, string TargetType)
        {
            try
            {
                var model = new CreateMovementCriterionGroupDto
                {
                    Name = Name,
                    Description = Description,
                    MaxScore = MaxScore,
                    TargetType = TargetType
                };

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync("/api/movement-criterion-groups", content);
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Thêm nhóm tiêu chí thành công!";
                    return RedirectToPage();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Create criterion group failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<dynamic>(errorContent);
                        ErrorMessage = $"Không thể thêm nhóm tiêu chí: {errorResponse}";
                    }
                    catch
                    {
                        ErrorMessage = $"Không thể thêm nhóm tiêu chí: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criterion group");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage();
        }

        // Update Group handler
        public async Task<IActionResult> OnPostUpdateGroupAsync(int Id, string Name, string? Description, int MaxScore, string TargetType)
        {
            try
            {
                var model = new UpdateMovementCriterionGroupDto
                {
                    Id = Id,
                    Name = Name,
                    Description = Description,
                    MaxScore = MaxScore,
                    TargetType = TargetType
                };

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.PutAsync($"/api/movement-criterion-groups/{Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Cập nhật nhóm tiêu chí thành công!";
                    return RedirectToPage();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Update criterion group failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<dynamic>(errorContent);
                        ErrorMessage = $"Không thể cập nhật nhóm tiêu chí: {errorResponse}";
                    }
                    catch
                    {
                        ErrorMessage = $"Không thể cập nhật nhóm tiêu chí: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion group");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage();
        }

        // Delete Group handler
        public async Task<IActionResult> OnPostDeleteGroupAsync(int Id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/movement-criterion-groups/{Id}");
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Xóa nhóm tiêu chí thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Delete criterion group failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<dynamic>(errorContent);
                        ErrorMessage = $"Không thể xóa nhóm tiêu chí: {errorResponse}";
                    }
                    catch
                    {
                        ErrorMessage = $"Không thể xóa nhóm tiêu chí: {response.StatusCode}";
                    }
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting criterion group");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage();
        }
    }
}