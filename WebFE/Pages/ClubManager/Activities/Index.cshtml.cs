using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.Activities
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public List<ActivityListItemDto> Activities { get; set; } = new();
        public string ApiBaseUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Forward cookies
                var request = new HttpRequestMessage(HttpMethod.Get, "api/activity/my-club-activities");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Activities = await response.Content.ReadFromJsonAsync<List<ActivityListItemDto>>() ?? new();
                }

                ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club activities");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/activity/club-manager/{id}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity deleted successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete activity {Id}: {Error}", id, errorContent);
                    TempData["Error"] = "Failed to delete activity. You may not have permission.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting activity");
                TempData["Error"] = "An error occurred while deleting the activity";
            }

            return RedirectToPage();
        }
    }
}


