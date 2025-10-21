using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace WebFE.Pages.Admin.Activities
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IHttpClientFactory httpClientFactory, ILogger<DetailsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        public ActivityDetailDto? Activity { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                Activity = await client.GetFromJsonAsync<ActivityDetailDto>($"/api/admin/activities/{Id}");
                if (Activity == null) return NotFound();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity details");
                return NotFound();
            }
        }
    }
}

