using System.ComponentModel.DataAnnotations;
using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditModel> _logger;
        private readonly IConfiguration _config;

        public EditModel(IHttpClientFactory httpClientFactory, ILogger<EditModel> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        
        [BindProperty]
        public EditActivityDto Activity { get; set; } = new();
        
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
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

                var detail = await response.Content.ReadFromJsonAsync<ActivityDetailDto>();
                
                if (detail == null)
                {
                    return NotFound();
                }

                // Map to Edit DTO
                Activity = new EditActivityDto
                {
                    Title = detail.Title,
                    Description = detail.Description,
                    Location = detail.Location,
                    ImageUrl = detail.ImageUrl,
                    StartTime = detail.StartTime,
                    EndTime = detail.EndTime,
                    Type = detail.Type,
                    IsPublic = detail.IsPublic,
                    MaxParticipants = detail.MaxParticipants ?? 1,
                    MovementPoint = detail.MovementPoint,
                    ClubCollaborationId = detail.ClubCollaborationId,
                    CollaborationPoint = detail.CollaborationPoint
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity for edit {Id}", Id);
                return NotFound();
            }
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
                // Validate dates
                if (Activity.StartTime >= Activity.EndTime)
                {
                    ModelState.AddModelError("Activity.EndTime", "End time must be after start time");
                    return Page();
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Convert Type string to int
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
                    ClubCollaborationId = Activity.ClubCollaborationId,
                    CollaborationPoint = Activity.CollaborationPoint
                };

                var request = new HttpRequestMessage(HttpMethod.Put, $"api/activity/club-manager/{Id}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity updated successfully";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update activity {Id}: {Error}", Id, errorContent);
                    ErrorMessage = errorContent;
                    ModelState.AddModelError(string.Empty, "Failed to update activity. Please try again.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activity {Id}", Id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the activity");
                return Page();
            }
        }
    }

    public class EditActivityDto : IValidatableObject
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        public string Type { get; set; } = "ClubMeeting";
        public bool IsPublic { get; set; }
        [Required] [Range(1, int.MaxValue, ErrorMessage = "Max Participants is required")] public int MaxParticipants { get; set; }
        
        [Range(0, 1000)]
        public double MovementPoint { get; set; }
        
        public int? ClubCollaborationId { get; set; }
        public int? CollaborationPoint { get; set; }

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

