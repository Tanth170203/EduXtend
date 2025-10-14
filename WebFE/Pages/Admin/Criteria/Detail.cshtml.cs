using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Criteria
{
    public class DetailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(IHttpClientFactory httpClientFactory, ILogger<DetailModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public MovementCriterionGroupDetailDto? GroupDetail { get; set; }
        
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
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<IActionResult> OnGetAsync(int groupId)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/movement-criterion-groups/{groupId}/detail");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var detail = JsonSerializer.Deserialize<MovementCriterionGroupDetailDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    GroupDetail = detail;
                }
                else
                {
                    ErrorMessage = "Không thể tải dữ liệu nhóm tiêu chí.";
                    return RedirectToPage("/Admin/Criteria/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion group detail");
                ErrorMessage = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
                return RedirectToPage("/Admin/Criteria/Index");
            }

            return Page();
        }

        // Criterion handlers
        public async Task<IActionResult> OnPostCreateCriterionAsync(int GroupId, string Title, string? Description, 
            int MaxScore, string TargetType, string? DataSource, bool IsActive = true)
        {
            try
            {
                var model = new CreateMovementCriterionDto
                {
                    GroupId = GroupId,
                    Title = Title,
                    Description = Description,
                    MaxScore = MaxScore,
                    TargetType = TargetType,
                    DataSource = DataSource,
                    IsActive = IsActive
                };

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync("/api/movement-criteria", content);
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Thêm tiêu chí thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Create criterion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Không thể thêm tiêu chí.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criterion");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage(new { groupId = GroupId });
        }

        public async Task<IActionResult> OnPostUpdateCriterionAsync(int GroupId, int Id, string Title, string? Description, 
            int MaxScore, string TargetType, string? DataSource, bool IsActive = true)
        {
            try
            {
                var model = new UpdateMovementCriterionDto
                {
                    Id = Id,
                    GroupId = GroupId,
                    Title = Title,
                    Description = Description,
                    MaxScore = MaxScore,
                    TargetType = TargetType,
                    DataSource = DataSource,
                    IsActive = IsActive
                };

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.PutAsync($"/api/movement-criteria/{Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Cập nhật tiêu chí thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Update criterion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Không thể cập nhật tiêu chí.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage(new { groupId = GroupId });
        }

        public async Task<IActionResult> OnPostDeleteCriterionAsync(int GroupId, int Id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/movement-criteria/{Id}");
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Xóa tiêu chí thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Delete criterion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorObj.TryGetProperty("message", out var msgProp))
                        {
                            ErrorMessage = msgProp.GetString();
                        }
                        else
                        {
                            ErrorMessage = "Không thể xóa tiêu chí.";
                        }
                    }
                    catch
                    {
                        ErrorMessage = "Không thể xóa tiêu chí.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting criterion");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage(new { groupId = GroupId });
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int GroupId, int Id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.PatchAsync($"/api/movement-criteria/{Id}/toggle-active", null);
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Cập nhật trạng thái thành công!";
                }
                else
                {
                    ErrorMessage = "Không thể cập nhật trạng thái.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling criterion active status");
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return RedirectToPage(new { groupId = GroupId });
        }
    }
}

