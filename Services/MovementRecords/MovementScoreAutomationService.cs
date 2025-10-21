using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.MovementRecords;

/// <summary>
/// Background service to automatically calculate movement scores from activity attendance
/// </summary>
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Movement Score Automation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAttendanceScoresAsync();
                await ProcessClubMembersAsync();
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

            // Get a default criterion for activity participation
            // You should create this criterion in advance or modify this logic
            var activityCriterion = await dbContext.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("Tham gia hoạt động") && c.IsActive);

            if (activityCriterion == null)
            {
                _logger.LogWarning("No criterion found for activity participation");
                return;
            }

            // Process each attendance
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

                // Check if score already added for this activity
                var existingDetail = record.Details
                    .FirstOrDefault(d => d.CriterionId == activityCriterion.Id 
                                      && d.AwardedAt.Date == activity.EndTime.Date);

                if (existingDetail == null)
                {
                    // Add score
                    var detail = new MovementRecordDetail
                    {
                        MovementRecordId = record.Id,
                        CriterionId = activityCriterion.Id,
                        Score = Math.Min(activity.MovementPoint, activityCriterion.MaxScore),
                        AwardedAt = DateTime.UtcNow
                    };
                    dbContext.MovementRecordDetails.Add(detail);

                    // Update total score
                    var totalScore = await dbContext.MovementRecordDetails
                        .Where(d => d.MovementRecordId == record.Id)
                        .SumAsync(d => d.Score);

                    record.TotalScore = Math.Min(totalScore + detail.Score, 140); // Cap at 140
                    record.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Added {Points} points to student {StudentId} for activity {ActivityTitle}",
                        detail.Score, student.Id, activity.Title);
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

                    // Calculate score based on role
                    double score = member.RoleInClub switch
                    {
                        "President" => 10,
                        "VicePresident" => 8,
                        "Manager" => 5,
                        "Member" => 3,
                        _ => 1
                    };

                    // Get or create movement record
                    var record = await dbContext.MovementRecords
                        .FirstOrDefaultAsync(r => 
                            r.StudentId == member.StudentId && 
                            r.SemesterId == currentSemester.Id);

                    if (record == null)
                    {
                        record = new MovementRecord
                        {
                            StudentId = member.StudentId,
                            SemesterId = currentSemester.Id,
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
                        CriterionId = criterionForClub.Id,
                        Score = Math.Min(score, criterionForClub.MaxScore),
                        AwardedAt = DateTime.UtcNow
                    };

                    dbContext.MovementRecordDetails.Add(detail);

                    // Update total score
                    var totalScore = await dbContext.MovementRecordDetails
                        .Where(d => d.MovementRecordId == record.Id)
                        .SumAsync(d => d.Score);

                    record.TotalScore = Math.Min(totalScore + detail.Score, 140);
                    record.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Added {Score} club score to student {StudentId} in club {ClubName} (role: {Role})",
                        score, member.StudentId, member.Club.Name, member.RoleInClub);
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
            record.TotalScore = Math.Min(totalScore, 140); // Cap at 140
            record.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating student score");
        }
    }
}

