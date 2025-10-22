using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;
using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;

namespace WebFE.Pages.ClubManager
{
    public class RequestsModel : PageModel
    {
        private readonly ILogger<RequestsModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RequestsModel(ILogger<RequestsModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public bool IsRecruitmentOpen { get; set; }
        public List<JoinRequestDto> PendingRequests { get; set; } = new();

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

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Get managed club
                var clubResponse = await httpClient.GetAsync("/api/club/my-managed-club");
                if (!clubResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get managed club");
                    return Redirect("/ClubManager");
                }

                var clubContent = await clubResponse.Content.ReadAsStringAsync();
                var club = JsonSerializer.Deserialize<ClubDetailDto>(clubContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (club == null)
                {
                    return Redirect("/ClubManager");
                }

                ClubId = club.Id;
                ClubName = club.Name;
                IsRecruitmentOpen = club.IsRecruitmentOpen;

                // Get pending join requests
                var requestsResponse = await httpClient.GetAsync($"/api/joinrequest/club/{ClubId}/pending");
                if (requestsResponse.IsSuccessStatusCode)
                {
                    var requestsContent = await requestsResponse.Content.ReadAsStringAsync();
                    var requests = JsonSerializer.Deserialize<List<JoinRequestDto>>(requestsContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    PendingRequests = requests ?? new List<JoinRequestDto>();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading requests page");
                return Redirect("/ClubManager");
            }
        }
    }
}

