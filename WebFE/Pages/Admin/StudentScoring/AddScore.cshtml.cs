using BusinessObject.DTOs.MovementCriteria;
using BusinessObject.DTOs.MovementRecord;
using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.StudentScoring
{
    public class AddScoreModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AddScoreModel> _logger;
        private readonly IConfiguration _configuration;

        public AddScoreModel(
            IHttpClientFactory httpClientFactory,
            ILogger<AddScoreModel> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public int StudentId { get; set; }

        [BindProperty]
        public int GroupId { get; set; }

        [BindProperty]
        public int CriterionId { get; set; }

        [BindProperty]
        public double Score { get; set; }

        [BindProperty]
        public string? Comments { get; set; }

        [BindProperty]
        public int? ActivityId { get; set; }

        public List<StudentDto> Students { get; set; } = new();
        public List<MovementCriterionGroupDto> Groups { get; set; } = new();
        public List<MovementCriterionDto> AllCriteria { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // Pre-selected student (from query string)
        public int? PreSelectedStudentId { get; set; }
        public string? PreSelectedStudentName { get; set; }
        public string? PreSelectedStudentCode { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int? studentId, string? studentName, string? studentCode)
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Load students
                await LoadStudentsAsync(httpClient);

                // Load Groups and Criteria (only for Student target type)
                await LoadGroupsAndCriteriaAsync(httpClient);

                // Pre-select student if provided
                if (studentId.HasValue)
                {
                    PreSelectedStudentId = studentId;
                    PreSelectedStudentName = studentName;
                    PreSelectedStudentCode = studentCode;
                    StudentId = studentId.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading add score page");
                ErrorMessage = "An error occurred while loading the page. Please try again.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Invalid information.";
                await ReloadDataAsync();
                return Page();
            }

            if (StudentId <= 0 || GroupId <= 0 || CriterionId <= 0 || Score < 0)
            {
                ErrorMessage = "Please provide all required information.";
                await ReloadDataAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Comments) || Comments.Length < 10)
            {
                ErrorMessage = "Comments are required and must be at least 10 characters.";
                await ReloadDataAsync();
                return Page();
            }

            try
            {
                using var httpClient = CreateHttpClient();

                var payload = new
                {
                    studentId = StudentId,
                    categoryId = GroupId,
                    criterionId = CriterionId,
                    score = Score,
                    comments = Comments,
                    awardedDate = DateTime.Now
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending score request: {Payload}", json);

                var response = await httpClient.PostAsync("/api/movement-records/add-manual-score-with-criterion", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Score added successfully: {Result}", result);

                    TempData["SuccessMessage"] = "✅ Score added successfully!";
                    return RedirectToPage("/Admin/StudentScoring/Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Add score failed: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "Student or criterion not found.",
                        HttpStatusCode.BadRequest => "Score exceeds allowed limit or data is invalid.",
                        _ => $"Unable to add score: {errorContent}"
                    };

                    await ReloadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding score");
                ErrorMessage = $"An error occurred: {ex.Message}";

                await ReloadDataWithPreservationAsync();

                return Page();
            }
        }

        private async Task LoadStudentsAsync(HttpClient httpClient)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/students");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var students = JsonSerializer.Deserialize<List<StudentDto>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (students != null)
                    {
                        Students = students.OrderBy(s => s.FullName ?? s.StudentCode).ToList();
                        _logger.LogInformation("Loaded {Count} students", Students.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load students: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students");
            }
        }

        private async Task LoadGroupsAndCriteriaAsync(HttpClient httpClient)
        {
            try
            {
                // Load Groups (filter Student target type)
                var groupsResponse = await httpClient.GetAsync("/api/movement-criterion-groups");
                if (groupsResponse.IsSuccessStatusCode)
                {
                    var content = await groupsResponse.Content.ReadAsStringAsync();
                    var allGroups = JsonSerializer.Deserialize<List<MovementCriterionGroupDto>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (allGroups != null)
                    {
                        // Only get groups for Student
                        Groups = allGroups.Where(g => g.TargetType == "Student").ToList();
                        _logger.LogInformation("Loaded {Count} student groups", Groups.Count);
                    }
                }

                // Load All Criteria (filter Student target type)
                var criteriaResponse = await httpClient.GetAsync("/api/movement-criteria");
                if (criteriaResponse.IsSuccessStatusCode)
                {
                    var content = await criteriaResponse.Content.ReadAsStringAsync();
                    var allCriteria = JsonSerializer.Deserialize<List<MovementCriterionDto>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (allCriteria != null)
                    {
                        // Only get criteria for Student and active ones
                        AllCriteria = allCriteria.Where(c => c.TargetType == "Student" && c.IsActive).ToList();
                        _logger.LogInformation("Loaded {Count} student criteria", AllCriteria.Count);
                    }
                }

                _logger.LogInformation("Groups and criteria loaded, preserving GroupId={GroupId}, CriterionId={CriterionId}", GroupId, CriterionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading groups and criteria");
            }
        }

        private async Task ReloadDataAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                await LoadStudentsAsync(httpClient);
                await LoadGroupsAndCriteriaAsync(httpClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading data");
            }
        }

        private async Task ReloadDataWithPreservationAsync()
        {
            try
            {
                using var httpClient = CreateHttpClient();
                await LoadStudentsAsync(httpClient);
                await LoadGroupsAndCriteriaAsync(httpClient);
                _logger.LogInformation("Data reloaded with preservation for GroupId={GroupId}, CriterionId={CriterionId}", GroupId, CriterionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading data with preservation");
            }
        }
    }

    // DTOs
    public class StudentDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? StudentCode { get; set; }
    }
}
