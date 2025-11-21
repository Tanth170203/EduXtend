using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Activities;
using Repositories.ClubMovementRecords;
using Repositories.Clubs;
using Repositories.MovementCriteria;
using Repositories.Semesters;
using Repositories.Students;
using Repositories.ActivitySchedules;
using Repositories.ActivityScheduleAssignments;
using Services.Activities;
using Services.ClubMovementRecords;

namespace Services.Tests.Activities
{
    /// <summary>
    /// Integration tests for activity completion flow with point calculation
    /// Tests the complete flow from activity completion to point awards
    /// </summary>
    public class ActivityCompletionIntegrationTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly ActivityService _activityService;
        private readonly ClubMovementRecordService _clubMovementRecordService;
        private readonly IActivityRepository _activityRepository;
        private readonly IClubMovementRecordRepository _clubMovementRecordRepository;
        private readonly IMovementCriterionRepository _movementCriterionRepository;
        private readonly Mock<ILogger<ActivityService>> _mockActivityLogger;
        private readonly Mock<ILogger<ClubMovementRecordService>> _mockClubMovementLogger;

        public ActivityCompletionIntegrationTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new EduXtendContext(options);

            // Setup repositories
            _activityRepository = new ActivityRepository(_context);
            _clubMovementRecordRepository = new ClubMovementRecordRepository(_context);
            var detailRepository = new ClubMovementRecordDetailRepository(_context);
            _movementCriterionRepository = new MovementCriterionRepository(_context);
            var semesterRepository = new SemesterRepository(_context);
            var studentRepository = new StudentRepository(_context);
            var clubRepository = new ClubRepository(_context);
            var scheduleRepository = new ActivityScheduleRepository(_context);
            var assignmentRepository = new ActivityScheduleAssignmentRepository(_context);

            // Setup loggers
            _mockActivityLogger = new Mock<ILogger<ActivityService>>();
            _mockClubMovementLogger = new Mock<ILogger<ClubMovementRecordService>>();

            // Setup mock movement record service (not used in these tests)
            var mockMovementRecordService = new Mock<Services.MovementRecords.IMovementRecordService>();

            // Setup services
            _clubMovementRecordService = new ClubMovementRecordService(
                _clubMovementRecordRepository,
                detailRepository,
                _movementCriterionRepository,
                semesterRepository,
                _mockClubMovementLogger.Object
            );

            _activityService = new ActivityService(
                _activityRepository,
                studentRepository,
                clubRepository,
                scheduleRepository,
                assignmentRepository,
                mockMovementRecordService.Object,
                _clubMovementRecordService,
                _mockActivityLogger.Object
            );

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create semester
            var semester = new Semester
            {
                Id = 1,
                Name = "Fall2025",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 12, 31),
                IsActive = true
            };
            _context.Semesters.Add(semester);

            // Create club category
            var category = new ClubCategory
            {
                Id = 1,
                Name = "Technology",
                Description = "Technology related clubs"
            };
            _context.ClubCategories.Add(category);

            // Create clubs
            var club1 = new Club
            {
                Id = 1,
                Name = "IT Club",
                SubName = "Information Technology",
                Description = "Information Technology Club",
                CategoryId = 1,
                IsActive = true
            };
            var club2 = new Club
            {
                Id = 2,
                Name = "English Club",
                SubName = "English Speaking",
                Description = "English Speaking Club",
                CategoryId = 1,
                IsActive = true
            };
            _context.Clubs.AddRange(club1, club2);

            // Create roles
            var adminRole = new Role
            {
                Id = 1,
                RoleName = "Admin",
                Description = "Administrator"
            };
            var managerRole = new Role
            {
                Id = 2,
                RoleName = "ClubManager",
                Description = "Club Manager"
            };
            _context.Roles.AddRange(adminRole, managerRole);

            // Create users
            var admin = new User
            {
                Id = 1,
                FullName = "Admin User",
                Email = "admin@test.com",
                RoleId = 1
            };
            var manager = new User
            {
                Id = 2,
                FullName = "Manager User",
                Email = "manager@test.com",
                RoleId = 2
            };
            _context.Users.AddRange(admin, manager);

            // Create movement criterion group
            var group = new MovementCriterionGroup
            {
                Id = 1,
                Name = "Club Activities",
                Description = "Club activity scoring criteria"
            };
            _context.MovementCriterionGroups.Add(group);

            // Create movement criteria for different activity types
            // Note: Titles must match the search patterns in ClubMovementRecordService
            var criteria = new List<MovementCriterion>
            {
                new MovementCriterion
                {
                    Id = 1,
                    GroupId = 1,
                    Title = "Sinh hoạt CLB định kỳ",
                    Description = "Regular club meeting",
                    MaxScore = 5,
                    TargetType = "Club",
                    DataSource = "ClubMeeting",
                    IsActive = true
                },
                new MovementCriterion
                {
                    Id = 2,
                    GroupId = 1,
                    Title = "Sự kiện lớn 100-200 người",
                    Description = "Large scale event",
                    MaxScore = 20,
                    TargetType = "Club",
                    DataSource = "LargeEvent",
                    IsActive = true
                },
                new MovementCriterion
                {
                    Id = 3,
                    GroupId = 1,
                    Title = "Cuộc thi cấp trường",
                    Description = "School level competition",
                    MaxScore = 20,
                    TargetType = "Club",
                    DataSource = "SchoolCompetition",
                    IsActive = true
                },
                new MovementCriterion
                {
                    Id = 4,
                    GroupId = 1,
                    Title = "Phối hợp với CLB khác",
                    Description = "Collaboration with other clubs",
                    MaxScore = 10,
                    TargetType = "Club",
                    DataSource = "ClubCollaboration",
                    IsActive = true
                }
            };
            _context.MovementCriteria.AddRange(criteria);

            _context.SaveChanges();
        }

        [Fact]
        public async Task CompleteActivity_SuccessfulCompletion_ShouldAwardPoints()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Weekly Meeting",
                Description = "Regular club meeting",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 5,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.True(result.success);
            Assert.Equal(5, result.organizingClubPoints);
            Assert.True(!result.collaboratingClubPoints.HasValue || result.collaboratingClubPoints.Value == 0);

            // Verify activity status updated
            var updatedActivity = await _activityRepository.GetByIdAsync(activity.Id);
            Assert.Equal("Completed", updatedActivity!.Status);

            // Verify ClubMovementRecord created
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.NotNull(record);
            Assert.Equal(5, record.ClubMeetingScore);
            Assert.Equal(5, record.TotalScore);

            // Verify ClubMovementRecordDetail created
            Assert.Single(record.Details);
            var detail = record.Details.First();
            Assert.Equal(5, detail.Score);
            Assert.Equal("Auto", detail.ScoreType);
            Assert.Equal(activity.Id, detail.ActivityId);
        }

        [Fact]
        public async Task CompleteActivity_WeeklyLimitEnforcement_ShouldCapAt5Points()
        {
            // Arrange - Create first activity (3 points)
            var activity1 = new Activity
            {
                Title = "Meeting 1",
                Description = "First meeting",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 3,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity1);
            await _activityService.CompleteActivityAsync(activity1.Id, 1);

            // Create second activity (3 points) - should only get 2 points due to weekly limit
            var activity2 = new Activity
            {
                Title = "Meeting 2",
                Description = "Second meeting",
                Location = "Room 102",
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddHours(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 3,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity2);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity2.Id, 1);

            // Assert
            Assert.True(result.success);
            Assert.Equal(2, result.organizingClubPoints); // Only 2 points awarded (5 - 3 = 2)

            // Verify total weekly points is capped at 5
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.NotNull(record);
            Assert.Equal(5, record.ClubMeetingScore); // Total should be 5 (3 + 2)
        }

        [Fact]
        public async Task CompleteActivity_WeeklyLimitExceeded_ShouldAwardZeroPoints()
        {
            // Arrange - Create first activity (5 points)
            var activity1 = new Activity
            {
                Title = "Meeting 1",
                Description = "First meeting",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 5,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity1);
            await _activityService.CompleteActivityAsync(activity1.Id, 1);

            // Create second activity (3 points) - should get 0 points
            var activity2 = new Activity
            {
                Title = "Meeting 2",
                Description = "Second meeting",
                Location = "Room 102",
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddHours(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 3,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity2);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity2.Id, 1);

            // Assert
            Assert.True(result.success);
            Assert.Equal(0, result.organizingClubPoints); // No points awarded

            // Verify detail record created with 0 score and note
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.NotNull(record);
            Assert.Equal(5, record.ClubMeetingScore); // Still 5 from first activity
            
            var zeroPointDetail = record.Details.FirstOrDefault(d => d.ActivityId == activity2.Id);
            Assert.NotNull(zeroPointDetail);
            Assert.Equal(0, zeroPointDetail.Score);
            Assert.Contains("weekly limit", zeroPointDetail.Note!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CompleteActivity_SemesterLimit_ShouldCapAt100Points()
        {
            // Arrange - Create multiple activities to exceed 100 points
            var activities = new List<Activity>();
            for (int i = 0; i < 6; i++)
            {
                var activity = new Activity
                {
                    Title = $"Large Event {i + 1}",
                    Description = $"Event {i + 1}",
                    Location = "Main Hall",
                    StartTime = DateTime.UtcNow.AddDays(-2 - i),
                    EndTime = DateTime.UtcNow.AddDays(-1 - i),
                    Type = ActivityType.LargeEvent,
                    ClubId = 1,
                    CreatedById = 2,
                    Status = "Approved",
                    IsPublic = true,
                    MovementPoint = 20,
                    RequiresApproval = true
                };
                await _activityRepository.CreateAsync(activity);
                activities.Add(activity);
            }

            // Act - Complete all activities (6 * 20 = 120 points)
            foreach (var activity in activities)
            {
                await _activityService.CompleteActivityAsync(activity.Id, 1);
            }

            // Assert - Total should be capped at 100
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.NotNull(record);
            Assert.Equal(100, record.TotalScore); // Capped at 100
            Assert.Equal(120, record.EventScore); // EventScore can exceed 100, but TotalScore is capped
        }

        [Fact]
        public async Task CompleteActivity_CollaborationActivity_ShouldAwardBothClubs()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Club Collaboration Event",
                Description = "Collaboration between IT and English clubs",
                Location = "Conference Room",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubCollaboration,
                ClubId = 1, // IT Club
                ClubCollaborationId = 2, // English Club
                CreatedById = 2,
                Status = "Approved",
                IsPublic = true,
                MovementPoint = 10,
                CollaborationPoint = 5,
                RequiresApproval = true
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.True(result.success);
            Assert.Equal(10, result.organizingClubPoints);
            Assert.Equal(5, result.collaboratingClubPoints);

            // Verify organizing club record
            var organizingRecord = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.NotNull(organizingRecord);
            Assert.Equal(10, organizingRecord.CollaborationScore);

            // Verify collaborating club record
            var collaboratingRecord = await _clubMovementRecordRepository.GetByClubMonthAsync(2, 1, DateTime.UtcNow.Month);
            Assert.NotNull(collaboratingRecord);
            Assert.Equal(5, collaboratingRecord.CollaborationScore);
        }

        [Fact]
        public async Task CompleteActivity_NotApproved_ShouldFail()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Pending Activity",
                Description = "Not yet approved",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "PendingApproval",
                IsPublic = false,
                MovementPoint = 5,
                RequiresApproval = true
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.False(result.success);
            Assert.Contains("approved", result.message, StringComparison.OrdinalIgnoreCase);

            // Verify no points awarded
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.Null(record);
        }

        [Fact]
        public async Task CompleteActivity_NotEnded_ShouldFail()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Future Activity",
                Description = "Not yet ended",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Approved",
                IsPublic = false,
                MovementPoint = 5,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.False(result.success);
            Assert.Contains("not ended", result.message, StringComparison.OrdinalIgnoreCase);

            // Verify no points awarded
            var record = await _clubMovementRecordRepository.GetByClubMonthAsync(1, 1, DateTime.UtcNow.Month);
            Assert.Null(record);
        }

        [Fact]
        public async Task CompleteActivity_AlreadyCompleted_ShouldFail()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Completed Activity",
                Description = "Already completed",
                Location = "Room 101",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.ClubMeeting,
                ClubId = 1,
                CreatedById = 2,
                Status = "Completed",
                IsPublic = false,
                MovementPoint = 5,
                RequiresApproval = false
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.False(result.success);
            // Note: The service checks "Approved" status first, so a "Completed" activity will fail with "must be approved" message
            Assert.Contains("must be approved", result.message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CompleteActivity_NoClub_ShouldCompleteWithoutPoints()
        {
            // Arrange - Admin-created public activity with no club
            var activity = new Activity
            {
                Title = "School-wide Event",
                Description = "Public event by admin",
                Location = "Main Hall",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Type = ActivityType.LargeEvent,
                ClubId = null, // No club
                CreatedById = 1,
                Status = "Approved",
                IsPublic = true,
                MovementPoint = 0,
                RequiresApproval = true
            };
            await _activityRepository.CreateAsync(activity);

            // Act
            var result = await _activityService.CompleteActivityAsync(activity.Id, 1);

            // Assert
            Assert.True(result.success);
            Assert.Equal(0, result.organizingClubPoints);
            Assert.True(!result.collaboratingClubPoints.HasValue || result.collaboratingClubPoints.Value == 0);

            // Verify activity status updated
            var updatedActivity = await _activityRepository.GetByIdAsync(activity.Id);
            Assert.Equal("Completed", updatedActivity!.Status);

            // Verify no ClubMovementRecord created
            var records = await _context.ClubMovementRecords.ToListAsync();
            Assert.Empty(records);
        }

        // Note: CriterionNotFound test removed because all ActivityTypes have criterion mappings in the system
        // ClubTraining, ClubWorkshop, ClubMeeting all map to "Sinh hoạt CLB" pattern

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
