using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Financial
{
    public class MemberFundsModel : ClubManagerPageModel
    {
        private readonly ILogger<MemberFundsModel> _logger;

        public MemberFundsModel(ILogger<MemberFundsModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? RequestId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await InitializeClubContextAsync();
            if (result is RedirectResult)
            {
                return result;
            }

            try
            {
                // Fetch club name
                using var client = CreateHttpClient();
                var clubResponse = await client.GetAsync($"api/club/{ClubId}");
                if (clubResponse.IsSuccessStatusCode)
                {
                    var content = await clubResponse.Content.ReadAsStringAsync();
                    var club = JsonSerializer.Deserialize<ClubDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    ClubName = club?.Name ?? "Club";
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading member funds page");
                TempData["ErrorMessage"] = "An error occurred while loading the page";
                return Page();
            }
        }

        private class ClubDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}

