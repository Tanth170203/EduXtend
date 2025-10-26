using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class AttendancesModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        public AttendancesModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public List<AdminActivityRegistrantDto> Registrants { get; private set; } = new();
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync(int id)
        {
            ActivityId = id;
            var client = _http.CreateClient("ApiClient");
            var req = new HttpRequestMessage(HttpMethod.Get, $"api/activity/club-manager/{id}/registrants");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                req.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Registrants = await res.Content.ReadFromJsonAsync<List<AdminActivityRegistrantDto>>() ?? new();
            }

            // Get activity details for title
            var detailReq = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{id}");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader2))
            {
                detailReq.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader2);
            }
            var detailRes = await client.SendAsync(detailReq);
            if (detailRes.IsSuccessStatusCode)
            {
                var detail = await detailRes.Content.ReadFromJsonAsync<ActivityDetailDto>();
                ActivityTitle = detail?.Title ?? "Activity";
            }

            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? string.Empty;
        }
    }
}

