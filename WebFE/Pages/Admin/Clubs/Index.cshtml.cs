using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Admin.Clubs
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public IndexModel(IHttpClientFactory http) => _http = http;

        public List<ClubListItemDto> Clubs { get; set; } = new();
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        [BindProperty(SupportsGet = true, Name = "page")]
        public int Page { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            
            // Read page from query string as fallback
            if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out int pageFromQuery))
            {
                Page = pageFromQuery;
            }
            
            // Build query string for search
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (IsActive.HasValue)
                queryParams.Add($"isActive={IsActive.Value}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = queryParams.Count > 0 ? $"api/club/search{queryString}" : "api/club";

            var result = await client.GetFromJsonAsync<List<ClubListItemDto>>(endpoint);
            var allClubs = result != null ? result.OrderBy(c => c.Name).ToList() : new List<ClubListItemDto>();

            // Pagination
            CurrentPage = Page > 0 ? Page : 1;
            TotalCount = allClubs.Count;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            
            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;
            
            Clubs = allClubs
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}
