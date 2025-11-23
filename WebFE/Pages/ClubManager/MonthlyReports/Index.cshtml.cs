using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.MonthlyReports
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
        public int? ClubId { get; set; }

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

                // Get club ID first
                var clubRequest = new HttpRequestMessage(HttpMethod.Get, "api/club/my-managed-club");
                foreach (var cookie in Request.Cookies)
                {
                    clubRequest.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var clubResponse = await client.SendAsync(clubRequest);
                if (!clubResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "You are not managing any club";
                    return Page();
                }

                var club = await clubResponse.Content.ReadFromJsonAsync<ClubDto>();
                if (club == null)
                {
                    return Page();
                }

                ClubId = club.Id;

                // Get monthly reports
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/monthly-reports?clubId={club.Id}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<MonthlyReportListDto>>>();
                    var allReports = apiResponse?.Data ?? new();
                    
                    // Apply filters
                    Reports = allReports.Where(r =>
                        (!FilterMonth.HasValue || r.ReportMonth == FilterMonth.Value) &&
                        (!FilterYear.HasValue || r.ReportYear == FilterYear.Value) &&
                        (string.IsNullOrEmpty(FilterStatus) || r.Status == FilterStatus)
                    ).OrderByDescending(r => r.ReportYear).ThenByDescending(r => r.ReportMonth).ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly reports");
                TempData["Error"] = "An error occurred while loading reports";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                if (!ClubId.HasValue)
                {
                    TempData["Error"] = "Club not found";
                    return RedirectToPage();
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Post, "api/monthly-reports");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var now = DateTime.Now;
                var createDto = new
                {
                    clubId = ClubId.Value,
                    month = now.Month,
                    year = now.Year
                };

                request.Content = JsonContent.Create(createDto);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Monthly report created successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create report: {Error}", errorContent);
                    TempData["Error"] = "Failed to create monthly report. It may already exist for this month.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monthly report");
                TempData["Error"] = "An error occurred while creating the report";
            }

            return RedirectToPage();
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
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public int Count { get; set; }
    }
}
