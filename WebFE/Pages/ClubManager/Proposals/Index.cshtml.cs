using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Proposals
{
    public class IndexModel : ClubManagerPageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public List<ProposalDTO> Proposals { get; set; } = new();

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
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var accessToken = Request.Cookies["AccessToken"];
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };

                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                // Get all proposals for this club using ClubId from TempData
                var proposalsResponse = await client.GetAsync($"api/proposal/club/{ClubId}");
                if (proposalsResponse.IsSuccessStatusCode)
                {
                    Proposals = await proposalsResponse.Content.ReadFromJsonAsync<List<ProposalDTO>>() ?? new();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposals");
                TempData["ErrorMessage"] = "An error occurred while loading proposals";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCloseAsync(int proposalId)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Auth/Login");
            }

            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                var accessToken = Request.Cookies["AccessToken"];
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:5001")
                };

                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await client.PostAsync($"api/proposal/{proposalId}/close", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Proposal closed successfully!";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error closing proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Failed to close proposal. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing proposal");
                TempData["ErrorMessage"] = "An error occurred while closing the proposal";
            }

            return RedirectToPage();
        }
    }
}

