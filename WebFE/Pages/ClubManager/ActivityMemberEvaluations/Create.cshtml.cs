using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.ActivityMemberEvaluations
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IHttpClientFactory httpClientFactory, ILogger<CreateModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int AssignmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ActivityId { get; set; }

        [BindProperty]
        public int ResponsibilityScore { get; set; } = 5;

        [BindProperty]
        public int SkillScore { get; set; } = 5;

        [BindProperty]
        public int AttitudeScore { get; set; } = 5;

        [BindProperty]
        public int EffectivenessScore { get; set; } = 5;

        [BindProperty]
        public string? Comments { get; set; }

        [BindProperty]
        public string? Strengths { get; set; }

        [BindProperty]
        public string? Improvements { get; set; }

        public string MemberName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ScheduleTitle { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (AssignmentId <= 0 || ActivityId <= 0)
            {
                TempData["Error"] = "Invalid parameters";
                return RedirectToPage("/ClubManager/Activities/Index");
            }

            try
            {
                // Get assignment details (you might need to add an endpoint for this)
                // For now, we'll just show the form
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
                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new
                {
                    ActivityScheduleAssignmentId = AssignmentId,
                    ResponsibilityScore,
                    SkillScore,
                    AttitudeScore,
                    EffectivenessScore,
                    Comments,
                    Strengths,
                    Improvements
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "api/activity-member-evaluations");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đánh giá thành viên thành công";
                    return RedirectToPage("./Index", new { activityId = ActivityId });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create evaluation: {Error}", errorContent);
                    ModelState.AddModelError(string.Empty, "Không thể tạo đánh giá");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating evaluation");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi");
                return Page();
            }
        }
    }
}
