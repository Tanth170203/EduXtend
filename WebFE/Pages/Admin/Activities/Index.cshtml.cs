using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Activities
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<ActivityListItemDto> Items { get; set; } = new();
        public int TotalActivities { get; set; }
        public int ApprovedActivities { get; set; }
        public int PendingActivities { get; set; }
        public int UpcomingActivities { get; set; }

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? Type { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public bool? IsPublic { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Show all activities regardless of status
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchTerm)) qs.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (!string.IsNullOrWhiteSpace(Type)) qs.Add($"type={Uri.EscapeDataString(Type)}");
            if (!string.IsNullOrWhiteSpace(Status)) qs.Add($"status={Uri.EscapeDataString(Status)}");
            if (IsPublic.HasValue) qs.Add($"isPublic={(IsPublic.Value ? "true" : "false")}");
            var endpoint = $"/api/admin/activities?{string.Join("&", qs)}";

            var allItems = await client.GetFromJsonAsync<List<ActivityListItemDto>>(endpoint) ?? new();
            
            // Calculate pagination
            TotalItems = allItems.Count;
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
            
            // Apply pagination
            Items = allItems
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            
            // Calculate statistics from all activities
            var allActivities = await client.GetFromJsonAsync<List<ActivityListItemDto>>("/api/admin/activities") ?? new();
            TotalActivities = allActivities.Count;
            ApprovedActivities = allActivities.Count(a => a.Status == "Approved");
            PendingActivities = allActivities.Count(a => a.Status == "PendingApproval");
            UpcomingActivities = allActivities.Count(a => a.StartTime > DateTime.Now);
            
            return Page();
        }
    }
}


