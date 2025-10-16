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
                    ErrorMessage = "Unable to load criteria group data.";
                    return RedirectToPage("/Admin/Criteria/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion group detail");
                ErrorMessage = "Unable to load data. Please try again later.";
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
                    SuccessMessage = "Criterion added successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Create criterion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Unable to add criterion.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criterion");
                ErrorMessage = "An error occurred. Please try again.";
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
                    SuccessMessage = "Criterion updated successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Update criterion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Unable to update criterion.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion");
                ErrorMessage = "An error occurred. Please try again.";
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
                    SuccessMessage = "Criterion deleted successfully!";
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
                            ErrorMessage = "Unable to delete criterion.";
                        }
                    }
                    catch
                    {
                        ErrorMessage = "Unable to delete criterion.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting criterion");
                ErrorMessage = "An error occurred. Please try again.";
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
                    SuccessMessage = "Status updated successfully!";
                }
                else
                {
                    ErrorMessage = "Unable to update status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling criterion active status");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage(new { groupId = GroupId });
        }
    }
}

