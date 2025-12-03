using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.JoinRequest;

namespace WebFE.Pages.ClubManager
{
    public class RequestsModel : ClubManagerPageModel
    {
        private readonly ILogger<RequestsModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RequestsModel(ILogger<RequestsModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public bool IsRecruitmentOpen { get; set; }
        public List<JoinRequestDto> PendingRequests { get; set; } = new();
        public List<JoinRequestDto> AllRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialize club context from TempData
            var result = await InitializeClubContextAsync();
            if (result is RedirectResult)
            {
                return result;
            }

            try
            {

                using var httpClient = CreateHttpClient();

                // Get club details
                var clubResponse = await httpClient.GetAsync($"/api/club/{ClubId}");
                if (!clubResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get club details");
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

                IsRecruitmentOpen = club.IsRecruitmentOpen;

                // Get all join requests
                var allRequestsResponse = await httpClient.GetAsync($"/api/joinrequest/club/{ClubId}");
                if (allRequestsResponse.IsSuccessStatusCode)
                {
                    var allRequestsContent = await allRequestsResponse.Content.ReadAsStringAsync();
                    var allRequests = JsonSerializer.Deserialize<List<JoinRequestDto>>(allRequestsContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    AllRequests = allRequests ?? new List<JoinRequestDto>();
                }

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

