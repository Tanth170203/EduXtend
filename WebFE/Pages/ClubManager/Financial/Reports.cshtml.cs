using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.ClubManager.Financial
{
    public class ReportsModel : PageModel
    {
        public int ClubId { get; set; }

        public void OnGet()
        {
            // Get Club ID from user claims or session
            var clubIdClaim = User.FindFirst("ClubId")?.Value;
            if (!string.IsNullOrEmpty(clubIdClaim) && int.TryParse(clubIdClaim, out int clubId))
            {
                ClubId = clubId;
            }
            else
            {
                ClubId = 1; // Default for testing
            }
        }
    }
}

