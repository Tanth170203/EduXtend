using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.Student.MyActivities
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        public IndexModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public List<ActivityListItemDto> Items { get; private set; } = new();
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            var req = new HttpRequestMessage(HttpMethod.Get, "api/activity/my-registrations");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                req.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Items = await res.Content.ReadFromJsonAsync<List<ActivityListItemDto>>() ?? new();
            }
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? string.Empty;
        }
    }
}


