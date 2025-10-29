using BusinessObject.DTOs.Proposal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Clubs.Proposals
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ILogger<CreateModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int ClubId { get; set; }

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        public string ClubName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Auth/Login");
            }

            if (ClubId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid club ID";
                return RedirectToPage("/Clubs/MyClubs");
            }

            try
            {
                // Fetch club name
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

                var clubResponse = await client.GetAsync($"api/club/{ClubId}");
                if (clubResponse.IsSuccessStatusCode)
                {
                    var club = await clubResponse.Content.ReadFromJsonAsync<BusinessObject.DTOs.Club.ClubDetailDto>();
                    ClubName = club?.Name ?? "Club";
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create proposal page");
                TempData["ErrorMessage"] = "An error occurred";
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

                var createDto = new CreateProposalDTO
                {
                    ClubId = ClubId,
                    Title = Title,
                    Description = Description
                };

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/proposal", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Proposal created successfully!";
                    return RedirectToPage("/Clubs/MemberDashboard", new { id = ClubId, section = "proposals" });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error creating proposal: {Error}", error);
                    TempData["ErrorMessage"] = "Failed to create proposal. Please try again.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating proposal");
                TempData["ErrorMessage"] = "An error occurred while creating the proposal";
                return Page();
            }
        }
    }
}

