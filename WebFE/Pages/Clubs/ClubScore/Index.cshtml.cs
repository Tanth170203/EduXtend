using BusinessObject.DTOs.ClubMovementRecord;
using BusinessObject.DTOs.Club;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace WebFE.Pages.Clubs.ClubScore
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public ClubMovementSummaryDto? Summary { get; set; }
        public List<SemesterSummary> SemesterSummaries { get; set; } = new();
        public int? CurrentSemesterId { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public List<ClubListItemDto> MyClubs { get; set; } = new();
        public int SelectedClubId { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class SemesterSummary
        {
            public int SemesterId { get; set; }
            public string SemesterName { get; set; } = string.Empty;
            public double TotalScore { get; set; }
            public int TotalCriteria { get; set; }
            public int MonthCount { get; set; }
            public DateTime? LastUpdated { get; set; }
            public DateTime CreatedAt { get; set; }
        }

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

        public async Task<IActionResult> OnGetAsync(int? clubId)
        {
            try
            {
                MyClubs = await GetUserClubsAsync();
                
                if (!MyClubs.Any())
                {
                    ErrorMessage = "You are not a member of any club. Please join a club to view club movement scores.";
                    return Page();
                }

                if (clubId.HasValue && MyClubs.Any(c => c.Id == clubId.Value))
                {
                    ClubId = clubId.Value;
                }
                else
                {
                    ClubId = MyClubs.First().Id;
                }
                
                SelectedClubId = ClubId;
                ClubName = MyClubs.FirstOrDefault(c => c.Id == ClubId)?.Name ?? string.Empty;

                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/club-movement-records/club/{ClubId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var records = JsonSerializer.Deserialize<List<ClubMovementRecordDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (records != null && records.Any())
                    {
                        var latestRecord = records.OrderByDescending(r => r.LastUpdated ?? r.CreatedAt).FirstOrDefault();
                        CurrentSemesterId = latestRecord?.SemesterId;

                        SemesterSummaries = records
                            .GroupBy(r => new { r.SemesterId, r.SemesterName })
                            .Select(g => new SemesterSummary
                            {
                                SemesterId = g.Key.SemesterId,
                                SemesterName = g.Key.SemesterName,
                                TotalScore = g.Sum(r => r.TotalScore),
                                TotalCriteria = g.Sum(r => r.Details.Count),
                                MonthCount = g.Count(),
                                LastUpdated = g.Max(r => r.LastUpdated ?? r.CreatedAt),
                                CreatedAt = g.Min(r => r.CreatedAt)
                            })
                            .OrderByDescending(s => s.SemesterId)
                            .ToList();

                        Summary = new ClubMovementSummaryDto
                        {
                            ClubId = ClubId,
                            ClubName = records.First().ClubName,
                            TotalRecords = SemesterSummaries.Count,
                            AverageScore = SemesterSummaries.Any() ? SemesterSummaries.Average(s => s.TotalScore) : 0,
                            HighestScore = SemesterSummaries.Any() ? SemesterSummaries.Max(s => s.TotalScore) : 0,
                            LowestScore = SemesterSummaries.Any() ? SemesterSummaries.Min(s => s.TotalScore) : 0,
                            Records = records
                        };
                    }
                    else
                    {
                        Summary = new ClubMovementSummaryDto
                        {
                            ClubId = ClubId,
                            ClubName = ClubName,
                            TotalRecords = 0,
                            AverageScore = 0,
                            HighestScore = 0,
                            LowestScore = 0,
                            Records = new List<ClubMovementRecordDto>()
                        };
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Summary = new ClubMovementSummaryDto
                    {
                        ClubId = ClubId,
                        ClubName = ClubName,
                        TotalRecords = 0,
                        AverageScore = 0,
                        HighestScore = 0,
                        LowestScore = 0,
                        Records = new List<ClubMovementRecordDto>()
                    };
                }
                else
                {
                    ErrorMessage = "Unable to load club movement scores.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club movement records");
                ErrorMessage = "Error loading data.";
            }

            return Page();
        }

        private async Task<List<ClubListItemDto>> GetUserClubsAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/club/my-clubs");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var clubs = JsonSerializer.Deserialize<List<ClubListItemDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return clubs ?? new List<ClubListItemDto>();
                }
                return new List<ClubListItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user clubs");
                return new List<ClubListItemDto>();
            }
        }
    }
}
