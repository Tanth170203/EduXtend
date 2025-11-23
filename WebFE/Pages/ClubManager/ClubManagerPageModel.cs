using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;

namespace WebFE.Pages.ClubManager
{
    /// <summary>
    /// Base page model for all ClubManager pages
    /// Provides common functionality like getting ClubId from TempData
    /// </summary>
    public abstract class ClubManagerPageModel : PageModel
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;

        protected async Task<IActionResult> InitializeClubContextAsync()
        {
            // Check authentication
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Redirect("/Auth/Login");
            }

            // Get ClubId from TempData (set by Index page)
            if (TempData["SelectedClubId"] != null)
            {
                ClubId = (int)TempData["SelectedClubId"];
                TempData.Keep("SelectedClubId"); // Keep for next request
            }
            else
            {
                // If no ClubId in TempData, redirect to dashboard to select club
                return Redirect("/ClubManager");
            }

            // Optionally load club name
            await LoadClubNameAsync();

            return Page();
        }

        protected virtual async Task LoadClubNameAsync()
        {
            // Override in derived classes if needed
            // For now, just set a placeholder
            ClubName = $"Club {ClubId}";
            await Task.CompletedTask;
        }

        protected HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
