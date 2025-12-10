using Microsoft.AspNetCore.Mvc;

namespace WebFE.Pages.ClubManager.Financial
{
    public class ReportsModel : ClubManagerPageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            return await InitializeClubContextAsync();
        }
    }
}

