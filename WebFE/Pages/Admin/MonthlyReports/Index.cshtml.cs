using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.Admin.MonthlyReports
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

        public List<MonthlyReportListDto> Reports { get; set; } = new();
        public List<ClubDto> Clubs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? FilterClubId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");

                // Get all clubs for filter dropdown
                var clubsRequest = new HttpRequestMessage(HttpMethod.Get, "api/club");
                foreach (var cookie in Request.Cookies)
                {
                    clubsRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var clubsResponse = await client.SendAsync(clubsRequest);
                if (clubsResponse.IsSuccessStatusCode)
                {
                    Clubs = await clubsResponse.Content.ReadFromJsonAsync<List<ClubDto>>() ?? new();
                }

                // Get all monthly reports (no clubId parameter for admin)
                var request = new HttpRequestMessage(HttpMethod.Get, "api/monthly-reports");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ReportsResponse>();
                    var allReports = result?.Data ?? new();
                    
                    // Apply filters - Admin should only see submitted reports (not Draft)
                    Reports = allReports.Where(r =>
                        r.Status != "Draft" && // Exclude Draft reports
                        (!FilterClubId.HasValue || r.ClubId == FilterClubId.Value) &&
                        (!FilterMonth.HasValue || r.ReportMonth == FilterMonth.Value) &&
                        (!FilterYear.HasValue || r.ReportYear == FilterYear.Value) &&
                        (string.IsNullOrEmpty(FilterStatus) || r.Status == FilterStatus)
                    ).OrderByDescending(r => r.ReportYear)
                     .ThenByDescending(r => r.ReportMonth)
                     .ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly reports for admin");
                TempData["Error"] = "An error occurred while loading reports";
                return Page();
            }
        }
    }

    public class ClubDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MonthlyReportListDto
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = null!;
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class ReportsResponse
    {
        public List<MonthlyReportListDto> Data { get; set; } = new();
        public int Count { get; set; }
    }
}
