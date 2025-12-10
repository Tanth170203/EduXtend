using Microsoft.AspNetCore.Mvc;

namespace WebFE.Pages.ClubManager.Financial
{
    public class DashboardModel : ClubManagerPageModel
    {
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ILogger<DashboardModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return await InitializeClubContextAsync();
        }
    }
}

