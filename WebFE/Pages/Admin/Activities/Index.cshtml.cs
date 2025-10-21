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

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchTerm)) qs.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");
            if (!string.IsNullOrWhiteSpace(Type)) qs.Add($"type={Uri.EscapeDataString(Type)}");
            if (!string.IsNullOrWhiteSpace(Status)) qs.Add($"status={Uri.EscapeDataString(Status)}");
            if (IsPublic.HasValue) qs.Add($"isPublic={(IsPublic.Value ? "true" : "false")}");
            var endpoint = qs.Count > 0 ? $"/api/admin/activities?{string.Join("&", qs)}" : "/api/admin/activities";

            Items = await client.GetFromJsonAsync<List<ActivityListItemDto>>(endpoint) ?? new();
            
            // Calculate statistics
            TotalActivities = Items.Count;
            ApprovedActivities = Items.Count(a => a.Status == "Approved");
            PendingActivities = Items.Count(a => a.Status == "PendingApproval");
            UpcomingActivities = Items.Count(a => a.StartTime > DateTime.Now && a.Status == "Approved");
            
            return Page();
        }
    }
}


