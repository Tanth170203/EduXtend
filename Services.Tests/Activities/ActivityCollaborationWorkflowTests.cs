using BusinessObject.DTOs.Activity;
using BusinessObject.Enum;
using BusinessObject.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Activities;
using Repositories.Students;
using Repositories.Clubs;
using Repositories.ActivitySchedules;
using Repositories.ActivityScheduleAssignments;
using Services.Activities;
using Services.MovementRecords;

namespace Services.Tests.Activities
{
    /// <summary>
    /// Integration-style tests for collaboration workflows
    /// Tests the complete flow from creation to registration to point assignment
    /// </summary>
    public class ActivityCollaborationWorkflowTests
    {
        private readonly Mock<IActivityRepository> _mockRepo;
        private readonly Mock<IStudentRepository> _mockStudentRepo;
        private readonly Mock<IClubRepository> _mockClubRepo;
        private readonly Mock<IActivityScheduleRepository> _mockScheduleRepo;
        private readonly Mock<IActivityScheduleAssignmentRepository> _mockAssignmentRepo;
        private readonly Mock<IMovementRecordService> _mockMovementRecordService;
        private readonly Mock<ILogger<ActivityService>> _mockLogger;
        private readonly ActivityService _service;

        public ActivityCollaborationWorkflowTests()
        {
            _mockRepo = new Mock<IActivityRepository>();
            _mockStudentRepo = new Mock<IStudentRepository>();
            _mockClubRepo = new Mock<IClubRepository>();
            _mockScheduleRepo = new Mock<IActivityScheduleRepository>();
            _mockAssignmentRepo = new Mock<IActivityScheduleAssignmentRepository>();
            _mockMovementRecordService = new Mock<IMovementRecordService>();
            var mockClubMovementRecordService = new Mock<Services.ClubMovementRecords.IClubMovementRecordService>();
            _mockLogger = new Mock<ILogger<ActivityService>>();
            
            _service = new ActivityService(
                _mockRepo.Object,
                _mockStudentRepo.Object,
                _mockClubRepo.Object,
                _mockScheduleRepo.Object,
                _mockAssignmentRepo.Object,
                _mockMovementRecordService.Object,
                mockClubMovementRecordService.Object,
                _mockLogger.Object
            );
        }

        #region Admin ClubCollaboration Workflow

        [Fact]
        public async Task AdminCreateClubCollaboration_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var adminUserId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });
            
            _mockRepo.Setup(r => r.IsAttendanceCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Activity>()))
                .ReturnsAsync((Activity a) => { a.Id = 100; return a; });
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(100))
                .ReturnsAsync(new Activity
                {
                    Id = 100,
                    Title = "Test Collaboration",
                    Type = ActivityType.ClubCollaboration,
                    ClubCollaborationId = collaboratingClubId,
                    CollaborationPoint = 2,
                    Status = "Approved",
                    CreatedById = adminUserId,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    MovementPoint = 0,
                    IsPublic = true,
                    CreatedBy = new User { Id = adminUserId, FullName = "Admin User" },
                    CollaboratingClub = new Club { Id = collaboratingClubId, Name = "Collaborating Club" }
                });
            
            _mockRepo.Setup(r => r.GetRegistrationCountAsync(100)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetAttendanceCountAsync(100)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetFeedbackCountAsync(100)).ReturnsAsync(0);

            var dto = new AdminCreateActivityDto
            {
                Title = "Test Collaboration",
                Description = "Test Description",
                Location = "Test Location",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                Type = ActivityType.ClubCollaboration,
                IsPublic = true,
                ClubCollaborationId = collaboratingClubId,
                CollaborationPoint = 2,
                MovementPoint = 0
            };

            // Act
            var result = await _service.AdminCreateAsync(adminUserId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal(collaboratingClubId, result.ClubCollaborationId);
            Assert.Equal(2, result.CollaborationPoint);
            Assert.Equal("Collaborating Club", result.CollaboratingClubName);
            
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Activity>(a => 
                a.ClubCollaborationId == collaboratingClubId &&
                a.CollaborationPoint == 2 &&
                a.Type == ActivityType.ClubCollaboration
            )), Times.Once);
        }

        #endregion

        #region Admin SchoolCollaboration Workflow

        [Fact]
        public async Task AdminCreateSchoolCollaboration_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var adminUserId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });
            
            _mockRepo.Setup(r => r.IsAttendanceCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Activity>()))
                .ReturnsAsync((Activity a) => { a.Id = 101; return a; });
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(101))
                .ReturnsAsync(new Activity
                {
                    Id = 101,
                    Title = "School Collaboration",
                    Type = ActivityType.SchoolCollaboration,
                    ClubCollaborationId = collaboratingClubId,
                    CollaborationPoint = 3,
                    Status = "Approved",
                    CreatedById = adminUserId,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    MovementPoint = 0,
                    IsPublic = true,
                    CreatedBy = new User { Id = adminUserId, FullName = "Admin User" },
                    CollaboratingClub = new Club { Id = collaboratingClubId, Name = "Collaborating Club" }
                });
            
            _mockRepo.Setup(r => r.GetRegistrationCountAsync(101)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetAttendanceCountAsync(101)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetFeedbackCountAsync(101)).ReturnsAsync(0);

            var dto = new AdminCreateActivityDto
            {
                Title = "School Collaboration",
                Description = "Test Description",
                Location = "Test Location",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                Type = ActivityType.SchoolCollaboration,
                IsPublic = true,
                ClubCollaborationId = collaboratingClubId,
                CollaborationPoint = 3,
                MovementPoint = 0
            };

            // Act
            var result = await _service.AdminCreateAsync(adminUserId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(101, result.Id);
            Assert.Equal(collaboratingClubId, result.ClubCollaborationId);
            Assert.Equal(3, result.CollaborationPoint);
            
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Activity>(a => 
                a.Type == ActivityType.SchoolCollaboration &&
                a.ClubCollaborationId == collaboratingClubId &&
                a.CollaborationPoint == 3
            )), Times.Once);
        }

        #endregion

        #region ClubManager ClubCollaboration Workflow

        [Fact]
        public async Task ClubManagerCreateClubCollaboration_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var managerId = 1;
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });
            
            _mockRepo.Setup(r => r.IsAttendanceCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Activity>()))
                .ReturnsAsync((Activity a) => { a.Id = 102; return a; });
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(102))
                .ReturnsAsync(new Activity
                {
                    Id = 102,
                    Title = "Club Collaboration",
                    Type = ActivityType.ClubCollaboration,
                    ClubId = organizingClubId,
                    ClubCollaborationId = collaboratingClubId,
                    CollaborationPoint = 2,
                    MovementPoint = 5,
                    Status = "PendingApproval",
                    CreatedById = managerId,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    IsPublic = false,
                    CreatedBy = new User { Id = managerId, FullName = "Manager User" },
                    Club = new Club { Id = organizingClubId, Name = "Organizing Club" },
                    CollaboratingClub = new Club { Id = collaboratingClubId, Name = "Collaborating Club" }
                });
            
            _mockRepo.Setup(r => r.GetRegistrationCountAsync(102)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetAttendanceCountAsync(102)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetFeedbackCountAsync(102)).ReturnsAsync(0);

            var dto = new ClubCreateActivityDto
            {
                Title = "Club Collaboration",
                Description = "Test Description",
                Location = "Test Location",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                Type = ActivityType.ClubCollaboration,
                IsPublic = false,
                ClubCollaborationId = collaboratingClubId,
                CollaborationPoint = 2,
                MovementPoint = 5,
                IsMandatory = false
            };

            // Act
            var result = await _service.ClubCreateAsync(managerId, organizingClubId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(102, result.Id);
            Assert.Equal(organizingClubId, result.ClubId);
            Assert.Equal(collaboratingClubId, result.ClubCollaborationId);
            Assert.Equal(2, result.CollaborationPoint);
            Assert.Equal(5, result.MovementPoint);
            
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Activity>(a => 
                a.Type == ActivityType.ClubCollaboration &&
                a.ClubId == organizingClubId &&
                a.ClubCollaborationId == collaboratingClubId &&
                a.CollaborationPoint == 2 &&
                a.MovementPoint == 5
            )), Times.Once);
        }

        #endregion

        #region ClubManager SchoolCollaboration Workflow

        [Fact]
        public async Task ClubManagerCreateSchoolCollaboration_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var managerId = 1;
            var organizingClubId = 1;
            
            _mockRepo.Setup(r => r.IsAttendanceCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Activity>()))
                .ReturnsAsync((Activity a) => { a.Id = 103; return a; });
            
            _mockRepo.Setup(r => r.GetByIdWithDetailsAsync(103))
                .ReturnsAsync(new Activity
                {
                    Id = 103,
                    Title = "School Collaboration",
                    Type = ActivityType.SchoolCollaboration,
                    ClubId = organizingClubId,
                    ClubCollaborationId = null,
                    CollaborationPoint = null,
                    MovementPoint = 7,
                    Status = "PendingApproval",
                    CreatedById = managerId,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    IsPublic = true,
                    CreatedBy = new User { Id = managerId, FullName = "Manager User" },
                    Club = new Club { Id = organizingClubId, Name = "Organizing Club" }
                });
            
            _mockRepo.Setup(r => r.GetRegistrationCountAsync(103)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetAttendanceCountAsync(103)).ReturnsAsync(0);
            _mockRepo.Setup(r => r.GetFeedbackCountAsync(103)).ReturnsAsync(0);

            var dto = new ClubCreateActivityDto
            {
                Title = "School Collaboration",
                Description = "Test Description",
                Location = "Test Location",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                Type = ActivityType.SchoolCollaboration,
                IsPublic = true,
                ClubCollaborationId = null,
                CollaborationPoint = null,
                MovementPoint = 7,
                IsMandatory = false
            };

            // Act
            var result = await _service.ClubCreateAsync(managerId, organizingClubId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(103, result.Id);
            Assert.Equal(organizingClubId, result.ClubId);
            Assert.Null(result.ClubCollaborationId);
            Assert.Null(result.CollaborationPoint);
            Assert.Equal(7, result.MovementPoint);
            
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Activity>(a => 
                a.Type == ActivityType.SchoolCollaboration &&
                a.ClubId == organizingClubId &&
                a.ClubCollaborationId == null &&
                a.CollaborationPoint == null &&
                a.MovementPoint == 7
            )), Times.Once);
        }

        #endregion

        #region Registration Eligibility Tests

        [Fact]
        public async Task RegisterAsync_PublicClubCollaboration_AnyUser_ShouldSucceed()
        {
            // Arrange
            var userId = 10;
            var activityId = 100;
            
            _mockRepo.Setup(r => r.GetByIdAsync(activityId))
                .ReturnsAsync(new Activity
                {
                    Id = activityId,
                    Type = ActivityType.ClubCollaboration,
                    ClubId = 1,
                    ClubCollaborationId = 2,
                    Status = "Approved",
                    IsPublic = true,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2)
                });
            
            _mockRepo.Setup(r => r.GetRegistrationAsync(activityId, userId))
                .ReturnsAsync((ActivityRegistration?)null);
            
            _mockRepo.Setup(r => r.IsRegisteredAsync(activityId, userId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.AddRegistrationAsync(activityId, userId))
                .ReturnsAsync(new ActivityRegistration { ActivityId = activityId, UserId = userId, Status = "Registered" });

            // Act
            var result = await _service.RegisterAsync(userId, activityId);

            // Assert
            Assert.True(result.success);
            _mockRepo.Verify(r => r.AddRegistrationAsync(activityId, userId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_NonPublicClubCollaboration_OrganizerMember_ShouldSucceed()
        {
            // Arrange
            var userId = 10;
            var activityId = 100;
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetByIdAsync(activityId))
                .ReturnsAsync(new Activity
                {
                    Id = activityId,
                    Type = ActivityType.ClubCollaboration,
                    ClubId = organizingClubId,
                    ClubCollaborationId = collaboratingClubId,
                    Status = "Approved",
                    IsPublic = false,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2)
                });
            
            _mockRepo.Setup(r => r.GetRegistrationAsync(activityId, userId))
                .ReturnsAsync((ActivityRegistration?)null);
            
            _mockRepo.Setup(r => r.IsRegisteredAsync(activityId, userId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, organizingClubId))
                .ReturnsAsync(true);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, collaboratingClubId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.AddRegistrationAsync(activityId, userId))
                .ReturnsAsync(new ActivityRegistration { ActivityId = activityId, UserId = userId, Status = "Registered" });

            // Act
            var result = await _service.RegisterAsync(userId, activityId);

            // Assert
            Assert.True(result.success);
            _mockRepo.Verify(r => r.AddRegistrationAsync(activityId, userId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_NonPublicClubCollaboration_CollaboratorMember_ShouldSucceed()
        {
            // Arrange
            var userId = 10;
            var activityId = 100;
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetByIdAsync(activityId))
                .ReturnsAsync(new Activity
                {
                    Id = activityId,
                    Type = ActivityType.ClubCollaboration,
                    ClubId = organizingClubId,
                    ClubCollaborationId = collaboratingClubId,
                    Status = "Approved",
                    IsPublic = false,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2)
                });
            
            _mockRepo.Setup(r => r.GetRegistrationAsync(activityId, userId))
                .ReturnsAsync((ActivityRegistration?)null);
            
            _mockRepo.Setup(r => r.IsRegisteredAsync(activityId, userId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, organizingClubId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, collaboratingClubId))
                .ReturnsAsync(true);
            
            _mockRepo.Setup(r => r.AddRegistrationAsync(activityId, userId))
                .ReturnsAsync(new ActivityRegistration { ActivityId = activityId, UserId = userId, Status = "Registered" });

            // Act
            var result = await _service.RegisterAsync(userId, activityId);

            // Assert
            Assert.True(result.success);
            _mockRepo.Verify(r => r.AddRegistrationAsync(activityId, userId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_NonPublicClubCollaboration_NonMember_ShouldFail()
        {
            // Arrange
            var userId = 10;
            var activityId = 100;
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            
            _mockRepo.Setup(r => r.GetByIdAsync(activityId))
                .ReturnsAsync(new Activity
                {
                    Id = activityId,
                    Type = ActivityType.ClubCollaboration,
                    ClubId = organizingClubId,
                    ClubCollaborationId = collaboratingClubId,
                    Status = "Approved",
                    IsPublic = false,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2)
                });
            
            _mockRepo.Setup(r => r.GetRegistrationAsync(activityId, userId))
                .ReturnsAsync((ActivityRegistration?)null);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, organizingClubId))
                .ReturnsAsync(false);
            
            _mockRepo.Setup(r => r.IsUserMemberOfClubAsync(userId, collaboratingClubId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.RegisterAsync(userId, activityId);

            // Assert
            Assert.False(result.success);
            Assert.Contains("organizing or collaborating clubs only", result.message);
            _mockRepo.Verify(r => r.AddRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion
    }
}
