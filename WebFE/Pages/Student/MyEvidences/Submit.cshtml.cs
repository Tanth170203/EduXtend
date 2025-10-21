using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Student.MyEvidences
{
    public class SubmitModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SubmitModel> _logger;
        private readonly IConfiguration _configuration;

        public SubmitModel(IHttpClientFactory httpClientFactory, ILogger<SubmitModel> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CriterionOptionDto> Criteria { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int? CriterionId { get; set; }
            public string? FilePath { get; set; }
        }

        public class CriterionOptionDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Score { get; set; }
            public string GroupName { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/movement-criteria");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var criteria = JsonSerializer.Deserialize<List<CriterionOptionDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (criteria != null)
                    {
                        Criteria = criteria;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading criteria");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var studentId = GetCurrentUserId();
                
                var createDto = new CreateEvidenceDto
                {
                    StudentId = studentId,
                    Title = Input.Title,
                    Description = Input.Description,
                    CriterionId = Input.CriterionId,
                    FilePath = Input.FilePath
                };

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync("/api/evidences", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Evidence submitted successfully! Waiting for review.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Submit evidence failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    ErrorMessage = "Unable to submit evidence.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting evidence");
                ErrorMessage = "An error occurred.";
            }

            await OnGetAsync();
            return Page();
        }

        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (studentIdClaim != null && int.TryParse(studentIdClaim, out var studentId))
                {
                    return studentId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID from claims");
            }
            
            return 0;
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7055");

            foreach (var cookie in Request.Cookies)
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }

            return httpClient;
        }
    }
}

