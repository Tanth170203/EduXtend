using BusinessObject.Enum;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Repositories.ActivitySchedules;
using Repositories.ActivityScheduleAssignments;

namespace Services.Tests.Activities
{
    public class ActivityScheduleRepositoryTests : IDisposable
    {
        private readonly EduXtendContext _context;
        private readonly ActivityScheduleRepository _scheduleRepo;
        private readonly ActivityScheduleAssignmentRepository _assignmentRepo;

        public ActivityScheduleRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<EduXtendContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EduXtendContext(options);
            _scheduleRepo = new ActivityScheduleRepository(_context);
            _assignmentRepo = new ActivityScheduleAssignmentRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region ActivitySchedule CRUD Tests

        [Fact]
        public async Task AddAsync_CreatesSchedule()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };

            // Act
            var result = await _scheduleRepo.AddAsync(schedule);

            // Assert
            Assert.NotEqual(0, result.Id);
            Assert.Equal("Test Schedule", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsScheduleWithAssignments()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _scheduleRepo.GetByIdAsync(schedule.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Schedule", result.Title);
            Assert.Single(result.Assignments);
            Assert.Equal("John Doe", result.Assignments.First().ResponsibleName);
        }

        [Fact]
        public async Task GetByActivityIdAsync_ReturnsSchedulesOrderedByOrder()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule1 = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                Title = "Second Schedule",
                Order = 2,
                CreatedAt = DateTime.Now
            };
            var schedule2 = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "First Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.AddRange(schedule1, schedule2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _scheduleRepo.GetByActivityIdAsync(activity.Id);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("First Schedule", result[0].Title);
            Assert.Equal("Second Schedule", result[1].Title);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesSchedule()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Original Title",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Act
            schedule.Title = "Updated Title";
            await _scheduleRepo.UpdateAsync(schedule);

            // Assert
            var updated = await _context.ActivitySchedules.FindAsync(schedule.Id);
            Assert.NotNull(updated);
            Assert.Equal("Updated Title", updated.Title);
        }

        [Fact]
        public async Task DeleteAsync_DeletesSchedule()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Act
            await _scheduleRepo.DeleteAsync(schedule.Id);

            // Assert
            var deleted = await _context.ActivitySchedules.FindAsync(schedule.Id);
            Assert.Null(deleted);
        }

        #endregion

        #region ActivityScheduleAssignment CRUD Tests

        [Fact]
        public async Task Assignment_AddAsync_CreatesAssignment()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };

            // Act
            var result = await _assignmentRepo.AddAsync(assignment);

            // Assert
            Assert.NotEqual(0, result.Id);
            Assert.Equal("John Doe", result.ResponsibleName);
            Assert.Equal("Speaker", result.Role);
        }

        [Fact]
        public async Task Assignment_GetByIdAsync_ReturnsAssignment()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _assignmentRepo.GetByIdAsync(assignment.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.ResponsibleName);
        }

        [Fact]
        public async Task Assignment_GetByScheduleIdAsync_ReturnsAllAssignments()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment1 = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            var assignment2 = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "Jane Smith",
                Role = "MC",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.AddRange(assignment1, assignment2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _assignmentRepo.GetByScheduleIdAsync(schedule.Id);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.ResponsibleName == "John Doe");
            Assert.Contains(result, a => a.ResponsibleName == "Jane Smith");
        }

        [Fact]
        public async Task Assignment_UpdateAsync_UpdatesAssignment()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Act
            assignment.Role = "MC";
            await _assignmentRepo.UpdateAsync(assignment);

            // Assert
            var updated = await _context.ActivityScheduleAssignments.FindAsync(assignment.Id);
            Assert.NotNull(updated);
            Assert.Equal("MC", updated.Role);
        }

        [Fact]
        public async Task Assignment_DeleteAsync_DeletesAssignment()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Act
            await _assignmentRepo.DeleteAsync(assignment.Id);

            // Assert
            var deleted = await _context.ActivityScheduleAssignments.FindAsync(assignment.Id);
            Assert.Null(deleted);
        }

        #endregion

        #region Cascade Delete Tests

        [Fact]
        public async Task DeleteSchedule_CascadeDeletesAssignments()
        {
            // Arrange
            var activity = new Activity
            {
                Title = "Test Activity",
                Type = ActivityType.LargeEvent,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Status = "Approved",
                CreatedById = 1,
                CreatedAt = DateTime.Now
            };
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var schedule = new ActivitySchedule
            {
                ActivityId = activity.Id,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Title = "Test Schedule",
                Order = 1,
                CreatedAt = DateTime.Now
            };
            _context.ActivitySchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var assignment1 = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "John Doe",
                Role = "Speaker",
                CreatedAt = DateTime.Now
            };
            var assignment2 = new ActivityScheduleAssignment
            {
                ActivityScheduleId = schedule.Id,
                ResponsibleName = "Jane Smith",
                Role = "MC",
                CreatedAt = DateTime.Now
            };
            _context.ActivityScheduleAssignments.AddRange(assignment1, assignment2);
            await _context.SaveChangesAsync();

            var assignmentIds = new[] { assignment1.Id, assignment2.Id };

            // Act
            await _scheduleRepo.DeleteAsync(schedule.Id);

            // Assert
            var deletedSchedule = await _context.ActivitySchedules.FindAsync(schedule.Id);
            Assert.Null(deletedSchedule);

            // Check if assignments are also deleted (cascade)
            var remainingAssignments = await _context.ActivityScheduleAssignments
                .Where(a => assignmentIds.Contains(a.Id))
                .ToListAsync();
            Assert.Empty(remainingAssignments);
        }

        #endregion
    }
}
