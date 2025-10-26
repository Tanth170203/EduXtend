using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebFE.Pages.Admin.Attendances
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
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Get all activities (only approved for Admin view)
            Activities = await client.GetFromJsonAsync<List<ActivityListItemDto>>("api/admin/activities?status=Approved") ?? new();
            // Admin attendances should not include Club activities
            Activities = Activities.Where(a => a.ClubId == null).ToList();

            return Page();
        }
    }
}

