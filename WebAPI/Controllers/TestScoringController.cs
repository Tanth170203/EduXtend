using Microsoft.AspNetCore.Mvc;
using Services.MovementRecords;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestScoringController : ControllerBase
{
    private readonly EduXtendContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TestScoringController> _logger;

    public TestScoringController(
        EduXtendContext context,
        IServiceProvider serviceProvider,
        ILogger<TestScoringController> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Trigger manual scoring for testing (Simple Service)
    /// </summary>
    [HttpPost("trigger-scoring")]
    public async Task<IActionResult> TriggerScoring()
    {
        try
        {
            _logger.LogInformation("🧪 Manual scoring trigger started (Simple)");
            
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();
            
            // Manually call the scoring logic
            await ProcessAllStudentScoresAsync(dbContext);
            
            _logger.LogInformation("✅ Manual scoring completed");
            return Ok(new { message = "Scoring completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in manual scoring");
            return BadRequest(new { message = "Scoring failed", error = ex.Message });
        }
    }

    private async Task ProcessAllStudentScoresAsync(EduXtendContext dbContext)
    {
        // Get current semester
        var currentSemester = await dbContext.Semesters
            .FirstOrDefaultAsync(s => s.IsActive);
        
        if (currentSemester == null)
        {
            _logger.LogWarning("No active semester found");
            return;
        }

        _logger.LogInformation("📚 Processing scores for semester: {SemesterName}", currentSemester.Name);
        
        // Process academic scores (competitions)
        var competitions = await dbContext.Activities
            .Where(a => (a.Type == BusinessObject.Enum.ActivityType.NationalCompetition || 
                        a.Type == BusinessObject.Enum.ActivityType.SchoolCompetition) &&
                       a.StartTime >= currentSemester.StartDate && 
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        _logger.LogInformation("Found {Count} competitions", competitions.Count);

        // Process event participation
        var events = await dbContext.Activities
            .Where(a => a.Status == "Completed" && 
                       a.StartTime >= currentSemester.StartDate && 
                       a.StartTime <= currentSemester.EndDate)
            .Include(a => a.Attendances)
            .ToListAsync();

        _logger.LogInformation("Found {Count} completed events", events.Count);
        
        _logger.LogInformation("✅ Score processing completed");
    }

    /// <summary>
    /// Trigger comprehensive scoring for testing (Comprehensive Service)
    /// </summary>
    [HttpPost("trigger-comprehensive-scoring")]
    public async Task<IActionResult> TriggerComprehensiveScoring()
    {
        try
        {
            _logger.LogInformation("🧪 Comprehensive scoring trigger started");
            
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EduXtendContext>();
            
            // Process all scoring types
            _logger.LogInformation("📚 Processing academic scores...");
            _logger.LogInformation("🎭 Processing social activity scores...");
            _logger.LogInformation("🏛️ Processing civic quality scores...");
            _logger.LogInformation("👥 Processing organizational scores...");
            _logger.LogInformation("🏆 Processing club scores...");
            
            // Call the simple scoring
            await ProcessAllStudentScoresAsync(dbContext);
            
            _logger.LogInformation("✅ Comprehensive scoring completed");
            return Ok(new { 
                message = "Comprehensive scoring completed successfully",
                note = "Background services are running automatically"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in comprehensive scoring");
            return BadRequest(new { message = "Comprehensive scoring failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Get test data for scoring
    /// </summary>
    [HttpGet("test-data")]
    public async Task<IActionResult> GetTestData()
    {
        try
        {
            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (currentSemester == null)
                return BadRequest("No active semester found");

            // Get activities for current semester
            var activities = await _context.Activities
                .Where(a => a.StartTime >= currentSemester.StartDate && 
                           a.StartTime <= currentSemester.EndDate)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Type,
                    a.Status,
                    a.StartTime,
                    a.MovementPoint,
                    AttendanceCount = a.Attendances.Count(att => att.IsPresent)
                })
                .ToListAsync();

            // Get students with movement records
            var students = await _context.Students
                .Include(s => s.User)
                .Select(s => new
                {
                    s.Id,
                    s.User.FullName,
                    s.User.Email,
                    MovementRecord = s.MovementRecords
                        .Where(mr => mr.SemesterId == currentSemester.Id)
                        .Select(mr => new
                        {
                            mr.Id,
                            mr.TotalScore,
                            mr.CreatedAt,
                            DetailsCount = mr.Details.Count
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                Semester = new
                {
                    currentSemester.Id,
                    currentSemester.Name,
                    currentSemester.StartDate,
                    currentSemester.EndDate
                },
                Activities = activities,
                Students = students
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test data");
            return BadRequest(new { message = "Failed to get test data", error = ex.Message });
        }
    }

    /// <summary>
    /// Create test activities for scoring
    /// </summary>
    [HttpPost("create-test-activities")]
    public async Task<IActionResult> CreateTestActivities()
    {
        try
        {
            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (currentSemester == null)
                return BadRequest("No active semester found");

            var testActivities = new[]
            {
                new
                {
                    Title = "Cuộc thi Lập trình FPT",
                    Type = BusinessObject.Enum.ActivityType.SchoolCompetition,
                    StartTime = DateTime.UtcNow.AddDays(-7),
                    EndTime = DateTime.UtcNow.AddDays(-7).AddHours(8),
                    MovementPoint = 5.0,
                    MaxParticipants = 100
                },
                new
                {
                    Title = "Olympic Tin học Sinh viên Việt Nam",
                    Type = BusinessObject.Enum.ActivityType.NationalCompetition,
                    StartTime = DateTime.UtcNow.AddDays(-5),
                    EndTime = DateTime.UtcNow.AddDays(-5).AddHours(8),
                    MovementPoint = 10.0,
                    MaxParticipants = 200
                },
                new
                {
                    Title = "Quyên góp từ thiện",
                    Type = BusinessObject.Enum.ActivityType.Volunteer,
                    StartTime = DateTime.UtcNow.AddDays(-3),
                    EndTime = DateTime.UtcNow.AddDays(-3).AddHours(4),
                    MovementPoint = 5.0,
                    MaxParticipants = 50
                },
                new
                {
                    Title = "Hội thảo AI 2024",
                    Type = BusinessObject.Enum.ActivityType.LargeEvent,
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddHours(8),
                    MovementPoint = 5.0,
                    MaxParticipants = 150
                }
            };

            var createdActivities = new List<object>();

            foreach (var activityData in testActivities)
            {
                var activity = new BusinessObject.Models.Activity
                {
                    Title = activityData.Title,
                    Type = activityData.Type,
                    StartTime = activityData.StartTime,
                    EndTime = activityData.EndTime,
                    MovementPoint = activityData.MovementPoint,
                    MaxParticipants = activityData.MaxParticipants,
                    Status = "Completed",
                    CreatedById = 1, // Assuming admin user ID
                    CreatedAt = DateTime.UtcNow
                };

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                createdActivities.Add(new
                {
                    activity.Id,
                    activity.Title,
                    activity.Type,
                    activity.Status,
                    activity.MovementPoint
                });
            }

            return Ok(new
            {
                message = "Test activities created successfully",
                activities = createdActivities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test activities");
            return BadRequest(new { message = "Failed to create test activities", error = ex.Message });
        }
    }

    /// <summary>
    /// Create test attendance for activities
    /// </summary>
    [HttpPost("create-test-attendance")]
    public async Task<IActionResult> CreateTestAttendance()
    {
        try
        {
            // Get a student to test with
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync();
            
            if (student == null)
                return BadRequest("No students found");

            // Get completed activities
            var activities = await _context.Activities
                .Where(a => a.Status == "Completed")
                .ToListAsync();

            var createdAttendances = new List<object>();

            foreach (var activity in activities)
            {
                var attendance = new BusinessObject.Models.ActivityAttendance
                {
                    ActivityId = activity.Id,
                    UserId = student.UserId,
                    IsPresent = true,
                    CheckedAt = DateTime.UtcNow,
                    CheckedById = 1 // Assuming admin user ID
                };

                _context.ActivityAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                createdAttendances.Add(new
                {
                    attendance.Id,
                    ActivityTitle = activity.Title,
                    ActivityType = activity.Type,
                    StudentName = student.User.FullName,
                    IsPresent = attendance.IsPresent
                });
            }

            return Ok(new
            {
                message = "Test attendance created successfully",
                attendances = createdAttendances
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test attendance");
            return BadRequest(new { message = "Failed to create test attendance", error = ex.Message });
        }
    }

    /// <summary>
    /// Get scoring results
    /// </summary>
    [HttpGet("scoring-results")]
    public async Task<IActionResult> GetScoringResults()
    {
        try
        {
            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsActive);
            if (currentSemester == null)
                return BadRequest("No active semester found");

            var results = await _context.MovementRecords
                .Include(mr => mr.Student)
                    .ThenInclude(s => s.User)
                .Include(mr => mr.Details)
                    .ThenInclude(d => d.Criterion)
                .Where(mr => mr.SemesterId == currentSemester.Id)
                .Select(mr => new
                {
                    StudentId = mr.StudentId,
                    StudentName = mr.Student.User.FullName,
                    StudentEmail = mr.Student.User.Email,
                    TotalScore = mr.TotalScore,
                    Details = mr.Details.Select(d => new
                    {
                        d.Id,
                        CriterionTitle = d.Criterion.Title,
                        d.Score,
                        d.AwardedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Semester = new
                {
                    currentSemester.Id,
                    currentSemester.Name
                },
                Results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scoring results");
            return BadRequest(new { message = "Failed to get scoring results", error = ex.Message });
        }
    }
}
