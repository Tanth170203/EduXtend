using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.Financial
{
    public class TransactionsModel : PageModel
    {
        public int ClubId { get; set; }

        public async Task OnGetAsync()
        {
            // Get Club ID from user claims or session
            var clubIdClaim = User.FindFirst("ClubId")?.Value;
            if (!string.IsNullOrEmpty(clubIdClaim) && int.TryParse(clubIdClaim, out int clubId))
            {
                ClubId = clubId;
            }
            else
            {
                // Fallback: Try to get from query or default
                ClubId = 1; // Default club for testing
            }
        }
    }
}

