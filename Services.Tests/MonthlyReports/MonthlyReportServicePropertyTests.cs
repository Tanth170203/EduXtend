using BusinessObject.Models;
using DataAccess;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.MonthlyReports;
using Services.Emails;
using Services.MonthlyReports;
using Services.Notifications;
using Xunit;

namespace Services.Tests.MonthlyReports;

/// <summary>
/// Property-based tests for MonthlyReportService submission functionality
/// Feature: monthly-report-submission-notifications
/// </summary>
public class MonthlyReportServicePropertyTests : IDisposable
{
    private EduXtendContext _context = null!;
    private MonthlyReportService _service = null!;
    private Mock<IMonthlyReportPdfService> _mockPdfService = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<MonthlyReportService>> _mockLogger = null!;
    private Mock<INotificationService> _mockNotificationService = null!;

    public MonthlyReportServicePropertyTests()
    {
        SetupTestContext();
    }

    private void SetupTestContext()
    {
        var options = new DbContextOptionsBuilder<EduXtendContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduXtendContext(options);
        
        // Setup mocks
        _mockPdfService = new Mock<IMonthlyReportPdfService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<MonthlyReportService>>();
        _mockNotificationService = new Mock<INotificationService>();

        // Setup mock returns
        _mockPdfService.Setup(x => x.ExportToPdfAsync(It.IsAny<int>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });
        
        _mockEmailService.Setup(x => x.SendMonthlyReportSubmissionEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);

        _mockNotificationService.Setup(x => x.SendNotificationAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(new Notification());

        // Seed admin role
        _context.Roles.Add(new Role { Id = 1, RoleName = "Admin" });
        _context.SaveChanges();

        // Create service
        var reportRepo = new MonthlyReportRepository(_context);
        var mockDataAggregator = new Mock<IMonthlyReportDataAggregator>();

        _service = new MonthlyReportService(
            reportRepo,
            mockDataAggregator.Object,
            _mockNotificationService.Object,
            _mockEmailService.Object,
            _mockPdfService.Object,
            _mockLogger.Object,
            _context
        );
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Feature: monthly-report-email-notification, Property 1: Email sent to all active Admins only
    /// Validates: Requirements 1.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EmailSentToAllActiveAdminsOnly()
    {
        return Prop.ForAll(
            GenerateAdminUsers(),
            GenerateSubmissionScenario(),
            (adminUsers, scenario) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test
                
                // Add admin users to database (mix of active and inactive)
                foreach (var admin in adminUsers)
                {
                    admin.RoleId = 1; // Admin role
                    _context.Users.Add(admin);
                }

                // Add club category first
                var category = new ClubCategory
                {
                    Id = 1,
                    Name = "Test Category"
                };
                _context.ClubCategories.Add(category);

                // Add club
                var club = new Club
                {
                    Id = scenario.ClubId,
                    Name = scenario.ClubName,
                    SubName = scenario.ClubName,
                    CategoryId = 1,
                    IsActive = true
                };
                _context.Clubs.Add(club);

                // Add report
                var plan = new Plan
                {
                    Id = scenario.ReportId,
                    ClubId = scenario.ClubId,
                    Title = $"Report {scenario.ReportMonth}/{scenario.ReportYear}",
                    Status = "Draft",
                    ReportType = "Monthly",
                    ReportMonth = scenario.ReportMonth,
                    ReportYear = scenario.ReportYear,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Plans.Add(plan);
                _context.SaveChanges();

                // Act
                _service.SubmitReportAsync(scenario.ReportId, scenario.UserId).Wait();

                // Assert - Verify email service was called only for active admins
                var activeAdmins = adminUsers.Where(a => a.IsActive && !string.IsNullOrEmpty(a.Email)).ToList();
                
                _mockEmailService.Verify(
                    x => x.SendMonthlyReportSubmissionEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<int>(),
                        It.IsAny<byte[]>()),
                    Times.Exactly(activeAdmins.Count)
                );

                return true;
            }
        );
    }

    /// <summary>
    /// Feature: monthly-report-email-notification, Property 4: Email failure does not block submission
    /// Validates: Requirements 2.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EmailFailureDoesNotBlockSubmission()
    {
        return Prop.ForAll(
            GenerateSubmissionScenario(),
            (scenario) =>
            {
                // Arrange
                SetupTestContext(); // Fresh context for each test
                
                // Setup email to fail
                _mockEmailService.Setup(x => x.SendMonthlyReportSubmissionEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<byte[]>()))
                    .ThrowsAsync(new Exception("Email sending failed"));

                // Add admin user
                var admin = new User
                {
                    Id = 100,
                    Email = "admin@test.com",
                    FullName = "Test Admin",
                    RoleId = 1,
                    IsActive = true
                };
                _context.Users.Add(admin);

                // Add club category first
                var category = new ClubCategory
                {
                    Id = 1,
                    Name = "Test Category"
                };
                _context.ClubCategories.Add(category);

                // Add club
                var club = new Club
                {
                    Id = scenario.ClubId,
                    Name = scenario.ClubName,
                    SubName = scenario.ClubName,
                    CategoryId = 1,
                    IsActive = true
                };
                _context.Clubs.Add(club);

                // Add report
                var plan = new Plan
                {
                    Id = scenario.ReportId,
                    ClubId = scenario.ClubId,
                    Title = $"Report {scenario.ReportMonth}/{scenario.ReportYear}",
                    Status = "Draft",
                    ReportType = "Monthly",
                    ReportMonth = scenario.ReportMonth,
                    ReportYear = scenario.ReportYear,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Plans.Add(plan);
                _context.SaveChanges();

                // Act - Should not throw exception even if email fails
                Exception? caughtException = null;
                try
                {
                    _service.SubmitReportAsync(scenario.ReportId, scenario.UserId).Wait();
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                // Assert - Verify submission succeeded (no exception thrown)
                if (caughtException != null)
                {
                    return false;
                }

                // Verify report status was updated to PendingApproval
                var updatedPlan = _context.Plans.Find(scenario.ReportId);
                return updatedPlan != null && updatedPlan.Status == "PendingApproval";
            }
        );
    }

    // Generators
    private static Arbitrary<List<User>> GenerateAdminUsers()
    {
        return Arb.From(
            Gen.Choose(1, 5)
                .SelectMany(count =>
                    Gen.ListOf(count, GenerateUser())
                        .Select(users => users.Select((u, i) =>
                        {
                            u.Id = i + 100; // Start from 100 to avoid conflicts
                            u.Email = $"admin{i + 1}@test.com";
                            u.FullName = $"Admin {i + 1}";
                            // Randomly set some as inactive
                            u.IsActive = i % 2 == 0;
                            return u;
                        }).ToList())
                )
        );
    }

    private static Gen<User> GenerateUser()
    {
        return Gen.Fresh(() => new User
        {
            Id = 0, // Will be set by generator
            Email = "test@test.com",
            FullName = "Test User",
            RoleId = 1,
            IsActive = true
        });
    }

    private static Arbitrary<SubmissionScenario> GenerateSubmissionScenario()
    {
        return Arb.From(
            Gen.Choose(1, 1000)
                .SelectMany(reportId =>
                    Gen.Choose(1, 100)
                        .SelectMany(clubId =>
                            Gen.Elements("Club A", "Club B", "Test Club", "Student Club")
                                .SelectMany(clubName =>
                                    Gen.Choose(1, 12)
                                        .SelectMany(month =>
                                            Gen.Choose(2020, 2025)
                                                .SelectMany(year =>
                                                    Gen.Choose(1, 50)
                                                        .Select(userId => new SubmissionScenario
                                                        {
                                                            ReportId = reportId,
                                                            ClubId = clubId,
                                                            ClubName = clubName,
                                                            ReportMonth = month,
                                                            ReportYear = year,
                                                            UserId = userId,
                                                            SubmittedAt = DateTime.UtcNow
                                                        })
                                                )
                                        )
                                )
                        )
                )
        );
    }

    public class SubmissionScenario
    {
        public int ReportId { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = "";
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public int UserId { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
