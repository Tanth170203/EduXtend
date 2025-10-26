using BusinessObject.DTOs.Activity;
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

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; } // "all", "public", "club"

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");

            // Build query string
            var queryParams = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            
            if (!string.IsNullOrWhiteSpace(Type))
                queryParams.Add($"type={Uri.EscapeDataString(Type)}");
            
            // Always filter only Approved activities for public page
            queryParams.Add("status=Approved");

            // Filter by public/club activities
            if (Filter == "public")
                queryParams.Add("isPublic=true");
            else if (Filter == "club")
                queryParams.Add("isPublic=false");

            var queryString = "?" + string.Join("&", queryParams);
            var endpoint = $"api/activity/search{queryString}";

            Activities = await client.GetFromJsonAsync<List<ActivityListItemDto>>(endpoint) ?? new();
        }
    }
}

