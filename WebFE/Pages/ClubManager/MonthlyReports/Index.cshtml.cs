using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebFE.Pages.ClubManager.MonthlyReports
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

        public List<MonthlyReportListDto> Reports { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

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
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Get monthly reports using ClubId from TempData
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/monthly-reports?clubId={ClubId}");
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
                if (ClubId <= 0)
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
                    clubId = ClubId,
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
