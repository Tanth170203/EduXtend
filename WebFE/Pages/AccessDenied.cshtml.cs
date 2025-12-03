using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebFE.Pages
{
    public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogWarning("Access denied for user {User} attempting to access {Path}",
                User.Identity?.Name ?? "Anonymous",
                HttpContext.Request.Headers["Referer"].ToString());
        }
    }
}
