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
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; } // "approved", "completed"

        [BindProperty(SupportsGet = true, Name = "page")]
        public int Page { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");

            // Try to read page from query string directly as fallback
            if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out int pageFromQuery))
            {
                Page = pageFromQuery;
            }

            // Build query string with pagination
            var queryParams = new List<string>();
            
            // Add pagination parameters
            CurrentPage = Page > 0 ? Page : 1;
            queryParams.Add($"page={CurrentPage}");
            queryParams.Add($"pageSize={PageSize}");
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            
            if (!string.IsNullOrWhiteSpace(Type))
                queryParams.Add($"type={Uri.EscapeDataString(Type)}");
            
            // Always show only public activities for the public page
            queryParams.Add("isPublic=true");

            var queryString = "?" + string.Join("&", queryParams);
            var endpoint = $"api/activity/search{queryString}";

            var result = await client.GetFromJsonAsync<BusinessObject.DTOs.Common.PaginatedResultDto<ActivityListItemDto>>(endpoint);
            
            if (result != null)
            {
                var now = DateTime.Now;
                
                // Filter to show only Approved and Completed activities
                var filteredItems = result.Items
                    .Where(a => a.Status == "Approved" || a.Status == "Completed")
                    .ToList();
                
                // Apply status filter
                if (!string.IsNullOrEmpty(Status))
                {
                    if (Status == "approved")
                    {
                        // Approved: activities that are approved (not completed yet)
                        filteredItems = filteredItems
                            .Where(a => a.Status == "Approved")
                            .ToList();
                    }
                    else if (Status == "completed")
                    {
                        // Completed: activities that have ended
                        filteredItems = filteredItems
                            .Where(a => a.Status == "Completed")
                            .ToList();
                    }
                }
                
                Activities = filteredItems;
                CurrentPage = result.CurrentPage;
                TotalPages = result.TotalPages;
                TotalCount = filteredItems.Count;
            }
        }
    }
}

