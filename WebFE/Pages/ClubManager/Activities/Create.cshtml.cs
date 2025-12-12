using System.ComponentModel.DataAnnotations;
using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class CreateModel : ClubManagerPageModel
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
        public string? ExtractedSchedulesJson { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialize club context from TempData
            var result = await InitializeClubContextAsync();
            if (result is RedirectResult)
            {
                return result;
            }

            // Check if there's extracted data from proposal auto-fill
            if (TempData["ExtractedActivityData"] is string extractedJson)
            {
                try
                {
                    var extractedData = JsonSerializer.Deserialize<ExtractedActivityDto>(extractedJson);
                    if (extractedData != null)
                    {
                        // Pre-fill form fields from extracted data
                        Activity.Title = extractedData.Title ?? string.Empty;
                        Activity.Description = extractedData.Description;
                        Activity.Location = extractedData.Location;
                        
                        if (extractedData.StartTime.HasValue)
                        {
                            Activity.StartTime = extractedData.StartTime.Value;
                        }
                        else
                        {
                            Activity.StartTime = DateTime.Now.AddDays(7);
                        }
                        
                        if (extractedData.EndTime.HasValue)
                        {
                            Activity.EndTime = extractedData.EndTime.Value;
                        }
                        else
                        {
                            Activity.EndTime = Activity.StartTime.AddHours(2);
                        }
                        
                        if (extractedData.MaxParticipants.HasValue)
                        {
                            Activity.MaxParticipants = extractedData.MaxParticipants.Value;
                        }
                        
                        if (!string.IsNullOrEmpty(extractedData.SuggestedType))
                        {
                            Activity.Type = extractedData.SuggestedType;
                        }
                        else
                        {
                            Activity.Type = "ClubMeeting";
                        }
                        
                        // Store schedules for JavaScript to pick up
                        if (extractedData.Schedules != null && extractedData.Schedules.Any())
                        {
                            ExtractedSchedulesJson = JsonSerializer.Serialize(extractedData.Schedules);
                        }
                        
                        _logger.LogInformation("Pre-filled activity form from proposal {ProposalId}", extractedData.ProposalId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing extracted activity data");
                }
            }
            else
            {
                // Set default values if no extracted data
                Activity.StartTime = DateTime.Now.AddDays(7);
                Activity.EndTime = DateTime.Now.AddDays(7).AddHours(2);
                Activity.Type = "ClubMeeting";
            }

            // Set common defaults
            Activity.IsPublic = false;
            Activity.RequiresApproval = true; // Always true for ClubManager
            Activity.Location = Activity.Location ?? "Da Nang, Vietnam"; // Default location text
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            return Page();
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
                
                // Use ClubId from TempData (already set by InitializeClubContextAsync)
                if (ClubId <= 0)
                {
                    ModelState.AddModelError(string.Empty, "You are not managing any club.");
                    return Page();
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"api/activity/club-manager?clubId={ClubId}");
                
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
                    "ClubCollaboration" => 10,
                    "SchoolCollaboration" => 11,
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
                    IsMandatory = Activity.IsMandatory,
                    ClubCollaborationId = Activity.ClubCollaborationId,
                    CollaborationPoint = Activity.CollaborationPoint,
                    // GPS Configuration
                    GpsLatitude = Activity.GpsLatitude,
                    GpsLongitude = Activity.GpsLongitude,
                    IsGpsCheckInEnabled = Activity.IsGpsCheckInEnabled,
                    GpsCheckInRadius = Activity.GpsCheckInRadius,
                    CheckInWindowMinutes = Activity.CheckInWindowMinutes,
                    CheckOutWindowMinutes = Activity.CheckOutWindowMinutes
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

    public class CreateActivityDto : IValidatableObject
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } = "ClubMeeting";
        public bool RequiresApproval { get; set; } = true;
        public bool IsPublic { get; set; }
        [Required] [Range(1, int.MaxValue, ErrorMessage = "Max Participants is required")] public int MaxParticipants { get; set; }
        public double MovementPoint { get; set; }
        public bool IsMandatory { get; set; }
        public int? ClubCollaborationId { get; set; }
        public int? CollaborationPoint { get; set; }
        
        // GPS Location fields for GPS-based attendance (default: Đà Nẵng)
        public double? GpsLatitude { get; set; } = 15.967483;
        public double? GpsLongitude { get; set; } = 108.260361;
        
        // GPS Check-in configuration (always enabled by default)
        public bool IsGpsCheckInEnabled { get; set; } = true;
        public int GpsCheckInRadius { get; set; } = 100; // Default 100m
        public int CheckInWindowMinutes { get; set; } = 10; // Default 10 minutes
        public int CheckOutWindowMinutes { get; set; } = 10; // Default 10 minutes

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MaxParticipants > 0)
            {
                if (Type == "LargeEvent" && (MaxParticipants < 100 || MaxParticipants > 200))
                {
                    yield return new ValidationResult(
                        "Large Event phải có số người tham gia từ 100-200 người",
                        new[] { nameof(MaxParticipants) });
                }
                else if (Type == "MediumEvent" && (MaxParticipants < 50 || MaxParticipants > 100))
                {
                    yield return new ValidationResult(
                        "Medium Event phải có số người tham gia từ 50-100 người",
                        new[] { nameof(MaxParticipants) });
                }
                else if (Type == "SmallEvent" && MaxParticipants >= 50)
                {
                    yield return new ValidationResult(
                        "Small Event phải có số người tham gia dưới 50 người",
                        new[] { nameof(MaxParticipants) });
                }
            }
        }
    }
}

