using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebFE.Pages.Activities
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        public DetailsModel(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ActivityDetailDto? Activity { get; private set; }
        public string ApiBaseUrl { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            // Forward cookies to backend so it can resolve current user and IsRegistered
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/activity/{Id}");
            if (Request.Headers.TryGetValue("Cookie", out var cookieHeader))
            {
                request.Headers.Add("Cookie", (IEnumerable<string>)cookieHeader);
            }
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Activity = await response.Content.ReadFromJsonAsync<ActivityDetailDto>();
            }
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? string.Empty;
            
            if (Activity == null) return NotFound();
            
            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            var client = _http.CreateClient("ApiClient");
            var res = await client.PostAsync($"api/activity/{Id}/register", null);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrWhiteSpace(msg) ? "Registration failed" : msg;
            }
            else
            {
                TempData["Success"] = "Activity registered";
            }
            return RedirectToPage(new { id = Id });
        }
    }
}

