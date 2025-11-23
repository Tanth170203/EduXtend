using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObject.DTOs.Activity;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.Activities
{
    [Authorize(Roles = "ClubManager,Admin")]
    public class EvaluationDetailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public EvaluationDetailModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public ActivityEvaluationDto? Evaluation { get; set; }
        public string? ErrorMessage { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int ActivityId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var apiUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";

                // Create request with cookie forwarding
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/api/activity/{ActivityId}/evaluation");
                foreach (var cookie in Request.Cookies)
                {
                    request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Evaluation = JsonSerializer.Deserialize<ActivityEvaluationDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Page();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Evaluation not found for this activity.";
                    return Page();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    ErrorMessage = "You do not have permission to view this evaluation.";
                    return Page();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load evaluation: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }
    }
}
