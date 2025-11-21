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
        try
        {
            _logger.LogInformation("🎯 Processing event participation scores...");

            // Get activities that are completed and have movement points
            var completedActivities = await dbContext.Activities
                .Include(a => a.Attendances)
                    .ThenInclude(att => att.User)
            .Where(a => a.Status == "Completed" && a.MovementPoint > 0)
            .ToListAsync();

            _logger.LogInformation("Found {Count} completed activities with movement points", completedActivities.Count);

            foreach (var activity in completedActivities)
            {
                await ProcessActivityAttendanceWithGroupCapAsync(dbContext, activity);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Finished processing event participation scores");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing event participation scores");
        }
    }

    private async Task ProcessActivityAttendanceWithGroupCapAsync(EduXtendContext dbContext, Activity activity)
    {
        try
        {
            // Get current semester
            var currentSemester = await GetCurrentSemesterAsync(dbContext);
            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found");
                return;
            }

            // Decision 414 - Social activities: điểm sự kiện (3–5 điểm/sự kiện)
            // Criterion title: "Tham gia sự kiện"
            var activityCriterion = await dbContext.MovementCriteria
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Title.Contains("Tham gia sự kiện") && c.IsActive);

            if (activityCriterion == null)
            {
                _logger.LogWarning("No criterion found for activity participation");
                return;
            }

            // Skip competitions here; they are processed in dedicated routines
            if (activity.Type == BusinessObject.Enum.ActivityType.SchoolCompetition
                || activity.Type == BusinessObject.Enum.ActivityType.NationalCompetition
                || activity.Type == BusinessObject.Enum.ActivityType.ProvincialCompetition)
            {
                return; // handled elsewhere
            }

            // Process each attendance for normal events (MovementPoint is expected 3–5 per event)
            foreach (var attendance in activity.Attendances.Where(a => a.IsPresent))
            {
                // Get student from UserId
                var student = await dbContext.Students
                    .FirstOrDefaultAsync(s => s.UserId == attendance.UserId);
                if (student == null)
                    continue;

                // Get or create movement record
                var record = await dbContext.MovementRecords
                    .Include(r => r.Details)
                    .FirstOrDefaultAsync(r => r.StudentId == student.Id && r.SemesterId == currentSemester.Id);

                if (record == null)
                {
                    record = new MovementRecord
                    {
                        StudentId = student.Id,
                        SemesterId = currentSemester.Id,
                        TotalScore = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.MovementRecords.Add(record);
                    await dbContext.SaveChangesAsync(); // Save to get the ID
                }

                // Check if score already added for THIS SPECIFIC ACTIVITY (by ActivityId)
                // NOTE: We allow multiple scores for same criterion (e.g., multiple competitions, events)
                // But we should not duplicate score for the SAME activity
                var existingDetail = await dbContext.MovementRecordDetails
                    .AnyAsync(d => d.MovementRecordId == record.Id 
                                && d.CriterionId == activityCriterion.Id
                                && d.ActivityId == activity.Id);

                if (!existingDetail)
                {
                    if (activityCriterion.Group == null)
                    {
                        _logger.LogWarning("Criterion {CriterionId} has no group", activityCriterion.Id);
                        continue;
                    }

                    // Calculate current GROUP score
                    var currentGroupScore = await dbContext.MovementRecordDetails
                        .Include(d => d.Criterion)
                        .Where(d => d.MovementRecordId == record.Id 
                                 && d.Criterion.GroupId == activityCriterion.GroupId)
                        .SumAsync(d => d.Score);

                    var groupMaxScore = activityCriterion.Group.MaxScore;

                    // Check if already reached group max score
                    if (currentGroupScore >= groupMaxScore)
                    {
                        _logger.LogInformation(
                            "Student {StudentId} already reached max score {MaxScore} for group {GroupName}. Skipping activity {ActivityTitle}",
                            student.Id, groupMaxScore, activityCriterion.Group.Name, activity.Title);
                        continue;
                    }

                    // Calculate score to add (movementPoint should be 3–5 per Decision 414 for event)
                    var scoreToAdd = Math.Min(activity.MovementPoint, groupMaxScore - currentGroupScore);

                    // Add score detail
                    var detail = new MovementRecordDetail
                    {
                        MovementRecordId = record.Id,
                        CriterionId = activityCriterion.Id,
                        ActivityId = activity.Id,
                        Score = scoreToAdd,
                        AwardedAt = DateTime.UtcNow
                    };
                    dbContext.MovementRecordDetails.Add(detail);

                    // Recalculate total score (sum all details, cap at 100)
                    var totalScore = await dbContext.MovementRecordDetails
                        .Where(d => d.MovementRecordId == record.Id)
                        .SumAsync(d => d.Score);

                    record.TotalScore = Math.Min(totalScore, 100); // Cap at 100 total
                    record.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Added {Points} points to student {StudentId} for activity {ActivityTitle} (Group: {GroupName}, Current: {CurrentScore}/{MaxScore})",
                        scoreToAdd, student.Id, activity.Title, activityCriterion.Group.Name, 
                        currentGroupScore + scoreToAdd, groupMaxScore);
                }
                else
                {
                    _logger.LogDebug("Score already added for student {StudentId} for activity {ActivityTitle}",
                        student.Id, activity.Title);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity {ActivityId}", activity.Id);
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
        // Volunteer activity type has been removed
        // This method is kept for backward compatibility but does nothing
        await Task.CompletedTask;
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

    /// <summary>
    /// Auto-scoring for Clubs based on activities within current month and semester
    /// - ClubMeeting: 5 points per week (max 20)
    /// - Events: Large(20) if >=70% attendance; Small(15) if >=70%; Internal(5)
    /// - Competitions: Provincial(20), National(30)
    /// - Plan: handled via manual toggle separately (10)
    /// </summary>
    private async Task ProcessClubScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("🏆 Processing club auto-scoring...");

            var currentSemester = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found for club auto-scoring");
                return;
            }

            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month; // e.g., 9..12

            // Get all active clubs
            var clubs = await db.Clubs.Where(c => c.IsActive).ToListAsync();

            foreach (var club in clubs)
            {
                try
                {
                    // Get or create monthly club record
                    var record = await db.ClubMovementRecords
                        .Include(r => r.Details)
                        .FirstOrDefaultAsync(r => r.ClubId == club.Id && r.SemesterId == currentSemester.Id && r.Month == month);

                    if (record == null)
                    {
                        record = new BusinessObject.Models.ClubMovementRecord
                        {
                            ClubId = club.Id,
                            SemesterId = currentSemester.Id,
                            Month = month,
                            CreatedAt = DateTime.UtcNow,
                        };
                        db.ClubMovementRecords.Add(record);
                        await db.SaveChangesAsync();
                    }

                    // Load criteria used
                    var critClubMeeting = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Sinh hoạt CLB") && c.TargetType == "Club" && c.IsActive);
                    var critLargeEvent = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Sự kiện lớn") && c.TargetType == "Club" && c.IsActive);
                    var critSmallEvent = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Sự kiện nhỏ") && c.TargetType == "Club" && c.IsActive);
                    var critInternalEvent = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Sự kiện nội bộ") && c.TargetType == "Club" && c.IsActive);
                    var critProvincial = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Tỉnh/TP") && c.TargetType == "Club" && c.IsActive);
                    var critNational = await db.MovementCriteria.FirstOrDefaultAsync(c => c.Title.Contains("Quốc gia") && c.TargetType == "Club" && c.IsActive);

                    // Month range
                    var monthStart = new DateTime(year, month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                    // 1) Club Meetings (5 points per week, max 20)
                    var meetings = await db.Activities
                        .Where(a => a.ClubId == club.Id
                                 && a.Type == BusinessObject.Enum.ActivityType.ClubMeeting
                                 && a.Status == "Completed"
                                 && a.EndTime >= monthStart && a.EndTime <= monthEnd)
                        .Include(a => a.Attendances)
                        .ToListAsync();

                    var weeks = meetings
                        .Select(a => System.Globalization.ISOWeek.GetWeekOfYear(a.EndTime))
                        .Distinct()
                        .Count();

                    // Each club meeting week gives 5 points; no upper cap per framework
                    var clubMeetingScore = weeks * 5;

                    // Remove previous auto details for club meeting in this month to idempotently recalc
                    if (critClubMeeting != null)
                    {
                        var oldMeetDetails = await db.ClubMovementRecordDetails
                            .Where(d => d.ClubMovementRecordId == record.Id && d.CriterionId == critClubMeeting.Id && d.ScoreType == "Auto")
                            .ToListAsync();
                        if (oldMeetDetails.Any())
                        {
                            db.ClubMovementRecordDetails.RemoveRange(oldMeetDetails);
                        }

                        if (clubMeetingScore > 0)
                        {
                            db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                            {
                                ClubMovementRecordId = record.Id,
                                CriterionId = critClubMeeting.Id,
                                Score = clubMeetingScore,
                                ScoreType = "Auto",
                                AwardedAt = DateTime.UtcNow
                            });
                        }
                    }

                    // 2) Events: Large (20) if >=70%; Small (15) if >=70%; Internal (5)
                    var events = await db.Activities
                        .Where(a => a.ClubId == club.Id
                                 && (a.Type == BusinessObject.Enum.ActivityType.LargeEvent
                                  || a.Type == BusinessObject.Enum.ActivityType.MediumEvent
                                  || a.Type == BusinessObject.Enum.ActivityType.SmallEvent)
                                 && a.Status == "Completed"
                                 && a.EndTime >= monthStart && a.EndTime <= monthEnd)
                        .Include(a => a.Attendances)
                        .ToListAsync();

                    // Clean previous auto event details
                    if (critLargeEvent != null || critSmallEvent != null || critInternalEvent != null)
                    {
                        var eventCritIds = new List<int>();
                        if (critLargeEvent != null) eventCritIds.Add(critLargeEvent.Id);
                        if (critSmallEvent != null) eventCritIds.Add(critSmallEvent.Id);
                        if (critInternalEvent != null) eventCritIds.Add(critInternalEvent.Id);

                        var oldEventDetails = await db.ClubMovementRecordDetails
                            .Where(d => d.ClubMovementRecordId == record.Id && eventCritIds.Contains(d.CriterionId) && d.ScoreType == "Auto")
                            .ToListAsync();
                        if (oldEventDetails.Any()) db.ClubMovementRecordDetails.RemoveRange(oldEventDetails);
                    }

                    foreach (var ev in events)
                    {
                        double? rate = null;
                        if (ev.MaxParticipants.HasValue && ev.MaxParticipants.Value > 0)
                        {
                            var present = ev.Attendances.Count(x => x.IsPresent);
                            rate = (double)present / ev.MaxParticipants.Value;
                        }

                        if (ev.Type == BusinessObject.Enum.ActivityType.LargeEvent)
                        {
                            if (critLargeEvent != null && rate.HasValue && rate.Value >= 0.7)
                            {
                                db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                                {
                                    ClubMovementRecordId = record.Id,
                                    CriterionId = critLargeEvent.Id,
                                    ActivityId = ev.Id,
                                    Score = 20,
                                    ScoreType = "Auto",
                                    AwardedAt = DateTime.UtcNow
                                });
                            }
                            else if (critInternalEvent != null && (!rate.HasValue || rate.Value < 0.7))
                            {
                                // fallback as internal if no 70%
                                db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                                {
                                    ClubMovementRecordId = record.Id,
                                    CriterionId = critInternalEvent.Id,
                                    ActivityId = ev.Id,
                                    Score = 5,
                                    ScoreType = "Auto",
                                    AwardedAt = DateTime.UtcNow
                                });
                            }
                        }
                        else if (ev.Type == BusinessObject.Enum.ActivityType.MediumEvent)
                        {
                            if (critSmallEvent != null && rate.HasValue && rate.Value >= 0.7)
                            {
                                db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                                {
                                    ClubMovementRecordId = record.Id,
                                    CriterionId = critSmallEvent.Id,
                                    ActivityId = ev.Id,
                                    Score = 15,
                                    ScoreType = "Auto",
                                    AwardedAt = DateTime.UtcNow
                                });
                            }
                            else if (critInternalEvent != null && (!rate.HasValue || rate.Value < 0.7))
                            {
                                db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                                {
                                    ClubMovementRecordId = record.Id,
                                    CriterionId = critInternalEvent.Id,
                                    ActivityId = ev.Id,
                                    Score = 5,
                                    ScoreType = "Auto",
                                    AwardedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    // 3) Competitions
                    var competitions = await db.Activities
                        .Where(a => a.ClubId == club.Id
                                 && (a.Type == BusinessObject.Enum.ActivityType.ProvincialCompetition
                                  || a.Type == BusinessObject.Enum.ActivityType.NationalCompetition)
                                 && a.Status == "Completed"
                                 && a.EndTime >= monthStart && a.EndTime <= monthEnd)
                        .ToListAsync();

                    // Clean previous competition auto details
                    if (critProvincial != null || critNational != null)
                    {
                        var compCritIds = new List<int>();
                        if (critProvincial != null) compCritIds.Add(critProvincial.Id);
                        if (critNational != null) compCritIds.Add(critNational.Id);
                        var oldCompDetails = await db.ClubMovementRecordDetails
                            .Where(d => d.ClubMovementRecordId == record.Id && compCritIds.Contains(d.CriterionId) && d.ScoreType == "Auto")
                            .ToListAsync();
                        if (oldCompDetails.Any()) db.ClubMovementRecordDetails.RemoveRange(oldCompDetails);
                    }

                    foreach (var comp in competitions)
                    {
                        if (comp.Type == BusinessObject.Enum.ActivityType.ProvincialCompetition && critProvincial != null)
                        {
                            db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                            {
                                ClubMovementRecordId = record.Id,
                                CriterionId = critProvincial.Id,
                                ActivityId = comp.Id,
                                Score = 20,
                                ScoreType = "Auto",
                                AwardedAt = DateTime.UtcNow
                            });
                        }
                        else if (comp.Type == BusinessObject.Enum.ActivityType.NationalCompetition && critNational != null)
                        {
                            db.ClubMovementRecordDetails.Add(new BusinessObject.Models.ClubMovementRecordDetail
                            {
                                ClubMovementRecordId = record.Id,
                                CriterionId = critNational.Id,
                                ActivityId = comp.Id,
                                Score = 30,
                                ScoreType = "Auto",
                                AwardedAt = DateTime.UtcNow
                            });
                        }
                    }

                    // Save all new auto details, then recalc summary
                    await db.SaveChangesAsync();

                    // Recalculate totals using repository style logic
                    var repo = new Repositories.ClubMovementRecords.ClubMovementRecordRepository(db);
                    await repo.RecalculateTotalScoreAsync(record.Id);

                    _logger.LogInformation("🏆 Club {ClubId} auto-scored for month {Month}", club.Id, month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error auto-scoring club {ClubId}", club.Id);
                }
            }

            _logger.LogInformation("✅ Club scores processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in ProcessClubScoresAsync");
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
                record.TotalScore = Math.Min(totalScore, 100); // Cap at 100 as per Decision 414
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
