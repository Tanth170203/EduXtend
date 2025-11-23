using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.CommunicationPlans
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CreateModel> _logger;
        private readonly IConfiguration _config;

        public CreateModel(IHttpClientFactory httpClientFactory, ILogger<CreateModel> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
        }

        [BindProperty]
        public int ActivityId { get; set; }

        [BindProperty]
        public List<CommunicationItemInput> Items { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public List<ActivityOption> Activities { get; set; } = new();
        public List<ClubMemberOption> ClubMembers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? activityId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                // Load activities without communication plans
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, "api/communication-plans/available-activities");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Activities = await response.Content.ReadFromJsonAsync<List<ActivityOption>>() ?? new();
                }

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

                // Pre-select activity if provided
                if (activityId.HasValue)
                {
                    ActivityId = activityId.Value;
                }

                // Initialize with one empty item
                Items.Add(new CommunicationItemInput { Order = 1, ScheduledDate = DateTime.Now });

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create page");
                return Page();
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
                    ActivityId = ActivityId,
                    Items = Items.Select((item, index) => new
                    {
                        Order = index + 1,
                        Content = item.Content,
                        ScheduledDate = item.ScheduledDate,
                        ResponsiblePerson = item.ResponsiblePerson,
                        Notes = item.Notes
                    }).ToList()
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "api/communication-plans");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Communication plan created successfully";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create plan: {Error}", errorContent);
                    ErrorMessage = errorContent;
                    ModelState.AddModelError(string.Empty, "Failed to create communication plan");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating communication plan");
                ModelState.AddModelError(string.Empty, "An error occurred");
                return Page();
            }
        }
    }

    public class ActivityOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class ClubMemberOption
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Position { get; set; }
    }
}
