using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Headers;

namespace WebFE.Pages.ClubManager.Financial
{
    public class MemberFundsModel : PageModel
    {
        private readonly ILogger<MemberFundsModel> _logger;
        private readonly IConfiguration _configuration;

        public MemberFundsModel(ILogger<MemberFundsModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public int? RequestId { get; set; }

        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;

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

                // Fetch club name
                using var client = CreateHttpClient();
                var clubResponse = await client.GetAsync($"api/club/{ClubId}");
                if (clubResponse.IsSuccessStatusCode)
                {
                    var club = await clubResponse.Content.ReadFromJsonAsync<ClubDto>();
                    ClubName = club?.Name ?? "Club";
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading member funds page");
                TempData["ErrorMessage"] = "An error occurred while loading the page";
                return Page();
            }
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var accessToken = Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                handler.CookieContainer.Add(
                    new Uri("https://localhost:5001"),
                    new Cookie("AccessToken", accessToken)
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            return client;
        }

        private int GetClubIdFromSession()
        {
            // Try to get from session or query
            if (Request.Query.ContainsKey("clubId") && int.TryParse(Request.Query["clubId"], out var clubId))
            {
                return clubId;
            }

            // Default fallback - in production, this should come from user's managed club
            return 1; // Temporary default
        }

        private class ClubDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}

