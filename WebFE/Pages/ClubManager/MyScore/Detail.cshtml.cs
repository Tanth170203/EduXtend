using BusinessObject.DTOs.ClubMovementRecord;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace WebFE.Pages.ClubManager.MyScore
{
    public class DetailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(IHttpClientFactory httpClientFactory, ILogger<DetailModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<ClubMovementRecordDto> Records { get; set; } = new();
        public string SemesterName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public string PresidentName { get; set; } = string.Empty;
        public string PresidentCode { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public int TotalCriteria { get; set; }
        public bool IsCurrentSemester { get; set; }

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
                handler.CookieContainer.Add(new Uri("https://localhost:5001"), new Cookie(cookie.Key, cookie.Value));
            }

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<IActionResult> OnGetAsync(int clubId, int semesterId)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"/api/club-movement-records/club/{clubId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allRecords = JsonSerializer.Deserialize<List<ClubMovementRecordDto>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (allRecords != null)
                    {
                        // Filter records by semester
                        Records = allRecords
                            .Where(r => r.SemesterId == semesterId)
                            .OrderBy(r => r.Month)
                            .ToList();

                        if (Records.Any())
                        {
                            var firstRecord = Records.First();
                            SemesterName = firstRecord.SemesterName;
                            ClubName = firstRecord.ClubName;
                            PresidentName = firstRecord.PresidentName;
                            PresidentCode = firstRecord.PresidentCode;
                            TotalScore = Records.Sum(r => r.TotalScore);
                            TotalCriteria = Records.Sum(r => r.Details.Count);

                            var latestOverallRecord = allRecords
                                .OrderByDescending(r => r.LastUpdated ?? r.CreatedAt)
                                .FirstOrDefault();
                            IsCurrentSemester = latestOverallRecord?.SemesterId == semesterId;
                        }
                        else
                        {
                            ErrorMessage = "No records found for this semester.";
                            return RedirectToPage("./Index");
                        }
                    }
                }
                else
                {
                    _logger.LogError("Failed to load club movement records: {StatusCode}", response.StatusCode);
                    ErrorMessage = "Unable to load club movement records.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading club movement records");
                ErrorMessage = "Error loading data.";
                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}
