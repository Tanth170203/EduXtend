using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Net.Http.Headers;

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
        public bool IsCurrentUserMember { get; private set; }
        public bool IsCurrentUserManager { get; private set; }
        public string? CurrentUserRole { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            
            // Get club details
            Club = await client.GetFromJsonAsync<ClubDetailDto>($"api/club/{Id}");
            if (Club == null) return NotFound();

            // Get club activities
            Activities = await client.GetFromJsonAsync<List<ActivityListItemDto>>($"api/activity/club/{Id}") ?? new();

            // Determine current user's membership/role
            var token = Request.Cookies["AccessToken"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                var authClient = _http.CreateClient("ApiClient");
                authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var myClubs = await authClient.GetFromJsonAsync<List<MyClubItemDto>>("api/club/my");
                var membership = myClubs?.FirstOrDefault(x => x.ClubId == Id);
                if (membership != null)
                {
                    IsCurrentUserMember = true;
                    IsCurrentUserManager = membership.IsManager;
                    CurrentUserRole = membership.RoleInClub;
                    if (IsCurrentUserManager)
                    {
                        // Managers go to manager dashboard instead of public details
                        return Redirect("/ClubManager");
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLeaveAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");
            var client = _http.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PostAsync($"api/club/{Id}/leave", null);
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể rời CLB";
            }
            return RedirectToPage(new { id = Id });
        }
    }
}
