using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager
{
    public class NewsModel : PageModel
    {
        private readonly ILogger<NewsModel> _logger;

        public NewsModel(ILogger<NewsModel> logger)
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

                // Check if user is ClubManager
                if (!roles.Contains("ClubManager", StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User without ClubManager role attempted to access Club News page");
                    return Redirect("/Error?code=403");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club news page");
                return Redirect("/Auth/Login");
            }
        }
    }
}
