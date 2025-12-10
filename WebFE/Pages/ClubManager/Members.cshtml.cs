using Microsoft.AspNetCore.Mvc;

namespace WebFE.Pages.ClubManager
{
    public class MembersModel : ClubManagerPageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            return await InitializeClubContextAsync();
        }
    }
}
