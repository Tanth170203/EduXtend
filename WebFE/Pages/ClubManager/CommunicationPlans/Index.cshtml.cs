using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.CommunicationPlans
{
    public class IndexModel : ClubManagerPageModel
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

        public List<CommunicationPlanDto> Plans { get; set; } = new();
        public List<CommunicationPlanDto> FilteredPlans { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialize club context from TempData
            var result = await InitializeClubContextAsync();
            if (result is RedirectResult)
            {
                return result;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Get communication plans using ClubId from TempData
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/communication-plans/club/{ClubId}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Plans = await response.Content.ReadFromJsonAsync<List<CommunicationPlanDto>>() ?? new();
                }

                // Apply filters
                FilteredPlans = Plans;

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    FilteredPlans = FilteredPlans
                        .Where(p => p.ActivityTitle.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (FilterMonth.HasValue && FilterYear.HasValue)
                {
                    FilteredPlans = FilteredPlans
                        .Where(p => p.CreatedAt.Month == FilterMonth.Value && p.CreatedAt.Year == FilterYear.Value)
                        .ToList();
                }
                else if (FilterYear.HasValue)
                {
                    FilteredPlans = FilteredPlans
                        .Where(p => p.CreatedAt.Year == FilterYear.Value)
                        .ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading communication plans");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/communication-plans/{id}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Communication plan deleted successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete plan {Id}: {Error}", id, errorContent);
                    TempData["Error"] = "Failed to delete communication plan";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting communication plan");
                TempData["Error"] = "An error occurred while deleting the plan";
            }

            return RedirectToPage();
        }
    }
}
