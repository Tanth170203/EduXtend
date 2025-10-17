using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Clubs
{
    public class ActiveModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public ActiveModel(IHttpClientFactory http) => _http = http;

        public List<ClubListItemDto> Clubs { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            var all = await client.GetFromJsonAsync<List<ClubListItemDto>>("api/club") ?? new();

            // ch? l?y IsActive = true
            Clubs = all.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        }

        public string Img(string? url, string fallback) =>
            string.IsNullOrWhiteSpace(url)
                ? Url.Content(fallback)
                : (Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url :
                    (url!.StartsWith("/") ? url : "/" + url));
    }
}
