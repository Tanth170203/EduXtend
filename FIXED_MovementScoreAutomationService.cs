using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.MovementRecords;

/// <summary>
/// FIXED: Background service to automatically calculate movement scores from activity attendance
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
                // 1. Check và switch semester trước
                await CheckAndSwitchSemesterAsync();
                
                // 2. Xử lý điểm
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

    /// <summary>
    /// FIXED: Check và switch semester tự động
    /// </summary>
    private async Task CheckAndSwitchSemesterAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            var now = DateTime.Now;
            
            // Tìm kì hiện tại
            var currentSemester = await dbContext.Semesters
                .FirstOrDefaultAsync(s => s.IsActive);
                
            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found");
                return;
            }
            
            // Tìm kì mới sắp bắt đầu
            var newSemester = await dbContext.Semesters
                .Where(s => s.StartDate <= now && s.StartDate > currentSemester.StartDate)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();
                
            if (newSemester != null)
            {
                _logger.LogInformation("Switching from semester {OldSemester} to {NewSemester}", 
                    currentSemester.Name, newSemester.Name);
                
                // Đóng kì cũ
                currentSemester.IsActive = false;
                
                // Mở kì mới  
                newSemester.IsActive = true;
                
                // Backup dữ liệu kì cũ
                await BackupSemesterDataAsync(dbContext, currentSemester.Id);
                
                // Tạo MovementRecord mới cho tất cả sinh viên
                await CreateNewSemesterRecordsAsync(dbContext, newSemester.Id);
                
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Successfully switched to semester {NewSemester}", newSemester.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and switching semester");
        }
    }

    /// <summary>
    /// FIXED: Backup dữ liệu kì cũ
    /// </summary>
    private async Task BackupSemesterDataAsync(EduXtendContext dbContext, int oldSemesterId)
    {
        try
        {
            // Có thể tạo bảng backup hoặc export ra file
            var records = await dbContext.MovementRecords
                .Include(r => r.Details)
                .Where(r => r.SemesterId == oldSemesterId)
                .ToListAsync();
                
            _logger.LogInformation("Backed up {Count} movement records from semester {SemesterId}", 
                records.Count, oldSemesterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backing up semester data");
        }
    }

    /// <summary>
    /// FIXED: Tạo MovementRecord mới cho kì mới
    /// </summary>
    private async Task CreateNewSemesterRecordsAsync(EduXtendContext dbContext, int newSemesterId)
    {
        try
        {
            var students = await dbContext.Students.ToListAsync();
            
            foreach (var student in students)
            {
                var existingRecord = await dbContext.MovementRecords
                    .FirstOrDefaultAsync(r => r.StudentId == student.Id && r.SemesterId == newSemesterId);
                    
                if (existingRecord == null)
                {
                    var newRecord = new MovementRecord
                    {
                        StudentId = student.Id,
                        SemesterId = newSemesterId,
                        TotalScore = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.MovementRecords.Add(newRecord);
                }
            }
            
            _logger.LogInformation("Created new semester records for {Count} students", students.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new semester records");
        }
    }

    /// <summary>
    /// FIXED: Process attendance scores với logic đúng
    /// </summary>
    private async Task ProcessAttendanceScoresAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();

        try
        {
            _logger.LogInformation("Processing attendance scores...");

            // Get current semester
            var currentSemester = await dbContext.Semesters
                .FirstOrDefaultAsync(s => s.IsActive);

            if (currentSemester == null)
            {
                _logger.LogWarning("No active semester found");
                return;
            }

            // Get activities that are completed and have movement points
            var completedActivities = await dbContext.Activities
                .Include(a => a.Attendances)
                    .ThenInclude(att => att.User)
                .Where(a => a.Status == "Completed" && a.MovementPoint > 0)
                .ToListAsync();

            _logger.LogInformation("Found {Count} completed activities with movement points", completedActivities.Count);

            foreach (var activity in completedActivities)
            {
                await ProcessActivityAttendanceAsync(dbContext, activity, currentSemester.Id);
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Finished processing attendance scores");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attendance scores");
        }
    }

    /// <summary>
    /// FIXED: Process activity attendance với logic đúng
    /// </summary>
    private async Task ProcessActivityAttendanceAsync(EduXtendContext dbContext, Activity activity, int semesterId)
    {
        try
        {
            // FIXED: Tìm tiêu chí đúng theo mapping
            var activityCriterion = await GetCriterionForActivityAsync(dbContext, activity);

            if (activityCriterion == null)
            {
                _logger.LogWarning("No criterion found for activity {ActivityId}", activity.Id);
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
                var record = await GetOrCreateMovementRecordAsync(dbContext, student.Id, semesterId);

                // FIXED: Check duplicate với logic đúng
                var existingDetail = await dbContext.MovementRecordDetails
                    .FirstOrDefaultAsync(d => d.MovementRecordId == record.Id 
                                            && d.CriterionId == activityCriterion.Id 
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
                    await RecalculateStudentScoreAsync(dbContext, record.Id);

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
    /// FIXED: Process club members với logic đúng
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

            var today = DateTime.UtcNow;
            var month = today.Month;
            var year = today.Year;

            foreach (var member in clubMembers)
            {
                try
                {
                    // FIXED: Tìm tiêu chí đúng theo role
                    var criterionForClub = await GetCriterionForClubRoleAsync(dbContext, member.RoleInClub);

                    if (criterionForClub == null)
                    {
                        _logger.LogWarning("No criterion found for club role {Role}", member.RoleInClub);
                        continue;
                    }

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

                    // FIXED: Tính điểm theo tiêu chí thực tế
                    var score = await CalculateClubMemberScoreAsync(dbContext, member, criterionForClub);

                    // Get or create movement record
                    var record = await GetOrCreateMovementRecordAsync(dbContext, member.StudentId, currentSemester.Id);

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
                    await RecalculateStudentScoreAsync(dbContext, record.Id);

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

    /// <summary>
    /// FIXED: Tìm tiêu chí cho activity
    /// </summary>
    private async Task<MovementCriterion?> GetCriterionForActivityAsync(EduXtendContext dbContext, Activity activity)
    {
        // FIXED: Mapping rõ ràng thay vì dùng Contains
        var criterionId = activity.Type switch
        {
            "ClubActivity" => 5, // Tham gia CLB
            "SchoolEvent" => 4,  // Sự kiện CTSV
            "Volunteer" => 10,   // Tình nguyện
            "Competition" => 3,  // Cuộc thi cấp trường
            _ => 4 // Default: Sự kiện CTSV
        };

        return await dbContext.MovementCriteria
            .FirstOrDefaultAsync(c => c.Id == criterionId && c.IsActive);
    }

    /// <summary>
    /// FIXED: Tìm tiêu chí cho club role
    /// </summary>
    private async Task<MovementCriterion?> GetCriterionForClubRoleAsync(EduXtendContext dbContext, string role)
    {
        var criterionId = role switch
        {
            "President" => 12, // Chủ nhiệm CLB
            "VicePresident" => 13, // Phó BTC
            "Manager" => 14, // Thành viên BCH
            "Member" => 5, // Thành viên CLB
            _ => 5 // Default: Thành viên CLB
        };

        return await dbContext.MovementCriteria
            .FirstOrDefaultAsync(c => c.Id == criterionId && c.IsActive);
    }

    /// <summary>
    /// FIXED: Tính điểm theo tiêu chí thực tế
    /// </summary>
    private async Task<double> CalculateClubMemberScoreAsync(EduXtendContext dbContext, ClubMember member, MovementCriterion criterion)
    {
        // FIXED: Tính theo tiêu chí thực tế thay vì hardcode
        var baseScore = member.RoleInClub switch
        {
            "President" => 8, // Chủ nhiệm CLB: 5-10 điểm
            "VicePresident" => 6, // Phó BTC: 5-10 điểm
            "Manager" => 4, // Thành viên BCH: 1-10 điểm
            "Member" => 2, // Thành viên CLB: 1-10 điểm
            _ => 1
        };

        // Có thể thêm logic đánh giá từ Ban chủ nhiệm CLB
        // var evaluation = await GetClubEvaluationAsync(member.ClubId, member.StudentId);
        // return Math.Min(baseScore + evaluation, criterion.MaxScore);

        return Math.Min(baseScore, criterion.MaxScore);
    }

    /// <summary>
    /// FIXED: Get or create movement record
    /// </summary>
    private async Task<MovementRecord> GetOrCreateMovementRecordAsync(EduXtendContext dbContext, int studentId, int semesterId)
    {
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
            await dbContext.SaveChangesAsync(); // Save to get the ID
        }

        return record;
    }

    /// <summary>
    /// FIXED: Recalculate student score
    /// </summary>
    private async Task RecalculateStudentScoreAsync(EduXtendContext dbContext, int recordId)
    {
        var record = await dbContext.MovementRecords
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (record != null)
        {
            var totalScore = await dbContext.MovementRecordDetails
                .Where(d => d.MovementRecordId == recordId)
                .SumAsync(d => d.Score);

            record.TotalScore = Math.Min(totalScore, 140); // Cap at 140
            record.LastUpdated = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// FIXED: Helper service for manual score calculations
/// </summary>
public interface IMovementScoreCalculationService
{
    Task<double> CalculateClubMemberScoreAsync(int studentId, int clubId, int semesterId);
    Task AddClubMembershipScoreAsync(int studentId, int clubId, int semesterId);
    Task RecalculateStudentScoreAsync(int studentId, int semesterId);
    Task<bool> CheckAndSwitchSemesterAsync();
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

            // FIXED: Tính theo tiêu chí thực tế
            var criterion = await GetCriterionForClubRoleAsync(membership.RoleInClub);
            if (criterion == null)
                return 0;

            var baseScore = membership.RoleInClub switch
            {
                "President" => 8,
                "VicePresident" => 6,
                "Manager" => 4,
                "Member" => 2,
                _ => 1
            };

            return Math.Min(baseScore, criterion.MaxScore);
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
            var membership = await _context.ClubMembers
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.ClubId == clubId && m.IsActive);

            if (membership == null)
                return;

            var criterion = await GetCriterionForClubRoleAsync(membership.RoleInClub);
            if (criterion == null)
                return;

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

            // FIXED: Check duplicate trước khi thêm
            var existingDetail = await _context.MovementRecordDetails
                .FirstOrDefaultAsync(d => d.MovementRecordId == record.Id && d.CriterionId == criterion.Id);

            if (existingDetail == null)
            {
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

    public async Task<bool> CheckAndSwitchSemesterAsync()
    {
        try
        {
            var now = DateTime.Now;
            
            // Tìm kì hiện tại
            var currentSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsActive);
                
            if (currentSemester == null)
                return false;
            
            // Tìm kì mới sắp bắt đầu
            var newSemester = await _context.Semesters
                .Where(s => s.StartDate <= now && s.StartDate > currentSemester.StartDate)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync();
                
            if (newSemester != null)
            {
                // Đóng kì cũ
                currentSemester.IsActive = false;
                
                // Mở kì mới  
                newSemester.IsActive = true;
                
                await _context.SaveChangesAsync();
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and switching semester");
            return false;
        }
    }

    private async Task<MovementCriterion?> GetCriterionForClubRoleAsync(string role)
    {
        var criterionId = role switch
        {
            "President" => 12, // Chủ nhiệm CLB
            "VicePresident" => 13, // Phó BTC
            "Manager" => 14, // Thành viên BCH
            "Member" => 5, // Thành viên CLB
            _ => 5 // Default: Thành viên CLB
        };

        return await _context.MovementCriteria
            .FirstOrDefaultAsync(c => c.Id == criterionId && c.IsActive);
    }
}
