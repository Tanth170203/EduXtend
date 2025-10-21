using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

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

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            
            // Get club details
            Club = await client.GetFromJsonAsync<ClubDetailDto>($"api/club/{Id}");
            if (Club == null) return NotFound();

            // Get club activities
            Activities = await client.GetFromJsonAsync<List<ActivityListItemDto>>($"api/activity/club/{Id}") ?? new();

            return Page();
        }
    }
}
