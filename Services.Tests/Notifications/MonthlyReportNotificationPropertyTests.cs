using BusinessObject.Models;
using DataAccess;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Repositories.Notifications;
using Services.Notifications;
using Xunit;

namespace Services.Tests.Notifications;

/// <summary>
/// Property-based tests for notification service functionality
/// Feature: monthly-report-submission-notifications
/// </summary>
public class MonthlyReportNotificationPropertyTests : IDisposable
{
    private EduXtendContext _context = null!;
    private NotificationService _service = null!;

    public MonthlyReportNotificationPropertyTests()
    {
        SetupTestContext();
    }

    private void SetupTestContext()
    {
        var options = new DbContextOptionsBuilder<EduXtendContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduXtendContext(options);
        var repo = new NotificationRepository(_context);
        _service = new NotificationService(repo, _context);

        // Seed admin role
        _context.Roles.Add(new Role { Id = 1, RoleName = "Admin" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Feature: monthly-report-submission-notifications, Property 1: Notification creation preserves all data
    /// Validates: Requirements 1.1, 4.1, 4.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NotificationCreationPreservesAllData()
    {
        return Prop.ForAll(
            GenerateNotificationData(),
            (notificationData) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test
                
                // Add target user
                var user = new User
                {
                    Id = notificationData.TargetUserId,
                    Email = $"user{notificationData.TargetUserId}@test.com",
                    FullName = $"User {notificationData.TargetUserId}",
                    RoleId = 1,
                    IsActive = true
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                var notification = new Notification
                {
                    Title = notificationData.Title,
                    Message = notificationData.Message,
                    Scope = "User",
                    TargetUserId = notificationData.TargetUserId,
                    CreatedById = 1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Act
                var result = _service.CreateAsync(notification).Result;

                // Assert
                var savedNotification = _context.Notifications.Find(result.Id);
                
                return savedNotification != null &&
                       savedNotification.Title == notificationData.Title &&
                       savedNotification.Message == notificationData.Message &&
                       savedNotification.TargetUserId == notificationData.TargetUserId &&
                       savedNotification.IsRead == false;
            }
        );
    }

    /// <summary>
    /// Feature: monthly-report-submission-notifications, Property 2: Mark as read updates notification state
    /// Validates: Requirements 7.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MarkAsReadUpdatesNotificationState()
    {
        return Prop.ForAll(
            GenerateNotificationData(),
            (notificationData) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test
                
                // Add target user
                var user = new User
                {
                    Id = notificationData.TargetUserId,
                    Email = $"user{notificationData.TargetUserId}@test.com",
                    FullName = $"User {notificationData.TargetUserId}",
                    RoleId = 1,
                    IsActive = true
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                var notification = new Notification
                {
                    Title = notificationData.Title,
                    Message = notificationData.Message,
                    Scope = "User",
                    TargetUserId = notificationData.TargetUserId,
                    CreatedById = 1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                var created = _service.CreateAsync(notification).Result;

                // Act
                _service.MarkAsReadAsync(created.Id).Wait();

                // Assert
                var updatedNotification = _context.Notifications.Find(created.Id);
                
                return updatedNotification != null && updatedNotification.IsRead == true;
            }
        );
    }

    // Generators
    private static Arbitrary<NotificationData> GenerateNotificationData()
    {
        return Arb.From(
            Gen.Choose(1, 100)
                .SelectMany(userId =>
                    Gen.Elements("Report Submitted", "Report Approved", "Report Rejected", "New Activity")
                        .SelectMany(title =>
                            Gen.Elements(
                                "Your monthly report has been submitted",
                                "Your report has been approved by admin",
                                "Your report needs revision",
                                "A new activity has been created"
                            ).Select(message => new NotificationData
                            {
                                TargetUserId = userId,
                                Title = title,
                                Message = message
                            })
                        )
                )
        );
    }

    public class NotificationData
    {
        public int TargetUserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
