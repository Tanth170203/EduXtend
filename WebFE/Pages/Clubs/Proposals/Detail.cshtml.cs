using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Clubs.Proposals
{
    public class DetailModel : PageModel
    {
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(ILogger<DetailModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ProposalDetailDTO? Proposal { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid proposal ID";
                return RedirectToPage("/Clubs/MyClubs");
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

                var response = await client.GetAsync($"api/proposal/{Id}/detail");
                
                if (response.IsSuccessStatusCode)
                {
                    Proposal = await response.Content.ReadFromJsonAsync<ProposalDetailDTO>();
                    return Page();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = "Proposal not found";
                    return RedirectToPage("/Clubs/MyClubs");
                }
                else
                {
                    _logger.LogError("Error loading proposal: {StatusCode}", response.StatusCode);
                    TempData["ErrorMessage"] = "An error occurred while loading the proposal";
                    return RedirectToPage("/Clubs/MyClubs");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading proposal {ProposalId}", Id);
                TempData["ErrorMessage"] = "An error occurred while loading the proposal";
                return RedirectToPage("/Clubs/MyClubs");
            }
        }

        public async Task<IActionResult> OnPostVoteAsync(int proposalId, bool isAgree)
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

                var voteDto = new ProposalVoteDTO
                {
                    ProposalId = proposalId,
                    IsAgree = isAgree
                };

                var json = JsonSerializer.Serialize(voteDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/proposal/{proposalId}/vote", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Your vote has been recorded: {(isAgree ? "Agree" : "Disagree")}";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error voting on proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Failed to record your vote. Please try again.";
                }

                return RedirectToPage(new { id = proposalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on proposal {ProposalId}", proposalId);
                TempData["ErrorMessage"] = "An error occurred while recording your vote";
                return RedirectToPage(new { id = proposalId });
            }
        }
    }
}

