using System.ComponentModel.DataAnnotations;
using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Activities
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

        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        [BindProperty] public AdminUpdateActivityInput Input { get; set; } = new();
        public string ApiBaseUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var detail = await client.GetFromJsonAsync<ActivityDetailDto>($"/api/admin/activities/{Id}");
            if (detail == null) return NotFound();

            Input = new AdminUpdateActivityInput
            {
                Id = detail.Id,
                Title = detail.Title,
                Description = detail.Description,
                Location = detail.Location,
                ImageUrl = detail.ImageUrl,
                StartTime = detail.StartTime,
                EndTime = detail.EndTime,
                Type = Enum.TryParse<ActivityType>(detail.Type, out var t) ? t : ActivityType.LargeEvent,
                IsPublic = detail.IsPublic,
                MaxParticipants = detail.MaxParticipants ?? 1,
                MovementPoint = detail.MovementPoint,
                ClubCollaborationId = detail.ClubCollaborationId,
                CollaborationPoint = detail.CollaborationPoint,
                CollaboratingClubName = detail.CollaboratingClubName
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            if (!ModelState.IsValid) return Page();
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new AdminUpdateActivityDto
            {
                Id = Input.Id,
                Title = Input.Title,
                Description = Input.Description,
                Location = Input.Location,
                ImageUrl = Input.ImageUrl,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                Type = Input.Type,
                IsPublic = Input.IsPublic,
                MaxParticipants = Input.MaxParticipants,
                MovementPoint = Input.MovementPoint,
                ClubCollaborationId = Input.ClubCollaborationId,
                CollaborationPoint = Input.CollaborationPoint
            };

            var resp = await client.PutAsJsonAsync($"/api/admin/activities/{Input.Id}", dto);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Update activity failed: {Status} {Msg}", resp.StatusCode, msg);
                ModelState.AddModelError(string.Empty, "Failed to update activity");
                return Page();
            }

            return RedirectToPage("/Admin/Activities/Index");
        }

        public class AdminUpdateActivityInput : IValidatableObject
        {
            public int Id { get; set; }
            [Required] public string Title { get; set; } = null!;
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? ImageUrl { get; set; }
            [Required] public DateTime StartTime { get; set; }
            [Required] public DateTime EndTime { get; set; }
            [Required] public ActivityType Type { get; set; } = ActivityType.LargeEvent;
            public bool IsPublic { get; set; } = true;
            [Required] [Range(1, int.MaxValue, ErrorMessage = "Max Participants is required")] public int MaxParticipants { get; set; }
            [Range(0, 1000)] public double MovementPoint { get; set; }
            public int? ClubCollaborationId { get; set; }
            [Range(1, 3)] public int? CollaborationPoint { get; set; }
            public string? CollaboratingClubName { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (MaxParticipants > 0)
                {
                    if (Type == ActivityType.LargeEvent && (MaxParticipants < 100 || MaxParticipants > 200))
                    {
                        yield return new ValidationResult(
                            "Large Event phải có số người tham gia từ 100-200 người",
                            new[] { nameof(MaxParticipants) });
                    }
                    else if (Type == ActivityType.MediumEvent && (MaxParticipants < 50 || MaxParticipants > 100))
                    {
                        yield return new ValidationResult(
                            "Medium Event phải có số người tham gia từ 50-100 người",
                            new[] { nameof(MaxParticipants) });
                    }
                    else if (Type == ActivityType.SmallEvent && MaxParticipants >= 50)
                    {
                        yield return new ValidationResult(
                            "Small Event phải có số người tham gia dưới 50 người",
                            new[] { nameof(MaxParticipants) });
                    }
                }
            }
        }
    }
}


