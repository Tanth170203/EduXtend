using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Moq;
using Repositories.MonthlyReports;
using Services.MonthlyReports;
using Services.Notifications;
using Xunit;

namespace Services.Tests.MonthlyReports;

/// <summary>
/// Unit tests for MonthlyReportApprovalService rejection functionality
/// Feature: monthly-report-submission-notifications
/// </summary>
public class MonthlyReportApprovalUnitTests : IDisposable
{
    private readonly EduXtendContext _context;
    private readonly MonthlyReportApprovalService _service;

    public MonthlyReportApprovalUnitTests()
    {
        var options = new DbContextOptionsBuilder<EduXtendContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduXtendContext(options);

        // Seed admin role
        _context.Roles.Add(new Role { Id = 1, RoleName = "Admin" });
        _context.SaveChanges();

        // Create service
        var reportRepo = new MonthlyReportRepository(_context);
        var notificationRepo = new Repositories.Notifications.NotificationRepository(_context);
        var notificationService = new NotificationService(notificationRepo, _context);

        _service = new MonthlyReportApprovalService(
            reportRepo,
            notificationService,
            _context
        );
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task RejectReportAsync_CreatesNotificationForClubManager()
    {
        // Arrange
        var clubId = 1;
        var reportId = 1;
        var managerId = 100;
        var adminId = 1;
        var rejectionReason = "Missing required information";

        // Add club category
        var category = new ClubCategory
        {
            Id = 1,
            Name = "Test Category"
        };
        _context.ClubCategories.Add(category);

        // Add club
        var club = new Club
        {
            Id = clubId,
            Name = "Test Club",
            SubName = "Test Club",
            CategoryId = 1,
            IsActive = true
        };
        _context.Clubs.Add(club);

        // Add club manager user
        var managerUser = new User
        {
            Id = managerId,
            Email = "manager@test.com",
            FullName = "Test Manager",
            RoleId = 2,
            IsActive = true
        };
        _context.Users.Add(managerUser);

        // Add major
        var major = new Major
        {
            Id = 1,
            Code = "SE",
            Name = "Software Engineering"
        };
        _context.Majors.Add(major);

        // Add student
        var student = new Student
        {
            Id = managerId,
            UserId = managerId,
            StudentCode = $"STU{managerId}",
            FullName = "Test Manager",
            Cohort = "K17",
            Email = "manager@test.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-20),
            Gender = BusinessObject.Enum.Gender.Male,
            EnrollmentDate = DateTime.UtcNow.AddYears(-2),
            Status = BusinessObject.Enum.StudentStatus.Active,
            MajorId = 1
        };
        _context.Students.Add(student);

        // Add club member (manager)
        var clubMember = new ClubMember
        {
            Id = managerId,
            ClubId = clubId,
            StudentId = managerId,
            RoleInClub = "Manager",
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        _context.ClubMembers.Add(clubMember);

        // Add report
        var plan = new Plan
        {
            Id = reportId,
            ClubId = clubId,
            Title = "Report 11/2024",
            Status = "PendingApproval",
            ReportType = "Monthly",
            ReportMonth = 11,
            ReportYear = 2024,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        var initialNotificationCount = _context.Notifications.Count();

        // Act
        await _service.RejectReportAsync(reportId, adminId, rejectionReason);

        // Assert
        var notifications = _context.Notifications
            .Where(n => n.TargetUserId == managerId)
            .ToList();

        Assert.True(notifications.Count > initialNotificationCount);
        
        var rejectionNotification = notifications.Last();
        Assert.Contains(rejectionReason, rejectionNotification.Message);
    }

    [Fact]
    public async Task ApproveReportAsync_CreatesNotificationForClubManager()
    {
        // Arrange
        var clubId = 1;
        var reportId = 1;
        var managerId = 100;
        var adminId = 1;

        // Add club category
        var category = new ClubCategory
        {
            Id = 1,
            Name = "Test Category"
        };
        _context.ClubCategories.Add(category);

        // Add club
        var club = new Club
        {
            Id = clubId,
            Name = "Test Club",
            SubName = "Test Club",
            CategoryId = 1,
            IsActive = true
        };
        _context.Clubs.Add(club);

        // Add club manager user
        var managerUser = new User
        {
            Id = managerId,
            Email = "manager@test.com",
            FullName = "Test Manager",
            RoleId = 2,
            IsActive = true
        };
        _context.Users.Add(managerUser);

        // Add major
        var major = new Major
        {
            Id = 1,
            Code = "SE",
            Name = "Software Engineering"
        };
        _context.Majors.Add(major);

        // Add student
        var student = new Student
        {
            Id = managerId,
            UserId = managerId,
            StudentCode = $"STU{managerId}",
            FullName = "Test Manager",
            Cohort = "K17",
            Email = "manager@test.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-20),
            Gender = BusinessObject.Enum.Gender.Male,
            EnrollmentDate = DateTime.UtcNow.AddYears(-2),
            Status = BusinessObject.Enum.StudentStatus.Active,
            MajorId = 1
        };
        _context.Students.Add(student);

        // Add club member (manager)
        var clubMember = new ClubMember
        {
            Id = managerId,
            ClubId = clubId,
            StudentId = managerId,
            RoleInClub = "Manager",
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };
        _context.ClubMembers.Add(clubMember);

        // Add report
        var plan = new Plan
        {
            Id = reportId,
            ClubId = clubId,
            Title = "Report 11/2024",
            Status = "PendingApproval",
            ReportType = "Monthly",
            ReportMonth = 11,
            ReportYear = 2024,
            CreatedAt = DateTime.UtcNow
        };
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        var initialNotificationCount = _context.Notifications.Count();

        // Act
        await _service.ApproveReportAsync(reportId, adminId);

        // Assert
        var notifications = _context.Notifications
            .Where(n => n.TargetUserId == managerId)
            .ToList();

        Assert.True(notifications.Count > initialNotificationCount);
    }
}
