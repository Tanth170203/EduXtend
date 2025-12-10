using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.CommunicationPlans
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IHttpClientFactory httpClientFactory, ILogger<DetailsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public CommunicationPlanDto? Plan { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/communication-plans/{id}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Communication plan not found";
                    return RedirectToPage("./Index");
                }

                Plan = await response.Content.ReadFromJsonAsync<CommunicationPlanDto>();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plan details");
                return RedirectToPage("./Index");
            }
        }
    }
}
