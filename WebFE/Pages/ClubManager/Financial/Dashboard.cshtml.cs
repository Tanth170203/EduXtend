using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Headers;

namespace WebFE.Pages.ClubManager.Financial
{
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ILogger<DashboardModel> logger)
        {
            _logger = logger;
        }

        public int ClubId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get club ID from session or query parameter
                ClubId = GetClubIdFromSession();
                if (ClubId == 0)
                {
                    TempData["ErrorMessage"] = "Club information not found";
                    return RedirectToPage("/ClubManager/Dashboard");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading financial dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard";
                return Page();
            }
        }

        private int GetClubIdFromSession()
        {
            // Try to get from query
            if (Request.Query.ContainsKey("clubId") && int.TryParse(Request.Query["clubId"], out var clubId))
            {
                return clubId;
            }

            // Default fallback - in production, this should come from user's managed club
            return 1; // Temporary default
        }
    }
}

