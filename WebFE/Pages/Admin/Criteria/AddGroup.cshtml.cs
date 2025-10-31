using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.Criteria
{
    public class AddGroupModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AddGroupModel> _logger;
        private readonly IConfiguration _configuration;

        public AddGroupModel(
            IHttpClientFactory httpClientFactory,
            ILogger<AddGroupModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int MaxScore { get; set; } = 100;

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

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Invalid input data.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Criterion group name must not be empty.";
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

                var createDto = new
                {
                    name = Name,
                    description = Description,
                    maxScore = MaxScore,
                    targetType = TargetType
                };

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Creating criterion group: {Json}", json);

                var response = await httpClient.PostAsync("/api/movement-criterion-groups", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Criterion group created successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Create failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = $"Unable to create criterion group: {errorContent}";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating criterion group");
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }
    }
}
