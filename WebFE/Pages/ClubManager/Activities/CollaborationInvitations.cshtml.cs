using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages.ClubManager.Activities
{
    public class CollaborationInvitationsModel : PageModel
    {
        private readonly IConfiguration _config;

        public CollaborationInvitationsModel(IConfiguration config)
        {
            _config = config;
        }

        public string ApiBaseUrl { get; set; } = string.Empty;

        public void OnGet()
        {
            ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
        }
    }
}
