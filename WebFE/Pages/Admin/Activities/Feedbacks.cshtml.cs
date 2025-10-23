using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.Admin.Activities
{
    public class FeedbacksModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public FeedbacksModel(IHttpClientFactory http) => _http = http;

        public int ActivityId { get; set; }
        public List<AdminActivityFeedbackDto> Items { get; private set; } = new();

        public async Task OnGetAsync(int id)
        {
            ActivityId = id;
            var client = _http.CreateClient("ApiClient");
            var req = new HttpRequestMessage(HttpMethod.Get, $"api/admin/activities/{id}/feedbacks");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                req.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Items = await res.Content.ReadFromJsonAsync<List<AdminActivityFeedbackDto>>() ?? new();
            }
        }
    }
}


