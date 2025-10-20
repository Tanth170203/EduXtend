using System.ComponentModel.DataAnnotations;
using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Activities
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

        [BindProperty]
        public AdminCreateActivityInput Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new AdminCreateActivityDto
            {
                Title = Input.Title,
                Description = Input.Description,
                Location = Input.Location,
                ImageUrl = Input.ImageUrl,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                Type = Input.Type,
                IsPublic = Input.IsPublic,
                MaxParticipants = Input.MaxParticipants,
                MovementPoint = Input.MovementPoint
            };

            var resp = await client.PostAsJsonAsync("/api/admin/activities", dto);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Create activity failed: {Status} {Msg}", resp.StatusCode, msg);
                ModelState.AddModelError(string.Empty, "Failed to create activity");
                return Page();
            }

            return RedirectToPage("/Admin/Activities/Index");
        }

        public class AdminCreateActivityInput
        {
            [Required] public string Title { get; set; } = null!;
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? ImageUrl { get; set; }
            [Required] public DateTime StartTime { get; set; } = DateTime.Now.AddDays(1);
            [Required] public DateTime EndTime { get; set; } = DateTime.Now.AddDays(1).AddHours(2);
            [Required] public ActivityType Type { get; set; } = ActivityType.AcademicClub;
            public bool IsPublic { get; set; } = true;
            public int? MaxParticipants { get; set; }
            [Range(0, 1000)] public double MovementPoint { get; set; } = 0;
        }
    }
}


