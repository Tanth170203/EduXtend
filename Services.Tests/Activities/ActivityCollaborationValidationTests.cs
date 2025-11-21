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
    public class ActivityCollaborationValidationTests
    {
        private readonly Mock<IActivityRepository> _mockRepo;
        private readonly Mock<IStudentRepository> _mockStudentRepo;
        private readonly Mock<IClubRepository> _mockClubRepo;
        private readonly Mock<IActivityScheduleRepository> _mockScheduleRepo;
        private readonly Mock<IActivityScheduleAssignmentRepository> _mockAssignmentRepo;
        private readonly Mock<IMovementRecordService> _mockMovementRecordService;
        private readonly Mock<ILogger<ActivityService>> _mockLogger;
        private readonly ActivityService _service;

        public ActivityCollaborationValidationTests()
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

        #region Admin ClubCollaboration Tests

        [Fact]
        public async Task ValidateCollaborationSettings_AdminClubCollaboration_ValidSettings_ShouldPass()
        {
            // Arrange
            var clubId = 1;
            _mockRepo.Setup(r => r.GetClubByIdAsync(clubId))
                .ReturnsAsync(new Club { Id = clubId, Name = "Test Club" });

            // Act & Assert
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.ClubCollaboration,
                "Admin",
                null,
                clubId,
                2,
                0
            );
        }

        [Fact]
        public async Task ValidateCollaborationSettings_AdminClubCollaboration_MissingClubId_ShouldThrow()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "Admin",
                    null,
                    null,
                    2,
                    0
                )
            );

            Assert.Contains("Collaborating club must be selected", exception.Message);
        }

        [Fact]
        public async Task ValidateCollaborationSettings_AdminClubCollaboration_InvalidClubId_ShouldThrow()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetClubByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Club?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "Admin",
                    null,
                    999,
                    2,
                    0
                )
            );

            Assert.Contains("does not exist", exception.Message);
        }

        [Fact]
        public async Task ValidateCollaborationSettings_AdminClubCollaboration_MissingCollaborationPoint_ShouldThrow()
        {
            // Arrange
            var clubId = 1;
            _mockRepo.Setup(r => r.GetClubByIdAsync(clubId))
                .ReturnsAsync(new Club { Id = clubId, Name = "Test Club" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "Admin",
                    null,
                    clubId,
                    null,
                    0
                )
            );

            Assert.Contains("Collaboration point must be set", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(10)]
        public async Task ValidateCollaborationSettings_AdminClubCollaboration_InvalidCollaborationPointRange_ShouldThrow(int invalidPoint)
        {
            // Arrange
            var clubId = 1;
            _mockRepo.Setup(r => r.GetClubByIdAsync(clubId))
                .ReturnsAsync(new Club { Id = clubId, Name = "Test Club" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "Admin",
                    null,
                    clubId,
                    invalidPoint,
                    0
                )
            );

            Assert.Contains("between 1 and 3", exception.Message);
        }

        #endregion

        #region Admin SchoolCollaboration Tests

        [Fact]
        public async Task ValidateCollaborationSettings_AdminSchoolCollaboration_ValidSettings_ShouldPass()
        {
            // Arrange
            var clubId = 1;
            _mockRepo.Setup(r => r.GetClubByIdAsync(clubId))
                .ReturnsAsync(new Club { Id = clubId, Name = "Test Club" });

            // Act & Assert
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.SchoolCollaboration,
                "Admin",
                null,
                clubId,
                3,
                0
            );
        }

        #endregion

        #region ClubManager ClubCollaboration Tests

        [Fact]
        public async Task ValidateCollaborationSettings_ClubManagerClubCollaboration_ValidSettings_ShouldPass()
        {
            // Arrange
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });

            // Act & Assert
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.ClubCollaboration,
                "ClubManager",
                organizingClubId,
                collaboratingClubId,
                2,
                5
            );
        }

        [Fact]
        public async Task ValidateCollaborationSettings_ClubManagerClubCollaboration_SameClub_ShouldThrow()
        {
            // Arrange
            var clubId = 1;
            _mockRepo.Setup(r => r.GetClubByIdAsync(clubId))
                .ReturnsAsync(new Club { Id = clubId, Name = "Test Club" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "ClubManager",
                    clubId,
                    clubId,
                    2,
                    5
                )
            );

            Assert.Contains("Cannot collaborate with your own club", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        [InlineData(-1)]
        public async Task ValidateCollaborationSettings_ClubManagerClubCollaboration_InvalidMovementPoint_ShouldThrow(double invalidPoint)
        {
            // Arrange
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.ClubCollaboration,
                    "ClubManager",
                    organizingClubId,
                    collaboratingClubId,
                    2,
                    invalidPoint
                )
            );

            Assert.Contains("Movement point must be between 1 and 10", exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task ValidateCollaborationSettings_ClubManagerClubCollaboration_ValidMovementPointRange_ShouldPass(double validPoint)
        {
            // Arrange
            var organizingClubId = 1;
            var collaboratingClubId = 2;
            _mockRepo.Setup(r => r.GetClubByIdAsync(collaboratingClubId))
                .ReturnsAsync(new Club { Id = collaboratingClubId, Name = "Collaborating Club" });

            // Act & Assert
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.ClubCollaboration,
                "ClubManager",
                organizingClubId,
                collaboratingClubId,
                2,
                validPoint
            );
        }

        #endregion

        #region ClubManager SchoolCollaboration Tests

        [Fact]
        public async Task ValidateCollaborationSettings_ClubManagerSchoolCollaboration_ValidSettings_ShouldPass()
        {
            // Act & Assert
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.SchoolCollaboration,
                "ClubManager",
                1,
                null,
                null,
                5
            );
        }

        [Fact]
        public async Task ValidateCollaborationSettings_ClubManagerSchoolCollaboration_WithClubId_ShouldThrow()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.SchoolCollaboration,
                    "ClubManager",
                    1,
                    2,
                    null,
                    5
                )
            );

            Assert.Contains("should not be set for school collaboration", exception.Message);
        }

        [Fact]
        public async Task ValidateCollaborationSettings_ClubManagerSchoolCollaboration_WithCollaborationPoint_ShouldThrow()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.SchoolCollaboration,
                    "ClubManager",
                    1,
                    null,
                    2,
                    5
                )
            );

            Assert.Contains("Collaboration point should not be set", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public async Task ValidateCollaborationSettings_ClubManagerSchoolCollaboration_InvalidMovementPoint_ShouldThrow(double invalidPoint)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ValidateCollaborationSettingsAsync(
                    ActivityType.SchoolCollaboration,
                    "ClubManager",
                    1,
                    null,
                    null,
                    invalidPoint
                )
            );

            Assert.Contains("Movement point must be between 1 and 10", exception.Message);
        }

        #endregion

        #region Non-Collaboration Activity Tests

        [Fact]
        public async Task ValidateCollaborationSettings_NonCollaborationType_ShouldNotValidate()
        {
            // Act & Assert - Should not throw for non-collaboration types
            await _service.ValidateCollaborationSettingsAsync(
                ActivityType.LargeEvent,
                "ClubManager",
                1,
                null,
                null,
                5
            );
        }

        #endregion
    }
}
