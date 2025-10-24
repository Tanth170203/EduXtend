using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Activities
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public IndexModel(IHttpClientFactory http) => _http = http;

        public List<ActivityListItemDto> Activities { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 9;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; } // "all", "public", "club"

        public async Task OnGetAsync([FromQuery] int page = 1)
        {
            var client = _http.CreateClient("ApiClient");

            // Build query string
            var queryParams = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            
            if (!string.IsNullOrWhiteSpace(Type))
                queryParams.Add($"type={Uri.EscapeDataString(Type)}");
            
            if (!string.IsNullOrWhiteSpace(Status))
                queryParams.Add($"status={Uri.EscapeDataString(Status)}");

            // Filter by public/club activities
            if (Filter == "public")
                queryParams.Add("isPublic=true");
            else if (Filter == "club")
                queryParams.Add("isPublic=false");

            // pagination
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={PageSize}");

            var queryString = "?" + string.Join("&", queryParams);
            var endpoint = (queryParams.Any(p => p.StartsWith("searchTerm=") || p.StartsWith("type=") || p.StartsWith("status=") || p.StartsWith("isPublic=")))
                ? $"api/activity/search-paged{queryString}"
                : $"api/activity/paged{queryString}";

            var result = await client.GetFromJsonAsync<PagedResult<ActivityListItemDto>>(endpoint);
            if (result != null)
            {
                Activities = result.Items ?? new();
                PageNumber = result.PageNumber;
                PageSize = result.PageSize;
                TotalItems = result.TotalItems;
                TotalPages = result.TotalPages;
            }
        }
    }
}

