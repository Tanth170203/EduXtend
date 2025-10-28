using BusinessObject.DTOs.ClubMovementRecord;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Services.ClubMovementRecords;
using System.Security.Claims;

namespace WebFE.Pages.Admin.ClubScoring;

public class DetailModel : PageModel
{
    private readonly IClubScoringService _service;
    private readonly EduXtendContext _context;

    public DetailModel(IClubScoringService service, EduXtendContext context)
    {
        _service = service;
        _context = context;
    }

    public ClubMovementRecordDto? Record { get; set; }
    public List<ClubCategoryScoreDto> CategoryScores { get; set; } = new();
    public List<LocalCriterionVM> ManualCriteria { get; set; } = new();
    public int ClubId { get; set; }
    public int SemesterId { get; set; }
    public int Month { get; set; }

    [BindProperty]
    public AddClubManualScoreDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int clubId, int semesterId, int month)
    {
        ClubId = clubId;
        SemesterId = semesterId;
        Month = month;

        Record = await _service.GetClubScoreAsync(clubId, semesterId, month);
        if (Record == null)
        {
            // Make sure semester name is available even if no record yet
            var sem = await _context.Semesters.FindAsync(semesterId);
            Record = new ClubMovementRecordDto
            {
                ClubId = clubId,
                SemesterId = semesterId,
                SemesterName = sem?.Name ?? string.Empty,
                Month = month,
                ClubName = (await _context.Clubs.FindAsync(clubId))?.Name ?? string.Empty
            };
        }

        // Build category summary from MovementCriterionGroups for Club
        var groups = await _context.MovementCriterionGroups
            .Where(g => g.TargetType == "Club")
            .ToListAsync();

        foreach (var g in groups)
        {
            var current = Record.Details
                .Where(d => d.GroupName == g.Name)
                .Sum(d => d.Score);
            CategoryScores.Add(new ClubCategoryScoreDto
            {
                CategoryName = g.Name,
                CurrentScore = current,
                MaxScore = g.MaxScore
            });
        }

        // Load manual criteria (range) for Club
        ManualCriteria = await _context.MovementCriteria
            .Where(c => c.TargetType == "Club" && c.MinScore != null && c.IsActive)
            .Select(c => new LocalCriterionVM
            {
                Id = c.Id,
                Title = c.Title,
                MinScore = c.MinScore ?? 0,
                MaxScore = c.MaxScore
            })
            .OrderBy(c => c.Title)
            .ToListAsync();

        // preset bind values
        Input.ClubId = clubId;
        Input.SemesterId = semesterId;
        Input.Month = month;

        return Page();
    }

    public async Task<IActionResult> OnPostAddManualAsync()
    {
        // basic validation
        if (Input.CriterionId <= 0 || Input.Score <= 0 || string.IsNullOrWhiteSpace(Input.Note))
        {
            ModelState.AddModelError(string.Empty, "Thiếu thông tin cần thiết");
            await OnGetAsync(Input.ClubId, Input.SemesterId, Input.Month);
            return Page();
        }

        // get admin user id from claims if available
        int adminId = 0;
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var uid)) adminId = uid;

        await _service.AddManualScoreAsync(Input);
        return RedirectToPage(new { clubId = Input.ClubId, semesterId = Input.SemesterId, month = Input.Month });
    }

    public class LocalCriterionVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
    }
}


