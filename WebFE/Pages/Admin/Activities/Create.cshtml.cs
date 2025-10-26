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
        private readonly IConfiguration _config;

        public CreateModel(IHttpClientFactory httpClientFactory, ILogger<CreateModel> logger, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = config;
        }

        [BindProperty]
        public AdminCreateActivityInput Input { get; set; } = new();
        
        public string ApiBaseUrl { get; set; } = string.Empty;

        public void OnGet()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            // Set default times without seconds
            var tomorrow = DateTime.Now.AddDays(1);
            Input.StartTime = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, tomorrow.Hour, tomorrow.Minute, 0);
            Input.EndTime = Input.StartTime.AddHours(2);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
            
            if (!ModelState.IsValid) return Page();

            // Server-side date validations
            if (Input.StartTime < DateTime.Now)
            {
                ModelState.AddModelError("Input.StartTime", "Không được chọn ngày giờ bắt đầu trong quá khứ");
                return Page();
            }
            if (Input.EndTime < DateTime.Now)
            {
                ModelState.AddModelError("Input.EndTime", "End time cannot be in the past");
                return Page();
            }
            if (Input.EndTime <= Input.StartTime)
            {
                ModelState.AddModelError("Input.EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu");
                return Page();
            }

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
            [Required] public ActivityType Type { get; set; } = ActivityType.LargeEvent;
            public bool IsPublic { get; set; } = true;
            public int? MaxParticipants { get; set; }
            [Range(0, 1000)] public double MovementPoint { get; set; } = 0;
        }
    }
}


