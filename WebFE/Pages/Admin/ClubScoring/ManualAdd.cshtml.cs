using BusinessObject.DTOs.ClubMovementRecord;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace WebFE.Pages.Admin.ClubScoring;

public class ManualAddModel : PageModel
{
    private readonly EduXtendContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ManualAddModel> _logger;
    private readonly IConfiguration _configuration;

    public ManualAddModel(
        EduXtendContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ManualAddModel> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public int ClubId { get; set; }
    public int SemesterId { get; set; }
    public int Month { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;

    public List<LocalGroupVM> Groups { get; set; } = new();
    public List<LocalCriterionVM> AllCriteria { get; set; } = new();

    [BindProperty]
    public int GroupId { get; set; }

    [BindProperty]
    public AddClubManualScoreDto Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(int clubId, int semesterId, int month)
    {
        ClubId = clubId; SemesterId = semesterId; Month = month;
        var club = await _context.Clubs.FindAsync(clubId);
        var sem = await _context.Semesters.FindAsync(semesterId);
        ClubName = club?.Name ?? string.Empty;
        SemesterName = sem?.Name ?? string.Empty;

        // Load Groups for Club
        Groups = await _context.MovementCriterionGroups
            .Where(g => g.TargetType == "Club")
            .OrderBy(g => g.Name)
            .Select(g => new LocalGroupVM
            {
                Id = g.Id,
                Name = g.Name,
                MaxScore = g.MaxScore
            })
            .ToListAsync();

        // Load all criteria for Club
        AllCriteria = await _context.MovementCriteria
            .Where(c => c.TargetType == "Club" && c.IsActive)
            .Select(c => new LocalCriterionVM
            {
                Id = c.Id,
                GroupId = c.GroupId,
                Title = c.Title,
                MinScore = c.MinScore ?? 0,
                MaxScore = c.MaxScore
            })
            .OrderBy(c => c.Title)
            .ToListAsync();

        Input.ClubId = clubId; Input.SemesterId = semesterId; Input.Month = month;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Input.CriterionId <= 0 || Input.Score <= 0 || string.IsNullOrWhiteSpace(Input.Note))
        {
            await ReloadDataWithPreservationAsync();
            return Page();
        }

        if (Input.Note.Length < 10)
        {
            ErrorMessage = "Note must be at least 10 characters long.";
            await ReloadDataWithPreservationAsync();
            return Page();
        }

        try
        {
            using var httpClient = CreateHttpClient();

            var payload = new
            {
                clubId = Input.ClubId,
                semesterId = Input.SemesterId,
                month = Input.Month,
                criterionId = Input.CriterionId,
                score = Input.Score,
                note = Input.Note
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending club score request: {Payload}", json);

            var response = await httpClient.PostAsync("/api/club-movement-records/add-manual-score", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Club score added successfully: {Result}", result);

                TempData["SuccessMessage"] = "✅ Score added successfully!";
                return RedirectToPage("Detail", new { clubId = Input.ClubId, semesterId = Input.SemesterId, month = Input.Month });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Add club score failed: {StatusCode} - {Error}", response.StatusCode, errorContent);

                ErrorMessage = response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "Club or criterion not found.",
                    HttpStatusCode.BadRequest => "Score exceeds allowed limit or data is invalid.",
                    _ => $"Unable to add score: {errorContent}"
                };

                await ReloadDataWithPreservationAsync();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding club score");
            ErrorMessage = $"An error occurred: {ex.Message}";
            await ReloadDataWithPreservationAsync();
            return Page();
        }
    }

    private async Task ReloadDataWithPreservationAsync()
    {
        ClubId = Input.ClubId; SemesterId = Input.SemesterId; Month = Input.Month;
        var club = await _context.Clubs.FindAsync(ClubId);
        var sem = await _context.Semesters.FindAsync(SemesterId);
        ClubName = club?.Name ?? string.Empty;
        SemesterName = sem?.Name ?? string.Empty;

        // Load Groups
        Groups = await _context.MovementCriterionGroups
            .Where(g => g.TargetType == "Club")
            .OrderBy(g => g.Name)
            .Select(g => new LocalGroupVM
            {
                Id = g.Id,
                Name = g.Name,
                MaxScore = g.MaxScore
            })
            .ToListAsync();

        // Load all criteria
        AllCriteria = await _context.MovementCriteria
            .Where(c => c.TargetType == "Club" && c.IsActive)
            .Select(c => new LocalCriterionVM
            {
                Id = c.Id,
                GroupId = c.GroupId,
                Title = c.Title,
                MinScore = c.MinScore ?? 0,
                MaxScore = c.MaxScore
            })
            .OrderBy(c => c.Title)
            .ToListAsync();
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
        foreach (var cookie in Request.Cookies)
        {
            handler.CookieContainer.Add(
                new Uri(apiBaseUrl),
                new Cookie(cookie.Key, cookie.Value)
            );
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiBaseUrl)
        };

        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
        );

        return client;
    }

    public class LocalGroupVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxScore { get; set; }
    }

    public class LocalCriterionVM
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
    }
}
