using BusinessObject.DTOs.ClubMovementRecord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.ClubMovementScore
{
    public class IndexModel : ClubManagerPageModel
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
                _logger.LogInformation("Loading club movement records for ClubId: {ClubId}", ClubId);
                
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
                        // Determine current semester based on latest updated record
                        var latestRecord = records
                            .OrderByDescending(r => r.LastUpdated ?? r.CreatedAt)
                            .FirstOrDefault();
                        CurrentSemesterId = latestRecord?.SemesterId;

                        // Group records by semester
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

                        // Calculate summary stats from all records
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
                        CurrentSemesterId = null;
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
                        SemesterSummaries = new List<SemesterSummary>();
                    }
                    _logger.LogInformation("✅ Loaded {Count} club movement records for ClubId: {ClubId}, grouped into {SemesterCount} semesters", records?.Count ?? 0, ClubId, SemesterSummaries.Count);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    CurrentSemesterId = null;
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
                    _logger.LogInformation("No club movement records found for ClubId: {ClubId}", ClubId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    CurrentSemesterId = null;
                    SemesterSummaries = new List<SemesterSummary>();
                    _logger.LogError("Failed to load club movement records: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Unable to load club movement scores.";
                }
            }
            catch (Exception ex)
            {
                CurrentSemesterId = null;
                SemesterSummaries = new List<SemesterSummary>();
                _logger.LogError(ex, "Error loading club movement records");
                ErrorMessage = "Error loading data.";
            }

            return Page();
        }
    }
}
