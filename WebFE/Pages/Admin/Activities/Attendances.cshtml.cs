using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.Admin.Activities
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
        public List<AdminActivityRegistrantDto> Registrants { get; private set; } = new();
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public async Task OnGetAsync(int id)
        {
            ActivityId = id;
            var client = _http.CreateClient("ApiClient");
            var req = new HttpRequestMessage(HttpMethod.Get, $"api/admin/activities/{id}/registrants");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                req.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var res = await client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Registrants = await res.Content.ReadFromJsonAsync<List<AdminActivityRegistrantDto>>() ?? new();
            }
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? string.Empty;
        }
    }
}




