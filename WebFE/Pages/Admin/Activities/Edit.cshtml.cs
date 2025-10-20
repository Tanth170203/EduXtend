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

        public EditModel(IHttpClientFactory httpClientFactory, ILogger<EditModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        [BindProperty] public AdminUpdateActivityInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
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
                Type = Enum.TryParse<ActivityType>(detail.Type, out var t) ? t : ActivityType.Other,
                IsPublic = detail.IsPublic,
                MaxParticipants = detail.MaxParticipants,
                MovementPoint = detail.MovementPoint
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                MovementPoint = Input.MovementPoint
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

        public class AdminUpdateActivityInput
        {
            public int Id { get; set; }
            [Required] public string Title { get; set; } = null!;
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? ImageUrl { get; set; }
            [Required] public DateTime StartTime { get; set; }
            [Required] public DateTime EndTime { get; set; }
            [Required] public ActivityType Type { get; set; } = ActivityType.AcademicClub;
            public bool IsPublic { get; set; } = true;
            public int? MaxParticipants { get; set; }
            [Range(0, 1000)] public double MovementPoint { get; set; }
        }
    }
}


