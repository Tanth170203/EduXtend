using BusinessObject.DTOs.ClubMovementRecord;
using DataAccess;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Services.ClubMovementRecords;

namespace WebFE.Pages.Admin.ClubScoring;

public class IndexModel : PageModel
{
    private readonly EduXtendContext _context;
    private readonly IClubScoringService _service;
    private readonly IConfiguration _config;

    public IndexModel(EduXtendContext context, IClubScoringService service, IConfiguration config)
    {
        _context = context;
        _service = service;
        _config = config;
    }

    public List<ClubMovementRecordDto> ClubScores { get; set; } = new();
    public List<BusinessObject.Models.Semester> Semesters { get; set; } = new();

    public int SelectedSemesterId { get; set; }
    public int SelectedMonth { get; set; }

    // Dashboard KPIs
    public int TotalClubs { get; set; }
    public int ScoredClubs { get; set; }
    public int UnscoredClubs { get; set; }
    public List<ClubMovementRecordDto> Top5 { get; set; } = new();
    public List<MonthOption> MonthOptions { get; set; } = new();
    public string ExportUrl { get; set; } = string.Empty;

    public async Task OnGetAsync(int? semesterId, int? month)
    {
        Semesters = await _context.Semesters
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var active = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
        SelectedSemesterId = semesterId ?? active?.Id ?? Semesters.FirstOrDefault()?.Id ?? 0;

        if (SelectedSemesterId != 0)
        {
            var sem = await _context.Semesters.FindAsync(SelectedSemesterId);
            if (sem != null)
            {
                var cursor = new DateTime(sem.StartDate.Year, sem.StartDate.Month, 1);
                var endCap = new DateTime(sem.EndDate.Year, sem.EndDate.Month, 1);
                MonthOptions.Clear();
                while (cursor <= endCap)
                {
                    MonthOptions.Add(new MonthOption
                    {
                        Month = cursor.Month,
                        Year = cursor.Year,
                        Display = $"Tháng {cursor.Month}/{cursor.Year}"
                    });
                    cursor = cursor.AddMonths(1);
                }

                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                bool inRange = MonthOptions.Any(mo => mo.Month == currentMonth && mo.Year == currentYear);
                SelectedMonth = month ?? (inRange ? currentMonth : MonthOptions.First().Month);
            }
            else
            {
                SelectedMonth = month ?? DateTime.UtcNow.Month;
            }
        }
        else
        {
            SelectedMonth = month ?? DateTime.UtcNow.Month;
        }

        if (SelectedSemesterId != 0)
        {
            // Get scores from service
            var scores = await _service.GetAllClubScoresAsync(SelectedSemesterId, SelectedMonth);
            
            // Get all active clubs
            var allClubs = await _context.Clubs
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            // Merge: show all clubs, with scores if available
            var scoredClubIds = scores.Select(s => s.ClubId).ToHashSet();
            
            ClubScores = allClubs.Select(club =>
            {
                var existingScore = scores.FirstOrDefault(s => s.ClubId == club.Id);
                if (existingScore != null)
                    return existingScore;
                
                // Return club with 0 scores if not scored yet
                return new ClubMovementRecordDto
                {
                    ClubId = club.Id,
                    ClubName = club.Name,
                    ClubMeetingScore = 0,
                    EventScore = 0,
                    CollaborationScore = 0,
                    CompetitionScore = 0,
                    PlanScore = 0,
                    TotalScore = 0
                };
            }).OrderByDescending(c => c.TotalScore).ThenBy(c => c.ClubName).ToList();

            // KPIs
            TotalClubs = allClubs.Count;
            ScoredClubs = ClubScores.Count(r => r.TotalScore > 0 ||
                (r.ClubMeetingScore + r.EventScore + r.CompetitionScore + r.PlanScore + r.CollaborationScore) > 0);
            UnscoredClubs = Math.Max(0, TotalClubs - ScoredClubs);

            Top5 = ClubScores
                .OrderByDescending(r => r.TotalScore)
                .Take(5)
                .ToList();

            // Build export URL to WebAPI base
            var apiBase = _config["ApiSettings:BaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrEmpty(apiBase))
            {
                ExportUrl = $"{apiBase}/api/reports/club-monthly?semesterId={SelectedSemesterId}";
            }
        }
    }
    public class MonthOption
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}


