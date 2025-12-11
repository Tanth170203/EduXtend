using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.GpsAttendance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Activities
{
    public class GpsCheckInModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public GpsCheckInModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ActivityDetailDto? Activity { get; private set; }
        public GpsAttendanceStatusDto? AttendanceStatus { get; private set; }
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");

            // Get activity details
            var activityReq = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{Id}");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                activityReq.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var activityResponse = await client.SendAsync(activityReq);
            if (activityResponse.IsSuccessStatusCode)
            {
                Activity = await activityResponse.Content.ReadFromJsonAsync<ActivityDetailDto>();
            }

            if (Activity == null) return NotFound();

            // Get GPS attendance status
            var statusReq = new HttpRequestMessage(HttpMethod.Get, $"api/gpsattendance/status/{Id}");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader2))
            {
                statusReq.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader2);
            }
            var statusResponse = await client.SendAsync(statusReq);
            if (statusResponse.IsSuccessStatusCode)
            {
                AttendanceStatus = await statusResponse.Content.ReadFromJsonAsync<GpsAttendanceStatusDto>();
            }

            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? string.Empty;

            return Page();
        }
    }
}
