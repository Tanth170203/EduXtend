using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.GpsAttendance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class GpsConfigModel : ClubManagerPageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ActivityDetailDto? Activity { get; private set; }
        
        [BindProperty]
        public ActivityGpsConfigDto GpsConfig { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var client = CreateHttpClient();

            // Get activity details
            var activityResponse = await client.GetAsync($"api/activity/{Id}");
            if (activityResponse.IsSuccessStatusCode)
            {
                Activity = await activityResponse.Content.ReadFromJsonAsync<ActivityDetailDto>();
            }

            if (Activity == null) return NotFound();

            // Get GPS config
            var configResponse = await client.GetAsync($"api/gpsconfig/{Id}");
            if (configResponse.IsSuccessStatusCode)
            {
                var config = await configResponse.Content.ReadFromJsonAsync<ActivityGpsConfigDto>();
                if (config != null)
                {
                    GpsConfig = config;
                }
            }

            GpsConfig.ActivityId = Id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = CreateHttpClient();

            GpsConfig.ActivityId = Id;

            var response = await client.PostAsJsonAsync($"api/gpsconfig/{Id}", GpsConfig);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "GPS configuration saved successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = string.IsNullOrEmpty(error) ? "Error saving GPS configuration" : error;
            }

            return RedirectToPage(new { id = Id });
        }
    }
}
