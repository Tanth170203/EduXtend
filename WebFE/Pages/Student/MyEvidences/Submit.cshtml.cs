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
            public IFormFile? File { get; set; }
        }

        public class CriterionOptionDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;  // ✅ Tên tiêu chí con
            public int MaxScore { get; set; }  // ✅ Điểm tối đa
            public string GroupName { get; set; } = string.Empty;  // Chỉ để nhận dữ liệu, không hiển thị
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                
                // ✅ Chỉ load criteria cho Student (không load criteria của Club)
                var response = await httpClient.GetAsync("/api/movement-criteria/by-target-type/Student");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var criteria = JsonSerializer.Deserialize<List<CriterionOptionDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (criteria != null)
                    {
                        // ✅ Chỉ lấy criteria đang active
                        Criteria = criteria.Where(c => c.IsActive).ToList();
                        _logger.LogInformation("Loaded {Count} active Student criteria", Criteria.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load criteria: {StatusCode}", response.StatusCode);
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

                using var httpClient = CreateHttpClient();

                // Create multipart form data
                using var formData = new MultipartFormDataContent();

                // Add text fields
                formData.Add(new StringContent(studentId.ToString()), "StudentId");
                formData.Add(new StringContent(Input.Title), "Title");
                
                if (!string.IsNullOrEmpty(Input.Description))
                {
                    formData.Add(new StringContent(Input.Description), "Description");
                }
                
                if (Input.CriterionId.HasValue)
                {
                    formData.Add(new StringContent(Input.CriterionId.Value.ToString()), "CriterionId");
                }

                // Add file if provided
                if (Input.File != null && Input.File.Length > 0)
                {
                    // Validate file size (10MB max)
                    const long maxFileSize = 10 * 1024 * 1024; // 10MB
                    if (Input.File.Length > maxFileSize)
                    {
                        ErrorMessage = "File size exceeds 10MB limit.";
                        await OnGetAsync();
                        return Page();
                    }

                    // Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                    var extension = Path.GetExtension(Input.File.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ErrorMessage = "Invalid file type. Allowed types: JPG, PNG, PDF, DOC, DOCX.";
                        await OnGetAsync();
                        return Page();
                    }

                    var fileContent = new StreamContent(Input.File.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Input.File.ContentType);
                    formData.Add(fileContent, "File", Input.File.FileName);
                }

                var response = await httpClient.PostAsync("/api/evidences", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Evidence submitted successfully! Waiting for review.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Submit evidence failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    
                    // Try to parse error message from JSON
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("message", out var messageProperty))
                        {
                            ErrorMessage = messageProperty.GetString() ?? "Unable to submit evidence.";
                        }
                        else
                        {
                            ErrorMessage = "Unable to submit evidence.";
                        }
                    }
                    catch
                    {
                        ErrorMessage = "Unable to submit evidence.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting evidence");
                ErrorMessage = "An error occurred while submitting evidence.";
            }

            await OnGetAsync();
            return Page();
        }

        private int GetCurrentUserId()
        {
            try
            {
                // Try to get StudentId from JWT claims first (for Student users)
                var studentIdClaim = User.FindFirst("StudentId")?.Value;
                if (studentIdClaim != null && int.TryParse(studentIdClaim, out var studentId))
                {
                    _logger.LogInformation("Got StudentId from claim: {StudentId}", studentId);
                    return studentId;
                }

                // Fallback: try to get from JWT token in cookie
                if (Request.Cookies.TryGetValue("AccessToken", out var token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        
                        // Log all claims for debugging
                        _logger.LogInformation("JWT Claims: {Claims}", 
                            string.Join(", ", jwt.Claims.Select(c => $"{c.Type}={c.Value}")));
                        
                        // Try to get StudentId from token
                        var studentIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == "StudentId")?.Value;
                        if (studentIdFromToken != null && int.TryParse(studentIdFromToken, out var studentIdFromJwt))
                        {
                            _logger.LogInformation("Got StudentId from JWT token: {StudentId}", studentIdFromJwt);
                            return studentIdFromJwt;
                        }

                        // Fallback to UserId if StudentId not found
                        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                        if (userIdFromToken != null && int.TryParse(userIdFromToken, out var userIdFromJwt))
                        {
                            _logger.LogWarning("Only found UserId {UserId}, StudentId not in token. User may need to re-login.", userIdFromJwt);
                            return userIdFromJwt; // This might cause issues if UserId != StudentId
                        }
                    }
                }

                _logger.LogWarning("Could not determine StudentId from any source");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting StudentId");
                return 0;
            }
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

