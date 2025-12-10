using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using BusinessObject.DTOs.MonthlyReport;

namespace WebFE.Pages.Admin.MonthlyReports
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReviewModel> _logger;

        public ReviewModel(IHttpClientFactory httpClientFactory, ILogger<ReviewModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public MonthlyReportDto? Report { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Auth/Login");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/monthly-reports/{id}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Monthly report not found";
                    return RedirectToPage("./Index");
                }

                Report = await response.Content.ReadFromJsonAsync<MonthlyReportDto>();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report for review");
                TempData["Error"] = "An error occurred while loading the report";
                return RedirectToPage("./Index");
            }
        }
    }
}
