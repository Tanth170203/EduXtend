using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.ActivityMemberEvaluations
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int ActivityId { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;
        public List<AssignmentForEvaluationDto> Assignments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (ActivityId <= 0)
            {
                TempData["Error"] = "Invalid activity ID";
                return RedirectToPage("/ClubManager/Activities/Index");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Get activity details
                var activityRequest = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{ActivityId}");
                foreach (var cookie in Request.Cookies)
                {
                    activityRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var activityResponse = await client.SendAsync(activityRequest);
                if (activityResponse.IsSuccessStatusCode)
                {
                    var activity = await activityResponse.Content.ReadFromJsonAsync<ActivityDto>();
                    ActivityTitle = activity?.Title ?? "Unknown Activity";
                }

                // Get assignments for evaluation
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/activities/{ActivityId}/evaluation-assignments");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Assignments = await response.Content.ReadFromJsonAsync<List<AssignmentForEvaluationDto>>() ?? new();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get assignments: {Error}", error);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading evaluation assignments");
                TempData["Error"] = "An error occurred while loading data";
                return Page();
            }
        }
    }

    public class ActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
    }

    public class AssignmentForEvaluationDto
    {
        public int AssignmentId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ResponsibleName { get; set; }
        public string? Role { get; set; }
        public string ScheduleTitle { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsEvaluated { get; set; }
        public double? AverageScore { get; set; }
        public DateTime? EvaluatedAt { get; set; }
    }
}
