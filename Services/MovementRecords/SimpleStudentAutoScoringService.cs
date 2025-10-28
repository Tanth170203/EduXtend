using BusinessObject.Models;
using BusinessObject.Enum;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.MovementRecords;

/// <summary>
/// Simplified auto-scoring service for students only
/// Based on Decision 414 - FPT University
/// </summary>
public class SimpleStudentAutoScoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SimpleStudentAutoScoringService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public SimpleStudentAutoScoringService(
        IServiceProvider serviceProvider,
        ILogger<SimpleStudentAutoScoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Simple Student Auto-Scoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Starting student scoring cycle...");
                
                await ProcessStudentScoresAsync();
                
                _logger.LogInformation("✅ Student scoring cycle completed");
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in student scoring cycle");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Simple Student Auto-Scoring Service stopped");
    }

    public async Task ProcessStudentScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            // 1. Academic Awareness (Ý thức học tập)
            await ProcessAcademicScoresAsync(dbContext);
            
            // 2. Social Activities (Hoạt động chính trị - xã hội)
            await ProcessSocialActivityScoresAsync(dbContext);
            
            // 3. Civic Qualities (Phẩm chất công dân)
            await ProcessCivicQualityScoresAsync(dbContext);
            
            // 4. Recalculate all totals
            await RecalculateAllStudentScoresAsync(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing student scores");
        }
    }

    #region 1. Academic Awareness Scoring (Ý thức học tập)

    private async Task ProcessAcademicScoresAsync(EduXtendContext dbContext)
    {
        try
        {
            _logger.LogInformation("📚 Processing academic awareness scores...");

            // 1.1. National/International competitions
            await ProcessNationalCompetitionScoresAsync(dbContext);
            
            // 1.2. School-level competitions
            await ProcessSchoolCompetitionScoresAsync(dbContext);

            _logger.LogInformation("✅ Academic awareness scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing academic scores");
        }
    }

    private async Task ProcessNationalCompetitionScoresAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        // Get national/international competitions
        var competitions = await dbContext.Activities
            .Where(a => a.Type == ActivityType.NationalCompetition &&
                       a.Status == "Completed" &&
                       a.StartTime >= currentSemester.StartDate &&
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Thi Olympic/ACM/ICPC");
        if (criterion == null) return;

        foreach (var competition in competitions)
        {
            await ProcessActivityAttendanceAsync(dbContext, competition, criterion, 10);
        }
    }

    private async Task ProcessSchoolCompetitionScoresAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        // Get school-level competitions
        var competitions = await dbContext.Activities
            .Where(a => a.Type == ActivityType.SchoolCompetition &&
                       a.Status == "Completed" &&
                       a.StartTime >= currentSemester.StartDate &&
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Thi cấp trường");
        if (criterion == null) return;

        foreach (var competition in competitions)
        {
            await ProcessActivityAttendanceAsync(dbContext, competition, criterion, 5);
        }
    }

    #endregion

    #region 2. Social Activities Scoring (Hoạt động chính trị - xã hội)

    private async Task ProcessSocialActivityScoresAsync(EduXtendContext dbContext)
    {
        try
        {
            _logger.LogInformation("🎭 Processing social activity scores...");

            // 2.1. Event participation
            await ProcessEventParticipationScoresAsync(dbContext);
            
            // 2.2. Club membership - DISABLED: Now managed by Club Manager manually
            // Club Manager will evaluate and submit scores to Admin
            // await ProcessClubMembershipScoresAsync(dbContext);
            
            // 2.3. Volunteer activities
            await ProcessVolunteerActivityScoresAsync(dbContext);

            _logger.LogInformation("✅ Social activity scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing social activity scores");
        }
    }

    private async Task ProcessEventParticipationScoresAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        var events = await dbContext.Activities
            .Where(a => a.Status == "Completed" && 
                       a.MovementPoint > 0 &&
                       a.StartTime >= currentSemester.StartDate &&
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Tham gia sự kiện");
        if (criterion == null) return;

        foreach (var evt in events)
        {
            await ProcessActivityAttendanceAsync(dbContext, evt, criterion, (int)evt.MovementPoint);
        }
    }

    private async Task ProcessClubMembershipScoresAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        var clubMembers = await dbContext.ClubMembers
            .Include(cm => cm.Student)
            .Where(cm => cm.IsActive)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Thành viên CLB");
        if (criterion == null) return;

        foreach (var member in clubMembers)
        {
            var score = CalculateClubMemberScore(member.RoleInClub);
            await AddMovementScoreAsync(dbContext, member.StudentId, currentSemester.Id, criterion.Id, score);
        }
    }

    private async Task ProcessVolunteerActivityScoresAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        var volunteerActivities = await dbContext.Activities
            .Where(a => a.Type == ActivityType.Volunteer &&
                       a.Status == "Completed" &&
                       a.StartTime >= currentSemester.StartDate &&
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Hoạt động tình nguyện");
        if (criterion == null) return;

        foreach (var activity in volunteerActivities)
        {
            await ProcessActivityAttendanceAsync(dbContext, activity, criterion, 5);
        }
    }

    #endregion

    #region 3. Civic Qualities Scoring (Phẩm chất công dân)

    private async Task ProcessCivicQualityScoresAsync(EduXtendContext dbContext)
    {
        try
        {
            _logger.LogInformation("🏛️ Processing civic quality scores...");

            // Process approved evidences for civic qualities
            await ProcessCivicQualityEvidencesAsync(dbContext);

            _logger.LogInformation("✅ Civic quality scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing civic quality scores");
        }
    }

    private async Task ProcessCivicQualityEvidencesAsync(EduXtendContext dbContext)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        var civicEvidences = await dbContext.Evidences
            .Include(e => e.Student)
            .Where(e => e.Status == "Approved" && 
                       (e.Title.Contains("hành vi tốt") || 
                        e.Title.Contains("giúp đỡ") || 
                        e.Title.Contains("trả của rơi")))
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Hành vi tốt");
        if (criterion == null) return;

        foreach (var evidence in civicEvidences)
        {
            await AddMovementScoreAsync(dbContext, evidence.StudentId, currentSemester.Id, criterion.Id, 5);
        }
    }

    #endregion

    #region Helper Methods

    private async Task<MovementCriterion?> GetCriterionAsync(EduXtendContext dbContext, string title)
    {
        return await dbContext.MovementCriteria
            .FirstOrDefaultAsync(c => c.Title.Contains(title) && c.IsActive);
    }

    private async Task<Semester?> GetCurrentSemesterAsync(EduXtendContext dbContext)
    {
        return await dbContext.Semesters
            .FirstOrDefaultAsync(s => s.IsActive);
    }

    private async Task ProcessActivityAttendanceAsync(EduXtendContext dbContext, Activity activity, MovementCriterion criterion, int baseScore)
    {
        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        foreach (var attendance in activity.Attendances.Where(a => a.IsPresent))
        {
            var student = await dbContext.Students
                .FirstOrDefaultAsync(s => s.UserId == attendance.UserId);
            
            if (student != null)
            {
                await AddMovementScoreAsync(dbContext, student.Id, currentSemester.Id, criterion.Id, baseScore);
            }
        }
    }

    private async Task AddMovementScoreAsync(EduXtendContext dbContext, int studentId, int semesterId, int criterionId, double score)
    {
        // Get criterion to check MaxScore
        var criterion = await dbContext.MovementCriteria
            .FirstOrDefaultAsync(c => c.Id == criterionId);
        
        if (criterion == null)
        {
            _logger.LogWarning("Criterion {CriterionId} not found", criterionId);
            return;
        }

        // Check if score already exists for this criterion this month
        var existingScore = await dbContext.MovementRecordDetails
            .Include(mrd => mrd.MovementRecord)
            .Where(mrd => mrd.MovementRecord.StudentId == studentId &&
                         mrd.MovementRecord.SemesterId == semesterId &&
                         mrd.CriterionId == criterionId &&
                         mrd.AwardedAt.Month == DateTime.UtcNow.Month &&
                         mrd.AwardedAt.Year == DateTime.UtcNow.Year)
            .FirstOrDefaultAsync();

        if (existingScore != null)
        {
            _logger.LogInformation("Score already exists for student {StudentId}, criterion {CriterionId}", studentId, criterionId);
            return;
        }

        // Get or create movement record
        var record = await dbContext.MovementRecords
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);

        if (record == null)
        {
            record = new MovementRecord
            {
                StudentId = studentId,
                SemesterId = semesterId,
                TotalScore = 0,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.MovementRecords.Add(record);
            await dbContext.SaveChangesAsync();
        }

        // Add score detail
        var detail = new MovementRecordDetail
        {
            MovementRecordId = record.Id,
            CriterionId = criterionId,
            Score = Math.Min(score, criterion.MaxScore),
            AwardedAt = DateTime.UtcNow
        };

        dbContext.MovementRecordDetails.Add(detail);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Added {Score} points to student {StudentId} for criterion {CriterionId}", 
            score, studentId, criterionId);
    }

    private double CalculateClubMemberScore(string role)
    {
        return role switch
        {
            "President" => 10,
            "VicePresident" => 8,
            "Manager" => 5,
            "Member" => 3,
            _ => 1
        };
    }

    private async Task RecalculateAllStudentScoresAsync(EduXtendContext dbContext)
    {
        try
        {
            var records = await dbContext.MovementRecords
                .Include(r => r.Details)
                .ToListAsync();

            foreach (var record in records)
            {
                var totalScore = record.Details.Sum(d => d.Score);
                record.TotalScore = Math.Min(totalScore, 140); // Cap at 140 as per Decision 414
                record.LastUpdated = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Recalculated {Count} student movement records", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating student scores");
        }
    }

    #endregion
}
