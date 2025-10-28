using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;
using BusinessObject.DTOs.JoinRequest;

namespace WebFE.Pages.Clubs
{
    public class ApplyModel : PageModel
    {
        private readonly ILogger<ApplyModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ApplyModel(ILogger<ApplyModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string? ClubLogoUrl { get; set; }
        public bool IsRecruitmentOpen { get; set; }
        public bool CanApply { get; set; }
        public List<DepartmentDto> Departments { get; set; } = new();
        public JoinRequestDto? ExistingRequest { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int clubId)
        {
            // Validate clubId
            if (clubId <= 0)
            {
                _logger.LogWarning("Invalid clubId: {ClubId}", clubId);
                TempData["ErrorMessage"] = "Invalid club ID";
                return RedirectToPage("/Clubs/Active");
            }

            ClubId = clubId;

            try
            {
                using var httpClient = CreateHttpClient();

                // Get club details
                _logger.LogInformation("Fetching club details for club {ClubId}", clubId);
                var clubResponse = await httpClient.GetAsync($"/api/club/{clubId}");
                if (!clubResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch club details. Status: {StatusCode}", clubResponse.StatusCode);
                    return RedirectToPage("/Clubs/Index");
                }

                var clubContent = await clubResponse.Content.ReadAsStringAsync();
                var club = JsonSerializer.Deserialize<BusinessObject.DTOs.Club.ClubDetailDto>(clubContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (club == null)
                {
                    _logger.LogWarning("Club data is null for club {ClubId}", clubId);
                    return RedirectToPage("/Clubs/Index");
                }

                ClubName = club.Name;
                ClubLogoUrl = club.LogoUrl;
                _logger.LogInformation("Club details loaded: {ClubName}", ClubName);

                // Get recruitment status
                _logger.LogInformation("Fetching recruitment status for club {ClubId}", clubId);
                var statusResponse = await httpClient.GetAsync($"/api/club/{clubId}/recruitment-status");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<BusinessObject.DTOs.Club.RecruitmentStatusDto>(statusContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    IsRecruitmentOpen = status?.IsRecruitmentOpen ?? false;
                    _logger.LogInformation("Recruitment status: {IsOpen}", IsRecruitmentOpen);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch recruitment status. Status: {StatusCode}", statusResponse.StatusCode);
                    IsRecruitmentOpen = false;
                }

                // Check if user has existing request
                _logger.LogInformation("Checking for existing request for club {ClubId}", clubId);
                var requestResponse = await httpClient.GetAsync($"/api/joinrequest/my-request/{clubId}");
                if (requestResponse.IsSuccessStatusCode)
                {
                    var requestContent = await requestResponse.Content.ReadAsStringAsync();
                    ExistingRequest = JsonSerializer.Deserialize<JoinRequestDto>(requestContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    CanApply = false; // Has existing request
                    _logger.LogInformation("User has existing request: {Status}", ExistingRequest?.Status);
                }
                else if (requestResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No existing request found");
                    // No existing request, check if user can apply
                    var canApplyResponse = await httpClient.GetAsync($"/api/joinrequest/can-apply/{clubId}");
                    if (canApplyResponse.IsSuccessStatusCode)
                    {
                        var canApplyContent = await canApplyResponse.Content.ReadAsStringAsync();
                        var canApplyResult = JsonSerializer.Deserialize<Dictionary<string, bool>>(canApplyContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        CanApply = canApplyResult?["canApply"] ?? false;
                        _logger.LogInformation("CanApply result: {CanApply}", CanApply);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to check can-apply status. Status: {StatusCode}", canApplyResponse.StatusCode);
                        // If recruitment is open and no existing request, allow apply
                        CanApply = IsRecruitmentOpen;
                    }
                }
                else
                {
                    _logger.LogWarning("Error checking existing request. Status: {StatusCode}", requestResponse.StatusCode);
                    CanApply = IsRecruitmentOpen;
                }

                // Get departments
                _logger.LogInformation("Fetching departments for club {ClubId}", clubId);
                var deptResponse = await httpClient.GetAsync($"/api/joinrequest/club/{clubId}/departments");
                if (deptResponse.IsSuccessStatusCode)
                {
                    var deptContent = await deptResponse.Content.ReadAsStringAsync();
                    var departments = JsonSerializer.Deserialize<List<DepartmentDto>>(deptContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Departments = departments ?? new List<DepartmentDto>();
                    _logger.LogInformation("Loaded {Count} departments", Departments.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch departments. Status: {StatusCode}", deptResponse.StatusCode);
                    Departments = new List<DepartmentDto>();
                }

                _logger.LogInformation("Apply page loaded. CanApply: {CanApply}, IsRecruitmentOpen: {IsOpen}, HasExistingRequest: {HasRequest}", 
                    CanApply, IsRecruitmentOpen, ExistingRequest != null);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading apply page for club {ClubId}", clubId);
                TempData["ErrorMessage"] = "An error occurred while loading the application page. Please try again later.";
                return RedirectToPage("/Clubs/Index");
            }
        }
    }
}

