using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    public class IndexModel : ClubManagerPageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public List<ActivityListItemDto> Activities { get; set; } = new();
        public string ApiBaseUrl { get; set; } = string.Empty;
        
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        
        [BindProperty(SupportsGet = true)]
        public int? ClubIdParam { get; set; }
        
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync()
        {
            // If clubId provided in query string, use it and save to TempData
            if (ClubIdParam.HasValue)
            {
                ClubId = ClubIdParam.Value;
                TempData["SelectedClubId"] = ClubIdParam.Value;
                TempData.Keep("SelectedClubId");
            }
            else
            {
                // Initialize club context from TempData
                var result = await InitializeClubContextAsync();
                if (result is RedirectResult)
                {
                    return result;
                }
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                
                // Get activities for the selected club with pagination
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/activity/club/{ClubId}?page={PageNumber}&pageSize={PageSize}");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var paginatedResult = await response.Content.ReadFromJsonAsync<BusinessObject.DTOs.Common.PaginatedResultDto<ActivityListItemDto>>();
                    if (paginatedResult != null)
                    {
                        Activities = paginatedResult.Items;
                        TotalPages = paginatedResult.TotalPages;
                        TotalCount = paginatedResult.TotalCount;
                    }
                }

                ApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club activities");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/activity/club-manager/{id}");
                
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Activity deleted successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete activity {Id}: {Error}", id, errorContent);
                    TempData["Error"] = "Failed to delete activity. You may not have permission.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting activity");
                TempData["Error"] = "An error occurred while deleting the activity";
            }

            return RedirectToPage();
        }
    }
}


