using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebFE.Pages.Admin.ClubNews
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            try
            {
                // Get user info from JWT token
                var token = Request.Cookies["AccessToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return Redirect("/Auth/Login");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                
                // Get user roles
                var roles = jwt.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                    .Select(c => c.Value)
                    .ToList();

                // Check if user is Admin
                if (!roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User without Admin role attempted to access Club News management");
                    return Redirect("/Error?code=403");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club news management page");
                return Redirect("/Auth/Login");
            }
        }
    }
}
