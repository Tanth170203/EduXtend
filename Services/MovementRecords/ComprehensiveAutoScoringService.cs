using BusinessObject.Models;
using BusinessObject.Enum;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.MovementRecords;

/// <summary>
/// Comprehensive auto-scoring service based on Decision 414 - FPT University
/// Covers all 4 categories: Academic, Social Activities, Civic Qualities, Organizational Work
/// </summary>
public class ComprehensiveAutoScoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComprehensiveAutoScoringService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public ComprehensiveAutoScoringService(
        IServiceProvider serviceProvider,
        ILogger<ComprehensiveAutoScoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Comprehensive Auto-Scoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔄 Starting comprehensive scoring cycle...");
                
                // 1. Academic Awareness (Ý thức học tập)
                await ProcessAcademicScoresAsync();
                
                // 2. Social Activities (Hoạt động chính trị - xã hội)
                await ProcessSocialActivityScoresAsync();
                
                // 3. Civic Qualities (Phẩm chất công dân)
                await ProcessCivicQualityScoresAsync();
                
                // 4. Organizational Work (Công tác tổ chức)
                await ProcessOrganizationalScoresAsync();
                
                // 5. Club Scoring (Chấm điểm CLB)
                await ProcessClubScoresAsync();
                
                // 6. Recalculate all totals
                await RecalculateAllStudentScoresAsync();
                await RecalculateAllClubScoresAsync();
                
                _logger.LogInformation("✅ Comprehensive scoring cycle completed");
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in comprehensive scoring cycle");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Comprehensive Auto-Scoring Service stopped");
    }

    #region 1. Academic Awareness Scoring (Ý thức học tập)

    private async Task ProcessAcademicScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("📚 Processing academic awareness scores...");

            // 1.1. Competition participation (Olympic, ACM, etc.)
            await ProcessCompetitionScoresAsync(dbContext);
            
            // 1.2. School-level competitions
            await ProcessSchoolCompetitionScoresAsync(dbContext);
            
            // Note: Manual entries (teacher commendations) need to be entered manually
            // This is handled by Evidence system with manual approval

            _logger.LogInformation("✅ Academic awareness scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing academic scores");
        }
    }

    private async Task ProcessCompetitionScoresAsync(EduXtendContext dbContext)
    {
        // Get national/international competitions
        var competitions = await dbContext.Activities
            .Where(a => a.Type == ActivityType.NationalCompetition)
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
        // Get school-level competitions
        var competitions = await dbContext.Activities
            .Where(a => a.Type == ActivityType.SchoolCompetition)
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

    private async Task ProcessSocialActivityScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

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
        var events = await dbContext.Activities
            .Where(a => a.Status == "Completed" && a.MovementPoint > 0)
            .Include(a => a.Attendances)
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Tham gia sự kiện");
        if (criterion == null) return;

        foreach (var evt in events)
        {
            // Calculate score based on event size and attendance
            var attendanceRate = evt.Attendances.Count(a => a.IsPresent) / (double)evt.Attendances.Count;
            var baseScore = attendanceRate >= 0.7 ? evt.MovementPoint : evt.MovementPoint * 0.5;
            
            await ProcessActivityAttendanceAsync(dbContext, evt, criterion, (int)baseScore);
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
        var volunteerActivities = await dbContext.Activities
            .Where(a => a.Type == ActivityType.Volunteer)
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

    private async Task ProcessCivicQualityScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("🏛️ Processing civic quality scores...");

            // 3.1. Good deeds (manual entry via Evidence system)
            // 3.2. Social activities (already covered in social activities)
            
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
        var civicEvidences = await dbContext.Evidences
            .Include(e => e.Student)
            .Where(e => e.Status == "Approved" && 
                       (e.Title.Contains("hành vi tốt") || 
                        e.Title.Contains("giúp đỡ") || 
                        e.Title.Contains("trả của rơi")))
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Hành vi tốt");
        if (criterion == null) return;

        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        foreach (var evidence in civicEvidences)
        {
            await AddMovementScoreAsync(dbContext, evidence.StudentId, currentSemester.Id, criterion.Id, 5);
        }
    }

    #endregion

    #region 4. Organizational Work Scoring (Công tác tổ chức)

    private async Task ProcessOrganizationalScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("👥 Processing organizational work scores...");

            // 4.1. Club leadership
            await ProcessClubLeadershipScoresAsync(dbContext);
            
            // 4.2. Event organization
            await ProcessEventOrganizationScoresAsync(dbContext);

            _logger.LogInformation("✅ Organizational work scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing organizational scores");
        }
    }

    private async Task ProcessClubLeadershipScoresAsync(EduXtendContext dbContext)
    {
        var clubLeaders = await dbContext.ClubMembers
            .Include(cm => cm.Student)
            .Where(cm => cm.IsActive && 
                        (cm.RoleInClub == "President" || 
                         cm.RoleInClub == "VicePresident" ||
                         cm.RoleInClub == "Manager"))
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Chủ nhiệm CLB");
        if (criterion == null) return;

        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        foreach (var leader in clubLeaders)
        {
            var score = leader.RoleInClub switch
            {
                "President" => 10,
                "VicePresident" => 8,
                "Manager" => 5,
                _ => 3
            };

            await AddMovementScoreAsync(dbContext, leader.StudentId, currentSemester.Id, criterion.Id, score);
        }
    }

    private async Task ProcessEventOrganizationScoresAsync(EduXtendContext dbContext)
    {
        // Get students who organized events
        var eventOrganizers = await dbContext.Activities
            .Where(a => a.CreatedById != null)
            .Include(a => a.CreatedBy)
            .Select(a => new { a.CreatedById, a.Title, a.MaxParticipants })
            .ToListAsync();

        var criterion = await GetCriterionAsync(dbContext, "Chủ nhiệm CLB"); // Reuse criterion
        if (criterion == null) return;

        var currentSemester = await GetCurrentSemesterAsync(dbContext);
        if (currentSemester == null) return;

        foreach (var organizer in eventOrganizers)
        {
            // Calculate score based on event size
            var score = organizer.MaxParticipants switch
            {
                > 200 => 10,
                > 100 => 8,
                > 50 => 5,
                _ => 3
            };

            // Get student from user
            var student = await dbContext.Students
                .FirstOrDefaultAsync(s => s.UserId == organizer.CreatedById);
            
            if (student != null)
            {
                await AddMovementScoreAsync(dbContext, student.Id, currentSemester.Id, criterion.Id, score);
            }
        }
    }

    #endregion

    #region 5. Club Scoring (Chấm điểm CLB)

    private async Task ProcessClubScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("🏆 Processing club scores...");

            var clubs = await dbContext.Clubs
                .Include(c => c.Activities)
                    .ThenInclude(a => a.Attendances)
                .Where(c => c.IsActive)
                .ToListAsync();

            foreach (var club in clubs)
            {
                await ProcessClubScoreAsync(dbContext, club);
            }

            _logger.LogInformation("✅ Club scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing club scores");
        }
    }

    private async Task ProcessClubScoreAsync(EduXtendContext dbContext, Club club)
    {
        // Calculate club score based on activities organized
        var totalScore = 0.0;

        foreach (var activity in club.Activities.Where(a => a.Status == "Completed"))
        {
            var attendanceCount = activity.Attendances.Count(a => a.IsPresent);
            var attendanceRate = attendanceCount / (double)activity.MaxParticipants;

            var score = attendanceCount switch
            {
                > 200 when attendanceRate >= 0.7 => 20, // Large event
                > 100 when attendanceRate >= 0.7 => 15, // Medium event
                > 50 when attendanceRate >= 0.7 => 5,   // Small event
                _ => 0
            };

            totalScore += score;
        }

        // Add collaboration scores
        var collaborationScore = await CalculateClubCollaborationScoreAsync(dbContext, club.Id);
        totalScore += collaborationScore;

        // Cap at 100 points
        totalScore = Math.Min(totalScore, 100);

        _logger.LogInformation("🏆 Club {ClubName} scored {Score} points", club.Name, totalScore);
    }

    private async Task<double> CalculateClubCollaborationScoreAsync(EduXtendContext dbContext, int clubId)
    {
        // Calculate collaboration with other clubs
        var collaborations = await dbContext.Activities
            .Where(a => a.ClubId == clubId && a.Title.Contains("phối hợp"))
            .CountAsync();

        return collaborations * 5; // 5 points per collaboration
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

    private async Task RecalculateAllStudentScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

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

    private async Task RecalculateAllClubScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            // This would be implemented when Club scoring system is fully developed
            _logger.LogInformation("Club score recalculation not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating club scores");
        }
    }

    #endregion
}
