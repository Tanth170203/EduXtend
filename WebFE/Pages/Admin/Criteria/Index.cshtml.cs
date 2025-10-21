using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
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
                    ErrorMessage = "Unable to load data. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion groups");
                ErrorMessage = "Unable to load data. Please try again later.";
            }

            return Page();
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
                    SuccessMessage = "Criteria group deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Delete criterion group failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Try to parse error message from response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<dynamic>(errorContent);
                        ErrorMessage = $"Unable to delete criteria group: {errorResponse}";
                    }
                    catch
                    {
                        ErrorMessage = $"Unable to delete criteria group: {response.StatusCode}";
                    }
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting criterion group");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }
    }
}