using BusinessObject.DTOs.Activity;
using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.ClubMovementRecord;
using BusinessObject.DTOs.JoinRequest;
using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebFE.Pages.Clubs
{
    public class MemberDashboardModel : PageModel
    {
        private readonly ILogger<MemberDashboardModel> _logger;

        public MemberDashboardModel(ILogger<MemberDashboardModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Section { get; set; } = "overview";

        [BindProperty(SupportsGet = true)]
        public int? SemesterId { get; set; }

        [BindProperty(SupportsGet = true, Name = "page")]
        public int Page { get; set; } = 1;

        public ClubDetailDto? Club { get; private set; }
        public List<ClubMemberDto> Members { get; private set; } = new();
        public List<DepartmentDto> Departments { get; private set; } = new();
        public List<ActivityListItemDto> Activities { get; private set; } = new();
        public List<ClubAwardDto> Awards { get; private set; } = new();
        
        // Pagination properties for activities
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalPages { get; set; }
        public int TotalActivities { get; set; }
        public List<ProposalDTO> Proposals { get; private set; } = new();

        // Club Score properties
        public ClubMovementSummaryDto? ScoreSummary { get; private set; }
        public List<SemesterScoreSummary> SemesterSummaries { get; private set; } = new();
        public int? CurrentSemesterId { get; private set; }

        // Club Score Detail properties
        public List<ClubMovementRecordDto> ScoreDetailRecords { get; private set; } = new();
        public string DetailSemesterName { get; private set; } = string.Empty;
        public string DetailPresidentName { get; private set; } = string.Empty;
        public string DetailPresidentCode { get; private set; } = string.Empty;
        public double DetailTotalScore { get; private set; }
        public int DetailTotalCriteria { get; private set; }
        public bool IsDetailCurrentSemester { get; private set; }

        public class SemesterScoreSummary
        {
            public int SemesterId { get; set; }
            public string SemesterName { get; set; } = string.Empty;
            public double TotalScore { get; set; }
            public int TotalCriteria { get; set; }
            public int MonthCount { get; set; }
            public DateTime? LastUpdated { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check authentication
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["ErrorMessage"] = "Please login to access member dashboard";
                return RedirectToPage("/Auth/Login");
            }

            if (Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid club ID";
                return RedirectToPage("/Clubs/Active");
            }

            try
            {
                // Create HttpClient with cookie forwarding
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var accessToken = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    handler.CookieContainer.Add(
                        new Uri("https://localhost:5001"),
                        new Cookie("AccessToken", accessToken)
                    );
                }

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                // Check if user is member
                var memberResponse = await client.GetFromJsonAsync<Dictionary<string, bool>>($"api/club/{Id}/is-member");
                var isMember = memberResponse?["isMember"] ?? false;

                if (!isMember)
                {
                    TempData["ErrorMessage"] = "You must be a member to access this dashboard";
                    return RedirectToPage("/Clubs/Details", new { id = Id });
                }

                // Fetch club details
                Club = await client.GetFromJsonAsync<ClubDetailDto>($"api/club/{Id}");
                if (Club == null) return NotFound();

                // Fetch all data
                Members = await client.GetFromJsonAsync<List<ClubMemberDto>>($"api/club/{Id}/members") ?? new();
                Departments = await client.GetFromJsonAsync<List<DepartmentDto>>($"api/club/{Id}/departments") ?? new();
                
                // Fetch activities with pagination
                var allActivities = await client.GetFromJsonAsync<List<ActivityListItemDto>>($"api/activity/club/{Id}") ?? new();
                
                // Read page from query string as fallback
                if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out int pageFromQuery))
                {
                    Page = pageFromQuery;
                }
                
                // Apply pagination to activities
                CurrentPage = Page > 0 ? Page : 1;
                TotalActivities = allActivities.Count;
                TotalPages = (int)Math.Ceiling(TotalActivities / (double)PageSize);
                
                if (CurrentPage > TotalPages && TotalPages > 0)
                    CurrentPage = TotalPages;
                
                Activities = allActivities
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
                
                Awards = await client.GetFromJsonAsync<List<ClubAwardDto>>($"api/club/{Id}/awards") ?? new();
                Proposals = await client.GetFromJsonAsync<List<ProposalDTO>>($"api/proposal/club/{Id}") ?? new();

                // Fetch club score data if on score section
                if (Section == "score")
                {
                    await LoadClubScoreDataAsync(client);
                }

                // Fetch club score detail data if on score-detail section
                if (Section == "score-detail" && SemesterId.HasValue)
                {
                    await LoadClubScoreDetailAsync(client, SemesterId.Value);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading member dashboard for club {ClubId}", Id);
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard";
                return RedirectToPage("/Clubs/Details", new { id = Id });
            }
        }

        private async Task LoadClubScoreDataAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync($"/api/club-movement-records/club/{Id}");

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
                            .Select(g => new SemesterScoreSummary
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

                        ScoreSummary = new ClubMovementSummaryDto
                        {
                            ClubId = Id,
                            ClubName = records.First().ClubName,
                            TotalRecords = SemesterSummaries.Count,
                            AverageScore = SemesterSummaries.Any() ? SemesterSummaries.Average(s => s.TotalScore) : 0,
                            HighestScore = SemesterSummaries.Any() ? SemesterSummaries.Max(s => s.TotalScore) : 0,
                            LowestScore = SemesterSummaries.Any() ? SemesterSummaries.Min(s => s.TotalScore) : 0,
                            Records = records
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club score data for club {ClubId}", Id);
            }
        }

        private async Task LoadClubScoreDetailAsync(HttpClient client, int semesterId)
        {
            try
            {
                var response = await client.GetAsync($"/api/club-movement-records/club/{Id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allRecords = JsonSerializer.Deserialize<List<ClubMovementRecordDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (allRecords != null)
                    {
                        ScoreDetailRecords = allRecords.Where(r => r.SemesterId == semesterId).OrderBy(r => r.Month).ToList();

                        if (ScoreDetailRecords.Any())
                        {
                            var firstRecord = ScoreDetailRecords.First();
                            DetailSemesterName = firstRecord.SemesterName;
                            DetailPresidentName = firstRecord.PresidentName;
                            DetailPresidentCode = firstRecord.PresidentCode;
                            DetailTotalScore = ScoreDetailRecords.Sum(r => r.TotalScore);
                            DetailTotalCriteria = ScoreDetailRecords.Sum(r => r.Details.Count);

                            var latestOverallRecord = allRecords.OrderByDescending(r => r.LastUpdated ?? r.CreatedAt).FirstOrDefault();
                            IsDetailCurrentSemester = latestOverallRecord?.SemesterId == semesterId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club score detail for club {ClubId}, semester {SemesterId}", Id, semesterId);
            }
        }
    }
}

