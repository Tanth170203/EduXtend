using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

namespace WebFE.Pages.Clubs
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public IndexModel(IHttpClientFactory http) => _http = http;

        public List<ClubListItemDto> Clubs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            
            // Build query string for search
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (IsActive.HasValue)
                queryParams.Add($"isActive={IsActive.Value}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = queryParams.Count > 0 ? $"api/club/search{queryString}" : "api/club";

            var result = await client.GetFromJsonAsync<List<ClubListItemDto>>(endpoint);
            if (result != null)
                Clubs = result.OrderBy(c => c.Name).ToList();
        }
    }
}
