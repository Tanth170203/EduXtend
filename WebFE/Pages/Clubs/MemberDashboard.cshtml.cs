using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Json;

namespace WebFE.Pages.Clubs
{
    public class MemberDashboardModel : PageModel
    {
        private readonly ILogger<MemberDashboardModel> _logger;

        public MemberDashboardModel(ILogger<MemberDashboardModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Section { get; set; } = "overview";

        public ClubDetailDto? Club { get; private set; }
        public List<ClubMemberDto> Members { get; private set; } = new();
        public List<DepartmentDto> Departments { get; private set; } = new();
        public List<ActivityListItemDto> Activities { get; private set; } = new();
        public List<ClubAwardDto> Awards { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Please login to access member dashboard";
                return RedirectToPage("/Auth/Login");
            }

            if (Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid club ID";
                return RedirectToPage("/Clubs/Active");
            }

            try
            {
                // Create HttpClient with cookie forwarding
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var accessToken = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    handler.CookieContainer.Add(
                        new Uri("https://localhost:5001"),
                        new Cookie("AccessToken", accessToken)
                    );
                }

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                // Check if user is member
                var memberResponse = await client.GetFromJsonAsync<Dictionary<string, bool>>($"api/club/{Id}/is-member");
                var isMember = memberResponse?["isMember"] ?? false;

                if (!isMember)
                {
                    TempData["ErrorMessage"] = "You must be a member to access this dashboard";
                    return RedirectToPage("/Clubs/Details", new { id = Id });
                }

                // Fetch club details
                Club = await client.GetFromJsonAsync<ClubDetailDto>($"api/club/{Id}");
                if (Club == null) return NotFound();

                // Fetch all data
                Members = await client.GetFromJsonAsync<List<ClubMemberDto>>($"api/club/{Id}/members") ?? new();
                Departments = await client.GetFromJsonAsync<List<DepartmentDto>>($"api/club/{Id}/departments") ?? new();
                Activities = await client.GetFromJsonAsync<List<ActivityListItemDto>>($"api/activity/club/{Id}") ?? new();
                Awards = await client.GetFromJsonAsync<List<ClubAwardDto>>($"api/club/{Id}/awards") ?? new();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading member dashboard for club {ClubId}", Id);
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard";
                return RedirectToPage("/Clubs/Details", new { id = Id });
            }
        }
    }
}

