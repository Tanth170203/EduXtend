using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.Activities
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _http;
        public DetailsModel(IHttpClientFactory http) => _http = http;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ActivityDetailDto? Activity { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _http.CreateClient("ApiClient");
            Activity = await client.GetFromJsonAsync<ActivityDetailDto>($"api/activity/{Id}");
            
            if (Activity == null) return NotFound();
            
            return Page();
        }
    }
}



