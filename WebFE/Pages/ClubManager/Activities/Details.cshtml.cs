using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class DetailsModel : ClubManagerPageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailsModel> _logger;
        private readonly IConfiguration _config;

        public DetailsModel(IHttpClientFactory httpClientFactory, ILogger<DetailsModel> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        
        public ActivityDetailDto? Activity { get; set; }
        public List<AdminActivityFeedbackDto> Feedbacks { get; set; } = [];
        public string ApiBaseUrl { get; set; } = string.Empty;
        public bool IsCollaboratedActivity { get; set; }

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
                ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
                
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{Id}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to load activity {Id}: {Status}", Id, response.StatusCode);
                    return NotFound();
                }

                Activity = await response.Content.ReadFromJsonAsync<ActivityDetailDto>();
                
                if (Activity == null)
                {
                    return NotFound();
                }

                // Check if this is a collaborated activity (current user's club is the collaborating club, not owner)
                // Use ClubId from TempData (already set by InitializeClubContextAsync)
                IsCollaboratedActivity = Activity.ClubCollaborationId == ClubId && Activity.ClubId != ClubId;

                // Load feedbacks
                await LoadFeedbacksAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity details {Id}", Id);
                return NotFound();
            }
        }

        private async Task LoadFeedbacksAsync()
        {
            try
            {
                var token = Request.Cookies["AccessToken"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("No access token found for feedback request");
                    return;
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{Id}/feedbacks");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    Feedbacks = await response.Content.ReadFromJsonAsync<List<AdminActivityFeedbackDto>>() ?? [];
                    _logger.LogInformation("Loaded {Count} feedbacks for activity {Id}", Feedbacks.Count, Id);
                }
                else
                {
                    _logger.LogWarning("Failed to load feedbacks for activity {Id}: {Status}", Id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feedbacks for activity {Id}", Id);
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
                    TempData["SuccessMessage"] = "Activity deleted successfully!";
                    _logger.LogInformation("Deleted activity {Id}", id);
                    return RedirectToPage("/ClubManager/Activities/Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete activity. You may not have permission or the activity may not exist.";
                    _logger.LogError("Failed to delete activity {Id}: {Status}", id, response.StatusCode);
                    return RedirectToPage();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting activity {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the activity.";
                return RedirectToPage();
            }
        }
    }
}

