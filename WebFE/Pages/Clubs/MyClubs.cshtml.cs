using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Json;

namespace WebFE.Pages.Clubs
{
    public class MyClubsModel : PageModel
    {
        private readonly ILogger<MyClubsModel> _logger;

        public MyClubsModel(ILogger<MyClubsModel> logger)
        {
            _logger = logger;
        }

        public List<ClubListItemDto> MyClubs { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Please login to view your clubs";
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                // Create HttpClient with cookie forwarding
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

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                // Fetch my clubs
                MyClubs = await client.GetFromJsonAsync<List<ClubListItemDto>>("api/club/my-clubs") ?? new();

                _logger.LogInformation("Successfully loaded {Count} clubs for user", MyClubs.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my clubs");
                TempData["ErrorMessage"] = "An error occurred while loading your clubs";
                return Page();
            }
        }

        public string Img(string? url, string fallback) =>
            string.IsNullOrWhiteSpace(url)
                ? Url.Content(fallback)
                : (Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url :
                    (url!.StartsWith("/") ? url : "/" + url));
    }
}

