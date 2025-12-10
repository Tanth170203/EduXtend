using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.CommunicationPlans
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
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Get club ID first
                var clubRequest = new HttpRequestMessage(HttpMethod.Get, "api/club/my-managed-club");
                foreach (var cookie in Request.Cookies)
                {
                    clubRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var clubResponse = await client.SendAsync(clubRequest);
                if (!clubResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "You are not managing any club";
                    return Page();
                }

                var club = await clubResponse.Content.ReadFromJsonAsync<ClubDto>();
                if (club == null)
                {
                    return Page();
                }

                // Get communication plans
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/communication-plans/club/{club.Id}");
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

    public class ClubDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
