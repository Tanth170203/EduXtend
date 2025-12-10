using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using BusinessObject.DTOs.MonthlyReport;

namespace WebFE.Pages.ClubManager.MonthlyReports
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IHttpClientFactory httpClientFactory, ILogger<EditModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public MonthlyReportDto? Report { get; set; }

        [BindProperty]
        public string? EventMediaUrls { get; set; }

        [BindProperty]
        public string? Purpose { get; set; }

        [BindProperty]
        public string? Significance { get; set; }

        [BindProperty]
        public string? ClubResponsibilitiesText { get; set; }

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
                
                if (Report == null)
                {
                    return RedirectToPage("./Index");
                }

                // Check if report can be edited
                if (Report.Status != "Draft" && Report.Status != "Rejected")
                {
                    TempData["Error"] = "This report cannot be edited";
                    return RedirectToPage("./Details", new { id });
                }

                // Populate form fields
                EventMediaUrls = Report.CurrentMonthActivities.SchoolEvents.FirstOrDefault()?.MediaUrls;
                Purpose = Report.NextMonthPlans.Purpose.Purpose;
                Significance = Report.NextMonthPlans.Purpose.Significance;
                
                // Load custom text directly
                ClubResponsibilitiesText = Report.NextMonthPlans.Responsibilities.CustomText ?? string.Empty;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report for edit");
                TempData["Error"] = "An error occurred while loading the report";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Build the update DTO
                var purposeDto = new
                {
                    Purpose = Purpose ?? string.Empty,
                    Significance = Significance ?? string.Empty
                };

                // Just save the custom text
                var responsibilitiesDto = new
                {
                    CustomText = ClubResponsibilitiesText ?? string.Empty
                };

                var updateDto = new
                {
                    EventMediaUrls,
                    NextMonthPurposeAndSignificance = JsonSerializer.Serialize(purposeDto),
                    ClubResponsibilities = JsonSerializer.Serialize(responsibilitiesDto)
                };

                var request = new HttpRequestMessage(HttpMethod.Put, $"api/monthly-reports/{id}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                request.Content = JsonContent.Create(updateDto);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Báo cáo đã được cập nhật thành công";
                    return RedirectToPage("./Details", new { id });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update report: {Error}", errorContent);
                    
                    // Try to parse error message for validation errors (Requirements: 19.3)
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("message", out var messageElement))
                        {
                            var errorMessage = messageElement.GetString();
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                TempData["Error"] = errorMessage;
                                return await OnGetAsync(id);
                            }
                        }
                    }
                    catch
                    {
                        // If parsing fails, use generic error
                    }
                    
                    TempData["Error"] = "Không thể cập nhật báo cáo. Vui lòng kiểm tra lại thông tin.";
                    return await OnGetAsync(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật báo cáo";
                return await OnGetAsync(id);
            }
        }
    }
}
