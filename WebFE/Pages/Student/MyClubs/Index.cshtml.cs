using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.Student.MyClubs
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public IndexModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        public List<MyClubItemDto> Items { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Items = await client.GetFromJsonAsync<List<MyClubItemDto>>("api/club/my") ?? new();
            return Page();
        }

        public async Task<IActionResult> OnPostLeaveAsync(int clubId)
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");
            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PostAsync($"api/club/{clubId}/leave", null);
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể rời CLB";
            }
            return RedirectToPage();
        }
    }
}


