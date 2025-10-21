using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;
using BusinessObject.DTOs.MovementRecord;
using BusinessObject.DTOs.Evidence;

namespace WebFE.Pages.Admin.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // Dashboard Statistics
        public int TotalStudents { get; set; }
        public int ActiveClubs { get; set; }
        public int MonthlyActivities { get; set; }
        public int PendingProposals { get; set; }
        
        // Movement Evaluation Statistics
        public int PendingEvidences { get; set; }
        public int TotalMovementRecords { get; set; }
        public double AverageMovementScore { get; set; }
        public List<MovementRecordDto> TopScorers { get; set; } = new();

        // Recent Activities
        public List<RecentActivityDto> RecentActivities { get; set; } = new();

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
                // TODO: Load actual data from API
                // For now, using placeholder data for some stats
                TotalStudents = 1250;
                ActiveClubs = 24;
                MonthlyActivities = 45;
                PendingProposals = 8;

                // Load Movement Evaluation statistics from API
                await LoadMovementStatisticsAsync();

                // Load recent activities (placeholder)
                RecentActivities = new List<RecentActivityDto>
                {
                    new RecentActivityDto
                    {
                        Name = "AI Technology Workshop 2025",
                        ClubName = "Programming Club",
                        StartDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new RecentActivityDto
                    {
                        Name = "Student Football Tournament",
                        ClubName = "Sports Club",
                        StartDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new RecentActivityDto
                    {
                        Name = "UI/UX Design Workshop",
                        ClubName = "Design Club",
                        StartDate = DateTime.UtcNow.AddDays(-7)
                    }
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return Page();
            }
        }

        private async Task LoadMovementStatisticsAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Get pending evidences count
                var evidenceResponse = await httpClient.GetAsync("/api/evidences/stats/pending-count");
                if (evidenceResponse.IsSuccessStatusCode)
                {
                    var content = await evidenceResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, int>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    PendingEvidences = result?["count"] ?? 0;
                }

                // Get all movement records for statistics
                var recordsResponse = await httpClient.GetAsync("/api/movement-records");
                if (recordsResponse.IsSuccessStatusCode)
                {
                    var content = await recordsResponse.Content.ReadAsStringAsync();
                    var records = JsonSerializer.Deserialize<List<MovementRecordDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (records != null && records.Any())
                    {
                        TotalMovementRecords = records.Count;
                        AverageMovementScore = records.Average(r => r.TotalScore);
                        TopScorers = records.OrderByDescending(r => r.TotalScore).Take(5).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movement statistics");
                // Set default values
                PendingEvidences = 0;
                TotalMovementRecords = 0;
                AverageMovementScore = 0;
            }
        }
    }

    public class RecentActivityDto
    {
        public string Name { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
    }
}