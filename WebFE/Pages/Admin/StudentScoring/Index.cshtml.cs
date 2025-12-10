using BusinessObject.DTOs.MovementRecord;
using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.StudentScoring
{
    public class IndexModel(
        IHttpClientFactory httpClientFactory,
        ILogger<IndexModel> logger,
        IConfiguration configuration) : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<IndexModel> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Data Properties
        public List<MovementRecordDto> Records { get; set; } = new();
        public List<SemesterDto> Semesters { get; set; } = new();
        public int? SelectedSemesterId { get; set; }
        public string? BaseApiUrl => _configuration["ApiSettings:BaseUrl"];

        // Statistics Properties
        public int TotalStudents => Records.Count;
        public double AverageScore => Records.Count > 0 ? Records.Average(r => r.TotalScore) : 0;
        public double HighestScore => Records.Count > 0 ? Records.Max(r => r.TotalScore) : 0;
        public double LowestScore => Records.Count > 0 ? Records.Min(r => r.TotalScore) : 0;
        public int ExcellentCount => Records.Count(r => r.TotalScore >= 80);
        public int GoodCount => Records.Count(r => r.TotalScore >= 60 && r.TotalScore < 80);
        public int NeedImprovementCount => Records.Count(r => r.TotalScore < 60);

        // Messages
        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Create HTTP Client with cookies for authentication
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new(),
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Copy cookies from request
            foreach (var cookie in Request.Cookies)
            {
                handler.CookieContainer.Add(
                    new(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001"),
                    new Cookie(cookie.Key, cookie.Value)
                );
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));

            return client;
        }

        /// <summary>
        /// Load movement reports data
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int? semesterId)
        {
            try
            {
                using var httpClient = CreateHttpClient();

                // Load semesters for filter dropdown
                await LoadSemestersAsync(httpClient);

                // If no semester specified, default to current active semester
                if (!semesterId.HasValue)
                {
                    var currentSemester = Semesters.FirstOrDefault(s => s.IsActive);
                    if (currentSemester != null)
                    {
                        semesterId = currentSemester.Id;
                        _logger.LogInformation("No semester specified, defaulting to current semester: {SemesterName}", currentSemester.Name);
                    }
                }

                SelectedSemesterId = semesterId;

                // Load movement records
                await LoadMovementRecordsAsync(httpClient, semesterId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while loading movement reports");
                ErrorMessage = "Unable to connect to the server. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading movement reports");
                ErrorMessage = "An error occurred while loading data. Please try again.";
            }

            return Page();
        }

        /// <summary>
        /// Load semesters from API
        /// </summary>
        private async Task LoadSemestersAsync(HttpClient httpClient)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/semesters");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var semesters = JsonSerializer.Deserialize<List<SemesterDto>>(content, JsonOptions);

                    if (semesters != null)
                    {
                        Semesters = semesters.OrderByDescending(s => s.StartDate).ToList();
                        _logger.LogInformation("Loaded {Count} semesters", Semesters.Count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load semesters: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading semesters");
            }
        }

        /// <summary>
        /// Load movement records from API
        /// </summary>
        private async Task LoadMovementRecordsAsync(HttpClient httpClient, int? semesterId)
        {
            try
            {
                string endpoint = semesterId.HasValue
                    ? $"/api/movement-records/semester/{semesterId.Value}"
                    : "/api/movement-records";

                _logger.LogInformation("Loading movement records from: {Endpoint}", endpoint);

                var response = await httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var records = JsonSerializer.Deserialize<List<MovementRecordDto>>(content, JsonOptions);

                    if (records != null)
                    {
                        Records = records.OrderByDescending(r => r.TotalScore).ToList();
                        _logger.LogInformation("Loaded {Count} movement records", Records.Count);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to load movement records: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    ErrorMessage = "Unable to load activity score list. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movement records");
                throw;
            }
        }

        /// <summary>
        /// Create new movement record
        /// </summary>
        public async Task<IActionResult> OnPostCreateAsync(int studentId, int semesterId)
        {
            if (studentId <= 0 || semesterId <= 0)
            {
                ErrorMessage = "Invalid information.";
                return RedirectToPage();
            }

            try
            {
                var createDto = new CreateMovementRecordDto
                {
                    StudentId = studentId,
                    SemesterId = semesterId
                };

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync("/api/movement-records", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Movement record created successfully!";
                    _logger.LogInformation("Created movement record for student {StudentId} in semester {SemesterId}",
                        studentId, semesterId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Create movement record failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.Conflict => "The student already has a record for this semester.",
                        HttpStatusCode.NotFound => "Student or semester not found.",
                        _ => "Unable to create record. Please try again."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movement record");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Add score to movement record
        /// </summary>
        public async Task<IActionResult> OnPostAddScoreAsync(
            int movementRecordId,
            int criterionId,
            double score)
        {
            if (movementRecordId <= 0 || criterionId <= 0 || score < 0)
            {
                ErrorMessage = "Invalid information.";
                return RedirectToPage();
            }

            try
            {
                var addScoreDto = new AddScoreDto
                {
                    MovementRecordId = movementRecordId,
                    CriterionId = criterionId,
                    Score = score
                };

                var json = JsonSerializer.Serialize(addScoreDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var httpClient = CreateHttpClient();
                var response = await httpClient.PostAsync("/api/movement-records/add-score", content);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = $"✅ Successfully added {score} points!";
                    _logger.LogInformation("Added {Score} points to record {RecordId} for criterion {CriterionId}",
                        score, movementRecordId, criterionId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Add score failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "Record or criterion not found.",
                        HttpStatusCode.BadRequest => "Score exceeds allowed limit.",
                        _ => "Unable to add score. Please try again."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding score");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Delete movement record
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (id <= 0)
            {
                ErrorMessage = "Invalid ID.";
                return RedirectToPage();
            }

            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.DeleteAsync($"/api/movement-records/{id}");

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "✅ Movement record deleted successfully!";
                    _logger.LogInformation("Deleted movement record {RecordId}", id);
                }
                else
                {
                    _logger.LogWarning("Delete failed: {StatusCode}", response.StatusCode);
                    ErrorMessage = response.StatusCode == HttpStatusCode.NotFound
                        ? "Record to delete not found."
                        : "Unable to delete record. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movement record");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Add manual score directly for a student (admin scoring)
        /// </summary>
        public async Task<IActionResult> OnPostAddManualScoreAsync(
            int studentId,
            int categoryId,
            double score,
            string comments)
        {
            if (studentId <= 0 || categoryId <= 0 || score < 0)
            {
                ErrorMessage = "Invalid information.";
                return RedirectToPage();
            }

            try
            {
                // Map category to criterion
                // For demo: category 1-4 maps to generic criteria
                // In production: would use actual criteria IDs from database
                int criterionId = categoryId; // Simplified - in production get from DB

                // Get the current semester (assuming active semester)
                using var httpClient = CreateHttpClient();
                var semesterResponse = await httpClient.GetAsync("/api/semesters");
                int semesterId = 1; // Default, should get from API

                if (semesterResponse.IsSuccessStatusCode)
                {
                    var semesterContent = await semesterResponse.Content.ReadAsStringAsync();
                    var semesters = JsonSerializer.Deserialize<List<SemesterDto>>(semesterContent, JsonOptions);

                    if (semesters?.Any() == true)
                    {
                        semesterId = semesters.First().Id;
                    }
                }

                // Create movement record if not exists
                var createRecordDto = new CreateMovementRecordDto
                {
                    StudentId = studentId,
                    SemesterId = semesterId
                };

                var json = JsonSerializer.Serialize(createRecordDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Try to create (will fail if exists, which is fine)
                var createResponse = await httpClient.PostAsync("/api/movement-records", content);

                // Add score
                var addScoreDto = new AddScoreDto
                {
                    MovementRecordId = 1, // Would get from create response or lookup
                    CriterionId = criterionId,
                    Score = score
                };

                var scoreJson = JsonSerializer.Serialize(addScoreDto);
                var scoreContent = new StringContent(scoreJson, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("/api/movement-records/add-score", scoreContent);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = $"✅ Successfully added {score} points to the student!";
                    _logger.LogInformation("Added {Score} points to student {StudentId}. Comments: {Comments}",
                        score, studentId, comments);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Add score failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.NotFound => "Student or criterion not found.",
                        HttpStatusCode.BadRequest => "Score exceeds allowed limit.",
                        _ => "Unable to add score. Please try again."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding manual score");
                ErrorMessage = "An error occurred. Please try again.";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Export to Excel (placeholder for future implementation)
        /// </summary>
        public IActionResult OnPostExportExcel(int? semesterId)
        {
            // TODO: Implement Excel export
            ErrorMessage = "Excel export feature is under development.";
            return RedirectToPage(new { semesterId });
        }

        /// <summary>
        /// Export to PDF (placeholder for future implementation)
        /// </summary>
        public IActionResult OnPostExportPdf()
        {
            // TODO: Implement PDF export
            ErrorMessage = "PDF export feature is under development.";
            return RedirectToPage();
        }
    }
}
