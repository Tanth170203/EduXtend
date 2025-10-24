using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Student.MyClubs
{
    public class MembersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public MembersModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [BindProperty(SupportsGet = true)]
        public int ClubId { get; set; }

        public string ClubName { get; set; } = "Club";
        public List<ClubMemberItemDto> Members { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Load basic club info (name)
            var club = await client.GetFromJsonAsync<BusinessObject.DTOs.Club.ClubDetailDto>($"api/club/{ClubId}");
            if (club != null) ClubName = club.Name;

            Members = await client.GetFromJsonAsync<List<ClubMemberItemDto>>($"api/club/{ClubId}/members") ?? new();
            return Page();
        }
    }
}


