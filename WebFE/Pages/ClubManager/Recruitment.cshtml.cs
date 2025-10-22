using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;
using BusinessObject.DTOs.Club;

namespace WebFE.Pages.ClubManager
{
    public class RecruitmentModel : PageModel
    {
        private readonly ILogger<RecruitmentModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RecruitmentModel(ILogger<RecruitmentModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public bool IsRecruitmentOpen { get; set; }
        public int PendingRequestCount { get; set; }

        private HttpClient CreateHttpClient()
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

        public IActionResult OnGet()
        {
            // Redirect to Requests page (consolidated recruitment + requests management)
            return RedirectToPage("/ClubManager/Requests");
        }
    }
}

