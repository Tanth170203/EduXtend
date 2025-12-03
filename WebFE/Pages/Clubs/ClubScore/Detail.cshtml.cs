using BusinessObject.DTOs.ClubMovementRecord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace WebFE.Pages.Clubs.ClubScore
{
    public class DetailModel : PageModel
    {
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(ILogger<DetailModel> logger)
        {
            _logger = logger;
        }

        public List<ClubMovementRecordDto> Records { get; set; } = new();
        public string SemesterName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public string PresidentName { get; set; } = string.Empty;
        public string PresidentCode { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public int TotalCriteria { get; set; }
        public bool IsCurrentSemester { get; set; }
        public int ClubId { get; set; }
        public int SemesterId { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int clubId, int semesterId)
        {
            ClubId = clubId;
            SemesterId = semesterId;

            try
            {
                using var httpClient = CreateHttpClient();

                // Verify membership before loading data
                var isMember = await IsUserMemberOfClubAsync(httpClient, clubId);
                if (!isMember)
                {
                    ErrorMessage = "You are not authorized to view scores for this club. You must be an active member.";
                    return RedirectToPage("./Index");
                }

                var response = await httpClient.GetAsync($"/api/club-movement-records/club/{clubId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allRecords = JsonSerializer.Deserialize<List<ClubMovementRecordDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (allRecords != null)
                    {
                        Records = allRecords.Where(r => r.SemesterId == semesterId).OrderBy(r => r.Month).ToList();

                        if (Records.Any())
                        {
                            var firstRecord = Records.First();
                            SemesterName = firstRecord.SemesterName;
                            ClubName = firstRecord.ClubName;
                            PresidentName = firstRecord.PresidentName;
                            PresidentCode = firstRecord.PresidentCode;
                            TotalScore = Records.Sum(r => r.TotalScore);
                            TotalCriteria = Records.Sum(r => r.Details.Count);

                            var latestOverallRecord = allRecords.OrderByDescending(r => r.LastUpdated ?? r.CreatedAt).FirstOrDefault();
                            IsCurrentSemester = latestOverallRecord?.SemesterId == semesterId;
                        }
                        else
                        {
                            ErrorMessage = "No records found for this semester.";
                            return RedirectToPage("./Index");
                        }
                    }
                }
                else
                {
                    ErrorMessage = "Unable to load club movement records.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club movement records");
                ErrorMessage = "Error loading data.";
                return RedirectToPage("./Index");
            }

            return Page();
        }

        private async Task<bool> IsUserMemberOfClubAsync(HttpClient httpClient, int clubId)
        {
            try
            {
                var response = await httpClient.GetAsync($"/api/club/{clubId}/is-member");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MembershipCheckResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result?.IsMember ?? false;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking membership for club {ClubId}", clubId);
                return false;
            }
        }

        private class MembershipCheckResponse
        {
            public bool IsMember { get; set; }
        }
    }
}
