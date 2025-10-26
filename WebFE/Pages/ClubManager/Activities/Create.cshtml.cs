using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
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
        public CreateActivityDto Activity { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string ApiBaseUrl { get; set; } = string.Empty;

        public void OnGet()
        {
            // Set default values
            Activity.StartTime = DateTime.Now.AddDays(7);
            Activity.EndTime = DateTime.Now.AddDays(7).AddHours(2);
            Activity.IsPublic = false;
            Activity.RequiresApproval = true; // Always true for ClubManager
            Activity.Type = "AcademicClub";
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
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

                // Validate dates
                if (Activity.StartTime < DateTime.Now)
                {
                    ModelState.AddModelError("Activity.StartTime", "Không được chọn ngày giờ bắt đầu trong quá khứ");
                    return Page();
                }

                if (Activity.EndTime < DateTime.Now)
                {
                    ModelState.AddModelError("Activity.EndTime", "End time cannot be in the past");
                    return Page();
                }

                if (Activity.StartTime >= Activity.EndTime)
                {
                    ModelState.AddModelError("Activity.EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu");
                    return Page();
                }

                // Check if activity type is Club Activity (internal, no approval needed)
                bool isClubActivity = Activity.Type == "ClubMeeting" || 
                                     Activity.Type == "ClubTraining" || 
                                     Activity.Type == "ClubWorkshop";
                
                // Club Activities don't require approval, others do
                Activity.RequiresApproval = !isClubActivity;

                var client = _httpClientFactory.CreateClient("ApiClient");
                // Fetch managed club id
                var clubReq = new HttpRequestMessage(HttpMethod.Get, "api/club/my-managed-club");
                foreach (var ck in Request.Cookies) clubReq.Headers.Add("Cookie", $"{ck.Key}={ck.Value}");
                var clubResp = await client.SendAsync(clubReq);
                int clubId = 0;
                if (clubResp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await clubResp.Content.ReadAsStringAsync());
                    clubId = doc.RootElement.GetProperty("id").GetInt32();
                }
                if (clubId <= 0)
                {
                    ModelState.AddModelError(string.Empty, "You are not managing any club.");
                    return Page();
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"api/activity/club-manager?clubId={clubId}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                // Map FE model to backend ClubCreateActivityDto structure
                // Convert Type string to enum int value
                int typeValue = Activity.Type switch
                {
                    "ClubMeeting" => 0,
                    "ClubTraining" => 1,
                    "ClubWorkshop" => 2,
                    "LargeEvent" => 3,
                    "MediumEvent" => 4,
                    "SmallEvent" => 5,
                    "SchoolCompetition" => 6,
                    "ProvincialCompetition" => 7,
                    "NationalCompetition" => 8,
                    "Volunteer" => 9,
                    "ClubCollaboration" => 10,
                    "SchoolCollaboration" => 11,
                    "EnterpriseCollaboration" => 12,
                    "Other" => 13,
                    _ => 0
                };

                var payload = new
                {
                    Title = Activity.Title,
                    Description = Activity.Description,
                    Location = Activity.Location,
                    ImageUrl = Activity.ImageUrl,
                    StartTime = Activity.StartTime,
                    EndTime = Activity.EndTime,
                    Type = typeValue,
                    IsPublic = Activity.IsPublic,
                    MaxParticipants = Activity.MaxParticipants,
                    MovementPoint = Activity.MovementPoint,
                    IsMandatory = Activity.IsMandatory
                };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity created successfully and submitted for approval";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create activity: {Error}", errorContent);
                    ErrorMessage = errorContent;
                    ModelState.AddModelError(string.Empty, "Failed to create activity. Please try again.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the activity");
                return Page();
            }
        }
    }

    public class CreateActivityDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = "ClubMeeting";
        public bool RequiresApproval { get; set; } = true;
        public bool IsPublic { get; set; }
        public int? MaxParticipants { get; set; }
        public double MovementPoint { get; set; }
        public bool IsMandatory { get; set; }
    }
}

