using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace WebFE.Pages.Admin.Activities
{
    public class PendingApprovalsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PendingApprovalsModel> _logger;

        public PendingApprovalsModel(IHttpClientFactory httpClientFactory, ILogger<PendingApprovalsModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<ActivityListItemDto> PendingActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Get all activities and filter pending on client side, or create a dedicated endpoint
                var allActivities = await client.GetFromJsonAsync<List<ActivityListItemDto>>("/api/activity");
                PendingActivities = allActivities?.Where(a => a.Status == "PendingApproval").ToList() ?? new();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending activities");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not authenticated";
                    return RedirectToPage();
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { AdminUserId = int.Parse(userId), Action = "Approve" };
                var response = await client.PostAsJsonAsync($"/api/admin/activities/{id}/approve", payload);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity approved successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to approve activity";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving activity {Id}", id);
                TempData["Error"] = "An error occurred while approving the activity";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, string rejectionReason)
        {
            var token = Request.Cookies["AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) return RedirectToPage("/Auth/Login");

            // Validate rejection reason
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] = "Rejection reason is required";
                return RedirectToPage();
            }

            if (rejectionReason.Length < 10)
            {
                TempData["Error"] = "Rejection reason must be at least 10 characters";
                return RedirectToPage();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not authenticated";
                    return RedirectToPage();
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new { 
                    AdminUserId = int.Parse(userId), 
                    Action = "Reject",
                    RejectionReason = rejectionReason
                };
                var response = await client.PostAsJsonAsync($"/api/admin/activities/{id}/reject", payload);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity rejected successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to reject activity {Id}: {Error}", id, errorContent);
                    TempData["Error"] = "Failed to reject activity. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting activity {Id}", id);
                TempData["Error"] = "An error occurred while rejecting the activity";
            }

            return RedirectToPage();
        }
    }
}

