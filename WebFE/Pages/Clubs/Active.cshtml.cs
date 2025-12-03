using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

namespace WebFE.Pages.Clubs
{
    public class ActiveModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public ActiveModel(IHttpClientFactory http) => _http = http;

        public List<ClubListItemDto> Clubs { get; private set; } = new();
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; }

        [BindProperty(SupportsGet = true, Name = "page")]
        public int Page { get; set; } = 1;

        public List<string> Categories { get; set; } = new();



        public async Task OnGetAsync()
        {
            var client = CreateAuthenticatedClient();
            
            // Read page from query string as fallback
            if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out int pageFromQuery))
            {
                Page = pageFromQuery;
            }
            
            // Load categories for dropdown
            Categories = await client.GetFromJsonAsync<List<string>>("api/club/categories") ?? new();
            
            // Build query string for search
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (!string.IsNullOrWhiteSpace(CategoryName))
                queryParams.Add($"categoryName={Uri.EscapeDataString(CategoryName)}");
            
            // Always filter for active clubs
            queryParams.Add("isActive=true");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = queryParams.Count > 0 ? $"api/club/search{queryString}" : "api/club";
            
            var clubs = await client.GetFromJsonAsync<List<ClubListItemDto>>(endpoint) ?? new();

            // Apply sorting
            var sortedClubs = SortBy switch
            {
                "newest" => clubs.OrderByDescending(c => c.FoundedDate).ToList(),
                "members" => clubs.OrderByDescending(c => c.MemberCount).ToList(),
                "az" => clubs.OrderBy(c => c.Name).ToList(),
                _ => clubs.OrderBy(c => c.Name).ToList()
            };

            // Pagination
            CurrentPage = Page > 0 ? Page : 1;
            TotalCount = sortedClubs.Count;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            
            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;
            
            Clubs = sortedClubs
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }



        private HttpClient CreateAuthenticatedClient()
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Forward all cookies from the current request to the API
            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new System.Net.Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }
    }
}
