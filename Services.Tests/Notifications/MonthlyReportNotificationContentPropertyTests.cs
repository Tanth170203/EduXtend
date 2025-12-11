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
/// Property-based tests for notification content validation
/// Feature: monthly-report-submission-notifications
/// </summary>
public class MonthlyReportNotificationContentPropertyTests : IDisposable
{
    private EduXtendContext _context = null!;
    private NotificationService _service = null!;

    public MonthlyReportNotificationContentPropertyTests()
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

        // Seed admin role and one admin user
        _context.Roles.Add(new Role { Id = 1, RoleName = "Admin" });
        _context.Users.Add(new User
        {
            Id = 1,
            Email = "admin@test.com",
            FullName = "Test Admin",
            RoleId = 1,
            IsActive = true
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Feature: monthly-report-submission-notifications, Property 2: Notification content is preserved
    /// Validates: Requirements 1.4, 3.1, 3.2, 3.3, 3.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NotificationContentIsPreserved()
    {
        return Prop.ForAll(
            GenerateReportNotificationData(),
            (reportData) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test

                var message = $"Báo cáo tháng {reportData.ReportMonth}/{reportData.ReportYear} của CLB {reportData.ClubName} đã được nộp.";
                
                var notification = new Notification
                {
                    Title = "Báo cáo mới được nộp",
                    Message = message,
                    Scope = "User",
                    TargetUserId = 1,
                    CreatedById = 1,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Act
                var result = _service.CreateAsync(notification).Result;

                // Assert
                var savedNotification = _context.Notifications.Find(result.Id);

                if (savedNotification == null)
                    return false;

                // Check that notification message contains all required information
                var savedMessage = savedNotification.Message ?? "";
                var containsClubName = savedMessage.Contains(reportData.ClubName);
                var containsReportMonth = savedMessage.Contains(reportData.ReportMonth.ToString());
                var containsReportYear = savedMessage.Contains(reportData.ReportYear.ToString());

                return containsClubName && containsReportMonth && containsReportYear;
            }
        );
    }

    /// <summary>
    /// Feature: monthly-report-submission-notifications, Property 3: Get notifications by user returns correct data
    /// Validates: Requirements 7.1, 7.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GetNotificationsByUserReturnsCorrectData()
    {
        return Prop.ForAll(
            Gen.Choose(1, 5).ToArbitrary(),
            (notificationCount) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test
                
                var userId = 1;

                // Create multiple notifications for the user
                for (int i = 0; i < notificationCount; i++)
                {
                    var notification = new Notification
                    {
                        Title = $"Notification {i + 1}",
                        Message = $"Message {i + 1}",
                        Scope = "User",
                        TargetUserId = userId,
                        CreatedById = 1,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                    };
                    _service.CreateAsync(notification).Wait();
                }

                // Act
                var notifications = _service.GetByUserIdAsync(userId).Result;

                // Assert
                return notifications.Count == notificationCount;
            }
        );
    }

    // Generators
    private static Arbitrary<ReportNotificationData> GenerateReportNotificationData()
    {
        return Arb.From(
            Gen.Choose(1, 10000)
                .SelectMany(reportId =>
                    GenerateClubName()
                        .SelectMany(clubName =>
                            Gen.Choose(1, 12)
                                .SelectMany(month =>
                                    Gen.Choose(2020, 2030)
                                        .Select(year => new ReportNotificationData
                                        {
                                            ReportId = reportId,
                                            ClubName = clubName,
                                            ReportMonth = month,
                                            ReportYear = year
                                        })
                                )
                        )
                )
        );
    }

    private static Gen<string> GenerateClubName()
    {
        return Gen.Elements(
            "Tech Club",
            "Sports Club",
            "Music Club",
            "Art Design Club",
            "Science Club",
            "Drama Club",
            "Photography Club",
            "Debate Club",
            "Chess Club",
            "Robotics Club"
        );
    }

    public class ReportNotificationData
    {
        public int ReportId { get; set; }
        public string ClubName { get; set; } = "";
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
    }
}
