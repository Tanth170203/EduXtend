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
            ClubId = clubId;

            try
            {
                using var httpClient = CreateHttpClient();

                // Get club details
                var clubResponse = await httpClient.GetAsync($"/api/club/{clubId}");
                if (!clubResponse.IsSuccessStatusCode)
                {
                    return RedirectToPage("/Clubs/Index");
                }

                var clubContent = await clubResponse.Content.ReadAsStringAsync();
                var club = JsonSerializer.Deserialize<BusinessObject.DTOs.Club.ClubDetailDto>(clubContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (club == null)
                {
                    return RedirectToPage("/Clubs/Index");
                }

                ClubName = club.Name;
                ClubLogoUrl = club.LogoUrl;

                // Get recruitment status
                var statusResponse = await httpClient.GetAsync($"/api/club/{clubId}/recruitment-status");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<BusinessObject.DTOs.Club.RecruitmentStatusDto>(statusContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    IsRecruitmentOpen = status?.IsRecruitmentOpen ?? false;
                }

                // Check if user has existing request
                var requestResponse = await httpClient.GetAsync($"/api/joinrequest/my-request/{clubId}");
                if (requestResponse.IsSuccessStatusCode)
                {
                    var requestContent = await requestResponse.Content.ReadAsStringAsync();
                    ExistingRequest = JsonSerializer.Deserialize<JoinRequestDto>(requestContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    CanApply = false; // Has existing request
                }
                else
                {
                    // Check if user can apply
                    var canApplyResponse = await httpClient.GetAsync($"/api/joinrequest/can-apply/{clubId}");
                    if (canApplyResponse.IsSuccessStatusCode)
                    {
                        var canApplyContent = await canApplyResponse.Content.ReadAsStringAsync();
                        var canApplyResult = JsonSerializer.Deserialize<Dictionary<string, bool>>(canApplyContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        CanApply = canApplyResult?["canApply"] ?? false;
                    }
                }

                // Get departments
                var deptResponse = await httpClient.GetAsync($"/api/joinrequest/club/{clubId}/departments");
                if (deptResponse.IsSuccessStatusCode)
                {
                    var deptContent = await deptResponse.Content.ReadAsStringAsync();
                    var departments = JsonSerializer.Deserialize<List<DepartmentDto>>(deptContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Departments = departments ?? new List<DepartmentDto>();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading apply page for club {ClubId}", clubId);
                return RedirectToPage("/Clubs/Index");
            }
        }
    }
}

