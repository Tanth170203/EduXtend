using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.Clubs
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public DetailsModel(IHttpClientFactory http) => _http = http;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ClubDetailDto? Club { get; private set; }
        public List<ActivityListItemDto> Activities { get; private set; } = new();
        public JoinRequestDto? MyJoinRequest { get; private set; }
        public bool IsMember { get; private set; }
        public List<ClubAwardDto> Awards { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Create HttpClient with manual cookie handling
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Get AccessToken from browser cookie and forward to API
            var accessToken = Request.Cookies["AccessToken"];
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                // Add AccessToken cookie to the cookie container for API calls
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new Cookie("AccessToken", accessToken));
            }

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            // Get club details
            Club = await client.GetFromJsonAsync<ClubDetailDto>($"api/club/{Id}");
            if (Club == null) return NotFound();

            // Get club activities
            var allActivities = await client.GetFromJsonAsync<List<ActivityListItemDto>>($"api/activity/club/{Id}") ?? new();

            // Get club awards (public data)
            Awards = await client.GetFromJsonAsync<List<ClubAwardDto>>($"api/club/{Id}/awards") ?? new();

            // Check if user has existing join request and membership (only if authenticated)
            bool isAuthenticated = User.Identity?.IsAuthenticated == true;
            
            if (isAuthenticated)
            {
                try
                {
                    // Check if user is already a member
                    var memberResponse = await client.GetFromJsonAsync<Dictionary<string, bool>>($"api/club/{Id}/is-member");
                    IsMember = memberResponse?["isMember"] ?? false;
                    
                    // If user is a member, redirect to Member Dashboard
                    if (IsMember)
                    {
                        return RedirectToPage("/Clubs/MemberDashboard", new { id = Id });
                    }
                }
                catch
                {
                    IsMember = false;
                }

                try
                {
                    // Check for existing join request
                    MyJoinRequest = await client.GetFromJsonAsync<JoinRequestDto>($"api/joinrequest/my-request/{Id}");
                }
                catch
                {
                    // No existing request - that's fine
                    MyJoinRequest = null;
                }
            }
            
            // Filter activities based on user authentication and membership status
            // For guests (not authenticated) or non-members:
            // - Hide completed activities (Status == "Completed")
            // - Hide members-only activities (IsPublic == false)
            if (!isAuthenticated || !IsMember)
            {
                Activities = allActivities
                    .Where(a => a.Status != "Completed" && a.IsPublic)
                    .ToList();
            }
            else
            {
                // Members can see all activities
                Activities = allActivities;
            }

            return Page();
        }
    }
}
