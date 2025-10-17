using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Clubs
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public IndexModel(IHttpClientFactory http) => _http = http;

        public List<ClubListItemDto> Clubs { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            var result = await client.GetFromJsonAsync<List<ClubListItemDto>>("api/club");
            if (result != null)
                Clubs = result;
        }
    }
}
