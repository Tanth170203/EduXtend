using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.MovementRecords;

/// <summary>
/// [DEPRECATED] This service is no longer in use.
/// All functionality has been merged into ComprehensiveAutoScoringService.
/// This file is kept for reference only and can be safely deleted.
/// 
/// Background service to automatically calculate movement scores from activity attendance
/// </summary>
[Obsolete("Use ComprehensiveAutoScoringService instead. This service is deprecated and not registered.")]
public class MovementScoreAutomationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MovementScoreAutomationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public MovementScoreAutomationService(
        IServiceProvider serviceProvider,
        ILogger<MovementScoreAutomationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // === Decision 414 - Academic awareness: competitions ===
    // National/Regional competitions: +10 ĐPT/lần; School-level competitions: +5 ĐPT/lần
    private async Task ProcessStudentCompetitionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        var sem = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive);
        if (sem == null) return;

        var criNational = await db.MovementCriteria.Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Title.Contains("Olympic") || c.Title.Contains("ACM") || c.Title.Contains("ICPC") || c.Title.Contains("Quốc gia"));
        var criSchool = await db.MovementCriteria.Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Title.Contains("Thi cấp trường"));

        var acts = await db.Activities
            .Where(a => a.EndTime >= sem.StartDate && a.EndTime <= sem.EndDate
                && (a.Type == BusinessObject.Enum.ActivityType.NationalCompetition || a.Type == BusinessObject.Enum.ActivityType.SchoolCompetition)
                && a.Status == "Completed")
            .Include(a => a.Attendances)
            .ToListAsync();

        foreach (var act in acts)
        {
            var criterion = act.Type == BusinessObject.Enum.ActivityType.NationalCompetition ? criNational : criSchool;
            if (criterion == null) continue;

            foreach (var att in act.Attendances.Where(x => x.IsPresent))
            {
                var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == att.UserId);
                if (student == null) continue;

                var rec = await db.MovementRecords.Include(r => r.Details).FirstOrDefaultAsync(r => r.StudentId == student.Id && r.SemesterId == sem.Id);
                if (rec == null)
                {
                    rec = new MovementRecord { StudentId = student.Id, SemesterId = sem.Id, CreatedAt = DateTime.UtcNow };
                    db.MovementRecords.Add(rec);
                    await db.SaveChangesAsync();
                }

                var already = rec.Details.Any(d => d.CriterionId == criterion.Id && d.ActivityId == act.Id);
                if (already) continue;

                // Enforce group cap (Academic awareness max 35)
                var currentGroup = await db.MovementRecordDetails
                    .Include(d => d.Criterion)
                    .Where(d => d.MovementRecordId == rec.Id && d.Criterion.GroupId == criterion.GroupId)
                    .SumAsync(d => d.Score);

                var baseScore = act.Type == BusinessObject.Enum.ActivityType.NationalCompetition ? 10 : 5;
                var scoreToAdd = Math.Min(baseScore, Math.Max(0, criterion.Group.MaxScore - currentGroup));
                if (scoreToAdd <= 0) continue;

                var detail = new MovementRecordDetail
                {
                    MovementRecordId = rec.Id,
                    CriterionId = criterion.Id,
                    ActivityId = act.Id,
                    Score = scoreToAdd,
                    AwardedAt = DateTime.UtcNow
                };
                db.MovementRecordDetails.Add(detail);

                var newTotal = rec.Details.Sum(d => d.Score) + scoreToAdd;
                rec.TotalScore = Math.Min(newTotal, 100);
                rec.LastUpdated = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    // === Decision 414 - Civic/Volunteer: +5 ĐPT/lần ===
    private async Task ProcessStudentVolunteerAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        var sem = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive);
        if (sem == null) return;

        var criVolunteer = await db.MovementCriteria.Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Title.Contains("Hành vi tốt") || c.Title.Contains("Hoạt động xã hội") || c.Title.Contains("Hoạt động tình nguyện"));
        if (criVolunteer == null) return;

        // Volunteer activity type has been removed - return empty list
        var acts = new List<Activity>();

        foreach (var act in acts)
        {
            foreach (var att in act.Attendances.Where(x => x.IsPresent))
            {
                var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == att.UserId);
                if (student == null) continue;

                var rec = await db.MovementRecords.Include(r => r.Details).FirstOrDefaultAsync(r => r.StudentId == student.Id && r.SemesterId == sem.Id);
                if (rec == null)
                {
                    rec = new MovementRecord { StudentId = student.Id, SemesterId = sem.Id, CreatedAt = DateTime.UtcNow };
                    db.MovementRecords.Add(rec);
                    await db.SaveChangesAsync();
                }

                // Enforce group cap (Civic qualities max 25)
                var currentGroup = await db.MovementRecordDetails
                    .Include(d => d.Criterion)
                    .Where(d => d.MovementRecordId == rec.Id && d.Criterion.GroupId == criVolunteer.GroupId)
                    .SumAsync(d => d.Score);

                var scoreToAdd = Math.Min(5, Math.Max(0, criVolunteer.Group.MaxScore - currentGroup));
                if (scoreToAdd <= 0) continue;

                db.MovementRecordDetails.Add(new MovementRecordDetail
                {
                    MovementRecordId = rec.Id,
                    CriterionId = criVolunteer.Id,
                    ActivityId = act.Id,
                    Score = scoreToAdd,
                    AwardedAt = DateTime.UtcNow
                });

                var newTotal = rec.Details.Sum(d => d.Score) + scoreToAdd;
                rec.TotalScore = Math.Min(newTotal, 100);
                rec.LastUpdated = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Movement Score Automation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAttendanceScoresAsync();
                //await ProcessClubMembersAsync();
                await ProcessClubAutoScoringAsync();
                await ProcessStudentCompetitionsAsync();
                await ProcessStudentVolunteerAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Movement Score Automation Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }

        _logger.LogInformation("Movement Score Automation Service stopped");
    }

    private async Task ProcessAttendanceScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("Processing attendance scores...");

            // Get activities that are completed and have movement points
            var completedActivities = await dbContext.Activities
                .Include(a => a.Attendances)
                    .ThenInclude(att => att.User)
                .Where(a => a.Status == "Completed" && a.MovementPoint > 0)
                .ToListAsync();

            _logger.LogInformation("Found {Count} completed activities with movement points", completedActivities.Count);

            foreach (var activity in completedActivities)
            {
                await ProcessActivityAttendanceAsync(dbContext, activity);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Finished processing attendance scores");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attendance scores");
        }
    }

    private async Task ProcessActivityAttendanceAsync(EduXtendContext dbContext, Activity activity)
    {
        try
        {
            // Get current semester
            var currentSemester = await dbContext.Semesters
                .FirstOrDefaultAsync(s => s.IsActive);

            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found");
                return;
            }

            // Decision 414 - Social activities: điểm sự kiện (3–5 điểm/sự kiện)
            // Criterion title: "Tham gia sự kiện"
            var activityCriterion = await dbContext.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("Tham gia sự kiện") && c.IsActive);

            if (activityCriterion == null)
            {
                _logger.LogWarning("No criterion found for activity participation");
                return;
            }

            // Skip competitions here; they are processed in dedicated routines below
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
                // Dedupe: nếu đã có bản ghi cùng activity → skip
                var existingDetail = await dbContext.MovementRecordDetails
                    .AnyAsync(d => d.MovementRecordId == record.Id 
                                && d.CriterionId == activityCriterion.Id
                                && d.ActivityId == activity.Id);

                if (!existingDetail)
                {
                    // Load criterion with group to check MaxScore
                    var criterionWithGroup = await dbContext.MovementCriteria
                        .Include(c => c.Group)
                        .FirstOrDefaultAsync(c => c.Id == activityCriterion.Id);

                    if (criterionWithGroup?.Group == null)
                    {
                        _logger.LogWarning("Criterion {CriterionId} has no group", activityCriterion.Id);
                        continue;
                    }

                    // Calculate current GROUP score
                    var currentGroupScore = await dbContext.MovementRecordDetails
                        .Include(d => d.Criterion)
                        .Where(d => d.MovementRecordId == record.Id 
                                 && d.Criterion.GroupId == criterionWithGroup.GroupId)
                        .SumAsync(d => d.Score);

                    var groupMaxScore = criterionWithGroup.Group.MaxScore;

                    // Check if already reached group max score
                    if (currentGroupScore >= groupMaxScore)
                    {
                        _logger.LogInformation(
                            "Student {StudentId} already reached max score {MaxScore} for group {GroupName}. Skipping activity {ActivityTitle}",
                            student.Id, groupMaxScore, criterionWithGroup.Group.Name, activity.Title);
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
                        scoreToAdd, student.Id, activity.Title, criterionWithGroup.Group.Name, 
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

    /// <summary>
    /// Process club members monthly for scoring
    /// Called every 6 hours, but only processes once per month per student/club
    /// </summary>
    private async Task ProcessClubMembersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("Processing club member scores...");

            // Get current semester
            var currentSemester = await dbContext.Semesters
                .Where(s => s.IsActive)
                .FirstOrDefaultAsync();

            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found for club member scoring");
                return;
            }

            // Get all active club members
            var clubMembers = await dbContext.ClubMembers
                .Include(m => m.Student)
                .Include(m => m.Club)
                .Where(m => m.IsActive)
                .ToListAsync();

            _logger.LogInformation("Found {Count} active club members", clubMembers.Count);

            var criterionForClub = await dbContext.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("CLB") && c.IsActive);

            if (criterionForClub == null)
            {
                _logger.LogWarning("No criterion found for club membership scoring");
                return;
            }

            var today = DateTime.UtcNow;
            var month = today.Month;
            var year = today.Year;

            foreach (var member in clubMembers)
            {
                try
                {
                    // Check if already scored this month
                    var existingScore = await dbContext.MovementRecordDetails
                        .Include(d => d.MovementRecord)
                        .Where(d => 
                            d.MovementRecord.StudentId == member.StudentId &&
                            d.MovementRecord.SemesterId == currentSemester.Id &&
                            d.CriterionId == criterionForClub.Id &&
                            d.AwardedAt.Year == year &&
                            d.AwardedAt.Month == month)
                        .FirstOrDefaultAsync();

                    if (existingScore != null)
                    {
                        _logger.LogInformation(
                            "Club member score already recorded this month for student {StudentId} in club {ClubId}",
                            member.StudentId, member.ClubId);
                        continue;
                    }

                    // Club member monthly score is manager-assessed (manual), skip auto-scoring for now
                    _logger.LogInformation(
                        "Skipping auto-scoring for club member {StudentId} in club {ClubId} (manager-assessed)",
                        member.StudentId, member.ClubId);
                    continue;

                    // Note: The actual monthly member score will be input by Club Manager in future feature
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error processing club member {StudentId} in club {ClubId}",
                        member.StudentId, member.ClubId);
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Finished processing club member scores");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing club members");
        }
    }

    /// <summary>
    /// Auto-scoring for Clubs based on activities within current month and semester
    /// - ClubMeeting: 5 points per week (max 20)
    /// - Events: Large(20) if >=70% attendance; Small(15) if >=70%; Internal(5)
    /// - Competitions: Provincial(20), National(30)
    /// - Plan: handled via manual toggle separately (10)
    /// </summary>
    private async Task ProcessClubAutoScoringAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("Processing club auto-scoring...");

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

                    _logger.LogInformation("Club {ClubId} auto-scored for month {Month}", club.Id, month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-scoring club {ClubId}", club.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessClubAutoScoringAsync");
        }
    }
}

/// <summary>
/// Helper service for manual score calculations
/// </summary>
public interface IMovementScoreCalculationService
{
    Task<double> CalculateClubMemberScoreAsync(int studentId, int clubId, int semesterId);
    Task AddClubMembershipScoreAsync(int studentId, int clubId, int semesterId);
    Task RecalculateStudentScoreAsync(int studentId, int semesterId);
}

public class MovementScoreCalculationService : IMovementScoreCalculationService
{
    private readonly EduXtendContext _context;
    private readonly ILogger<MovementScoreCalculationService> _logger;

    public MovementScoreCalculationService(
        EduXtendContext context,
        ILogger<MovementScoreCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<double> CalculateClubMemberScoreAsync(int studentId, int clubId, int semesterId)
    {
        try
        {
            // Get club membership
            var membership = await _context.ClubMembers
                .Include(m => m.Club)
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.ClubId == clubId && m.IsActive);

            if (membership == null)
                return 0;

            // Calculate score based on role
            double score = membership.RoleInClub switch
            {
                "President" => 10,
                "VicePresident" => 8,
                "Manager" => 5,
                "Member" => 3,
                _ => 1
            };

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating club member score");
            return 0;
        }
    }

    public async Task AddClubMembershipScoreAsync(int studentId, int clubId, int semesterId)
    {
        try
        {
            var score = await CalculateClubMemberScoreAsync(studentId, clubId, semesterId);
            if (score <= 0)
                return;

            // Get criterion for club membership
            var criterion = await _context.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("Thành viên CLB") && c.IsActive);

            if (criterion == null)
            {
                _logger.LogWarning("No criterion found for club membership");
                return;
            }

            // Get or create movement record
            var record = await _context.MovementRecords
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
                _context.MovementRecords.Add(record);
                await _context.SaveChangesAsync();
            }

            // Add detail
            var detail = new MovementRecordDetail
            {
                MovementRecordId = record.Id,
                CriterionId = criterion.Id,
                Score = Math.Min(score, criterion.MaxScore),
                AwardedAt = DateTime.UtcNow
            };
            _context.MovementRecordDetails.Add(detail);

            // Recalculate total
            await RecalculateStudentScoreAsync(studentId, semesterId);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added club membership score of {Score} to student {StudentId}", score, studentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding club membership score");
        }
    }

    public async Task RecalculateStudentScoreAsync(int studentId, int semesterId)
    {
        try
        {
            var record = await _context.MovementRecords
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);

            if (record == null)
                return;

            var totalScore = record.Details.Sum(d => d.Score);
            record.TotalScore = Math.Min(totalScore, 100); // Cap at 100
            record.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating student score");
        }
    }
}

