using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Criteria
{
    public class EditGroupModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EditGroupModel> _logger;
        private readonly IConfiguration _configuration;

        public EditGroupModel(
            IHttpClientFactory httpClientFactory,
            ILogger<EditGroupModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int MaxScore { get; set; }

        [BindProperty]
        public string TargetType { get; set; } = "Student";

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(
                    new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001"),
                    new Cookie(cookie.Key, cookie.Value)
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );

            return client;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "Invalid ID.";
                return RedirectToPage("./Index");
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/movement-criterion-groups/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var group = JsonSerializer.Deserialize<MovementCriterionGroupDto>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (group != null)
                    {
                        Id = group.Id;
                        Name = group.Name ?? string.Empty;
                        Description = group.Description;
                        MaxScore = group.MaxScore;
                        TargetType = group.TargetType ?? "Student";
                    }
                }
                else
                {
                    ErrorMessage = "Criterion group not found.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criterion group");
                ErrorMessage = "An error occurred while loading data.";
                return RedirectToPage("./Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Invalid input.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Group name must not be empty.";
                return Page();
            }

            if (MaxScore <= 0)
            {
                ErrorMessage = "Max score must be greater than 0.";
                return Page();
            }

            try
            {
                using var httpClient = CreateHttpClient();

                var updateDto = new
                {
                    id = Id,
                    name = Name,
                    description = Description,
                    maxScore = MaxScore,
                    targetType = TargetType
                };

                var json = JsonSerializer.Serialize(updateDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Updating criterion group: {Json}", json);

                var response = await httpClient.PutAsync($"/api/movement-criterion-groups/{Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Criterion group updated successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Update failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Unable to update criterion group: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating criterion group");
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }
    }
}
