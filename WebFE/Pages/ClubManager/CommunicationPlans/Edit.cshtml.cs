using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.CommunicationPlans
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IHttpClientFactory httpClientFactory, ILogger<EditModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public int PlanId { get; set; }

        [BindProperty]
        public List<CommunicationItemInput> Items { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public List<ClubMemberOption> ClubMembers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                PlanId = id;

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

                var plan = await response.Content.ReadFromJsonAsync<CommunicationPlanDto>();
                if (plan == null)
                {
                    return RedirectToPage("./Index");
                }

                ActivityTitle = plan.ActivityTitle;
                Items = plan.Items.Select(i => new CommunicationItemInput
                {
                    Order = i.Order,
                    Content = i.Content,
                    ScheduledDate = i.ScheduledDate,
                    ResponsiblePerson = i.ResponsiblePerson,
                    Notes = i.Notes
                }).ToList();

                // Load club members
                var clubIdClaim = User.FindFirst("ClubId")?.Value;
                if (!string.IsNullOrEmpty(clubIdClaim) && int.TryParse(clubIdClaim, out int clubId))
                {
                    var membersRequest = new HttpRequestMessage(HttpMethod.Get, $"api/club/{clubId}/members");
                    foreach (var cookie in Request.Cookies)
                    {
                        membersRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                    }

                    var membersResponse = await client.SendAsync(membersRequest);
                    if (membersResponse.IsSuccessStatusCode)
                    {
                        ClubMembers = await membersResponse.Content.ReadFromJsonAsync<List<ClubMemberOption>>() ?? new();
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plan for edit");
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new
                {
                    Items = Items.Select((item, index) => new
                    {
                        Order = index + 1,
                        Content = item.Content,
                        ScheduledDate = item.ScheduledDate,
                        ResponsiblePerson = item.ResponsiblePerson,
                        Notes = item.Notes
                    }).ToList()
                };

                var request = new HttpRequestMessage(HttpMethod.Put, $"api/communication-plans/{PlanId}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Communication plan updated successfully";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update plan: {Error}", errorContent);
                    ErrorMessage = errorContent;
                    ModelState.AddModelError(string.Empty, "Failed to update communication plan");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication plan");
                ModelState.AddModelError(string.Empty, "An error occurred");
                return Page();
            }
        }
    }
}
