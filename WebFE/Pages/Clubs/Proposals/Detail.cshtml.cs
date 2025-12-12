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
        
        public bool IsCreator { get; set; }
        
        public bool IsClubManager { get; set; }

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
                    
                    // Check if current user is the creator
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    IsCreator = Proposal?.CreatedById.ToString() == userId;
                    
                    // Check if current user has ClubManager role (fast check, no API call)
                    // Backend API will do the actual authorization check
                    IsClubManager = User.IsInRole("ClubManager");
                    
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

        public async Task<IActionResult> OnPostDeleteAsync(int proposalId)
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

                var response = await client.DeleteAsync($"api/proposal/{proposalId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Proposal deleted successfully!";
                    
                    // Get club ID from the proposal before redirecting
                    var proposalResponse = await client.GetAsync($"api/proposal/{proposalId}/detail");
                    if (proposalResponse.IsSuccessStatusCode)
                    {
                        var proposal = await proposalResponse.Content.ReadFromJsonAsync<ProposalDetailDTO>();
                        return RedirectToPage("/Clubs/MemberDashboard", new { id = proposal?.ClubId, section = "proposals" });
                    }
                    
                    return RedirectToPage("/Clubs/MyClubs");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error deleting proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Failed to delete proposal. Please try again.";
                    return RedirectToPage(new { id = proposalId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting proposal {ProposalId}", proposalId);
                TempData["ErrorMessage"] = "An error occurred while deleting the proposal";
                return RedirectToPage(new { id = proposalId });
            }
        }

        public async Task<IActionResult> OnPostAutoFillToActivityAsync(int proposalId)
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

                _logger.LogInformation("Calling API to extract activity from proposal {ProposalId}", proposalId);

                // Call API to extract activity data from proposal
                var response = await client.PostAsync($"api/proposal/{proposalId}/extract-to-activity", null);

                if (response.IsSuccessStatusCode)
                {
                    var extractedData = await response.Content.ReadFromJsonAsync<BusinessObject.DTOs.Activity.ExtractedActivityDto>();
                    
                    if (extractedData != null)
                    {
                        // Store extracted data in TempData for next page
                        TempData["ExtractedActivityData"] = JsonSerializer.Serialize(extractedData);
                        TempData["SuccessMessage"] = "Activity data extracted successfully! Please review and submit.";
                        
                        _logger.LogInformation("Successfully extracted activity from proposal {ProposalId}, redirecting to create activity", proposalId);
                        
                        // Redirect to ClubManager activity creation page
                        return RedirectToPage("/ClubManager/Activities/Create");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to extract activity data. Please try again.";
                        return RedirectToPage(new { id = proposalId });
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] = "You don't have permission to perform this action.";
                    return RedirectToPage(new { id = proposalId });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Bad request extracting activity from proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Only approved proposals can be converted to activities.";
                    return RedirectToPage(new { id = proposalId });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error extracting activity from proposal: {StatusCode} - {Error}", response.StatusCode, error);
                    TempData["ErrorMessage"] = "Failed to extract activity data. Please try again.";
                    return RedirectToPage(new { id = proposalId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-fill from proposal {ProposalId}", proposalId);
                TempData["ErrorMessage"] = "An error occurred during auto-fill. Please try again.";
                return RedirectToPage(new { id = proposalId });
            }
        }

    }
}

