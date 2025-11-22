using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace WebFE.Pages.ClubManager
{
    [Authorize(Roles = "ClubManager")]
    public class ClubInfoModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ClubInfoModel> _logger;
        private readonly IConfiguration _configuration;

        public ClubInfoModel(IHttpClientFactory httpClientFactory, ILogger<ClubInfoModel> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public ClubInputModel Input { get; set; } = new();

        public string? ApiBaseUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsEditMode { get; set; }

        public class ClubInputModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string SubName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? LogoUrl { get; set; }
            public string? BannerUrl { get; set; }
            public string CategoryName { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            IsEditMode = Request.Query.ContainsKey("edit");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "User not authenticated";
                    return Page();
                }

                // Get ClubId from TempData
                int clubId = 0;
                if (TempData["SelectedClubId"] != null)
                {
                    clubId = (int)TempData["SelectedClubId"];
                    TempData.Keep("SelectedClubId");
                }

                if (clubId == 0)
                {
                    return Redirect("/ClubManager");
                }

                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Add cookie to request
                var accessToken = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"AccessToken={accessToken}");
                }
                
                var response = await client.GetAsync($"{ApiBaseUrl}/api/club/{clubId}");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Could not load club information";
                    _logger.LogWarning("Failed to load club info: {StatusCode}", response.StatusCode);
                    return Page();
                }

                var content = await response.Content.ReadAsStringAsync();
                var club = JsonSerializer.Deserialize<ClubDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (club != null)
                {
                    Input = new ClubInputModel
                    {
                        Id = club.Id,
                        Name = club.Name,
                        SubName = club.SubName,
                        Description = club.Description,
                        LogoUrl = club.LogoUrl,
                        BannerUrl = club.BannerUrl,
                        CategoryName = club.CategoryName
                    };
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club information");
                ErrorMessage = "An error occurred while loading club information";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check your input";
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Add cookie to request
                var accessToken = Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"AccessToken={accessToken}");
                }
                
                var updateDto = new
                {
                    id = Input.Id,
                    name = Input.Name,
                    subName = Input.SubName,
                    description = Input.Description,
                    logoUrl = Input.LogoUrl,
                    bannerUrl = Input.BannerUrl
                };

                var json = JsonSerializer.Serialize(updateDto);
                var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{ApiBaseUrl}/api/club/{Input.Id}", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Club information updated successfully!";
                    return RedirectToPage();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to update club information: {errorContent}";
                    _logger.LogWarning("Failed to update club: {StatusCode} - {Error}", response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating club information");
                ErrorMessage = "An error occurred while updating club information";
            }

            return Page();
        }

        private class ClubDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string SubName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? LogoUrl { get; set; }
            public string? BannerUrl { get; set; }
            public string CategoryName { get; set; } = string.Empty;
        }
    }
}
