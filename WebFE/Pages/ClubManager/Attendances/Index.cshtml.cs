using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.ClubManager.Attendances
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<ActivityListItemDto> Activities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var req = new HttpRequestMessage(HttpMethod.Get, "api/activity/my-club-activities");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                req.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Activities = await res.Content.ReadFromJsonAsync<List<ActivityListItemDto>>() ?? new();
                
                // Filter only Approved activities for attendance management
                Activities = Activities.Where(a => a.Status == "Approved").ToList();
            }

            return Page();
        }
    }
}

