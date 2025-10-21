using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace WebFE.Pages.Admin.Semesters
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

        public List<SemesterDto> Semesters { get; set; } = new();
        
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
                // Debug: Check cookies
                var cookies = Request.Headers["Cookie"].ToString();
                _logger.LogInformation("🔍 OnGetAsync - Available cookies: {Cookies}", cookies);
                
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/semesters");
                _logger.LogInformation("🔍 OnGetAsync - Response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var semesters = JsonSerializer.Deserialize<List<SemesterDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (semesters != null)
                    {
                        Semesters = semesters;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to load semesters: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Unable to load data. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading semesters");
                ErrorMessage = "Unable to load data. Please try again later.";
            }

            return Page();
        }


        public async Task<IActionResult> OnPostDeleteSemesterAsync(int Id)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/semesters/{Id}");
                
                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Semester deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Delete semester failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Unable to delete semester: {response.StatusCode}";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting semester");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }
    }
}