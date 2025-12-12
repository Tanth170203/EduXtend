using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Clubs.Proposals
{
    public class EditModel : PageModel
    {
        private readonly ILogger<EditModel> _logger;

        public EditModel(ILogger<EditModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

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
                    
                    if (Proposal == null)
                    {
                        TempData["ErrorMessage"] = "Proposal not found";
                        return RedirectToPage("/Clubs/MyClubs");
                    }

                    // Check if user is the creator
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (Proposal.CreatedById.ToString() != userId)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to edit this proposal";
                        return RedirectToPage("/Clubs/Proposals/Detail", new { id = Id });
                    }

                    // Check if proposal is still pending vote
                    if (Proposal.Status != "PendingVote")
                    {
                        TempData["ErrorMessage"] = "You can only edit proposals that are pending vote";
                        return RedirectToPage("/Clubs/Proposals/Detail", new { id = Id });
                    }

                    // Populate form fields
                    Title = Proposal.Title;
                    Description = Proposal.Description;

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

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

                var updateDto = new UpdateProposalDTO
                {
                    Title = Title,
                    Description = Description
                };

                var json = JsonSerializer.Serialize(updateDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/proposal/{Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Proposal updated successfully!";
                    return RedirectToPage("/Clubs/Proposals/Detail", new { id = Id });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error updating proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Failed to update proposal. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating proposal");
                TempData["ErrorMessage"] = "An error occurred while updating the proposal";
                return Page();
            }
        }
    }
}
