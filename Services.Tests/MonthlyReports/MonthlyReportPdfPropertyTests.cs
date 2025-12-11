using BusinessObject.DTOs.MonthlyReport;
using BusinessObject.Models;
using DataAccess;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Repositories.MonthlyReports;
using Services.MonthlyReports;
using Xunit;

namespace Services.Tests.MonthlyReports;

/// <summary>
/// Property-based tests for monthly report PDF generation functionality
/// Feature: monthly-report-submission-notifications
/// </summary>
public class MonthlyReportPdfPropertyTests : IDisposable
{
    private EduXtendContext _context = null!;
    private Mock<IMonthlyReportRepository> _mockReportRepo = null!;
    private Mock<IMonthlyReportDataAggregator> _mockDataAggregator = null!;
    private MonthlyReportPdfService _pdfService = null!;

    public MonthlyReportPdfPropertyTests()
    {
        SetupTestContext();
    }

    private void SetupTestContext()
    {
        var options = new DbContextOptionsBuilder<EduXtendContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduXtendContext(options);
        _mockReportRepo = new Mock<IMonthlyReportRepository>();
        _mockDataAggregator = new Mock<IMonthlyReportDataAggregator>();
        _pdfService = new MonthlyReportPdfService(_mockReportRepo.Object, _mockDataAggregator.Object, _context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Feature: monthly-report-submission-notifications, Property 4: PDF generation triggered
    /// Validates: Requirements 2.1, 2.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PdfGenerationTriggeredForAnyReport()
    {
        return Prop.ForAll(
            GenerateReportId(),
            GenerateMonthlyReportDto(),
            (reportId, reportDto) =>
            {
                // Arrange
                SetupTestContext();
                
                // Create a Plan entity that matches the DTO
                var plan = new Plan
                {
                    Id = reportId,
                    ClubId = reportDto.ClubId,
                    Title = $"Report {reportDto.ReportMonth}/{reportDto.ReportYear}",
                    Status = reportDto.Status,
                    ReportType = "Monthly",
                    ReportMonth = reportDto.ReportMonth,
                    ReportYear = reportDto.ReportYear,
                    CreatedAt = reportDto.CreatedAt
                };

                // Setup mock repository to return the plan
                _mockReportRepo
                    .Setup(r => r.GetByIdAsync(reportId))
                    .ReturnsAsync(plan);

                // Setup mock data aggregator
                _mockDataAggregator
                    .Setup(d => d.GetSchoolEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(reportDto.CurrentMonthActivities.SchoolEvents);
                _mockDataAggregator
                    .Setup(d => d.GetSupportActivitiesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(reportDto.CurrentMonthActivities.SupportActivities);
                _mockDataAggregator
                    .Setup(d => d.GetCompetitionsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(reportDto.CurrentMonthActivities.Competitions);
                _mockDataAggregator
                    .Setup(d => d.GetInternalMeetingsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(reportDto.CurrentMonthActivities.InternalMeetings);
                _mockDataAggregator
                    .Setup(d => d.GetNextMonthPlansAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync(reportDto.NextMonthPlans);

                // Act
                byte[]? pdfBytes = null;
                Exception? exception = null;
                
                try
                {
                    pdfBytes = _pdfService.ExportToPdfAsync(reportId).Result;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                // Assert
                // 1. PDF generation should be triggered (mock was called)
                _mockReportRepo.Verify(
                    r => r.GetByIdAsync(reportId),
                    Times.Once,
                    "PDF service should call GetByIdAsync on repository"
                );

                // 2. PDF should be generated successfully (non-null, non-empty byte array)
                var pdfGenerated = pdfBytes != null && pdfBytes.Length > 0;

                // 3. No exceptions should be thrown
                var noException = exception == null;

                return pdfGenerated && noException;
            }
        );
    }

    // Generators
    private static Arbitrary<int> GenerateReportId()
    {
        return Arb.From(Gen.Choose(1, 10000));
    }

    private static Arbitrary<MonthlyReportDto> GenerateMonthlyReportDto()
    {
        return Arb.From(
            Gen.Choose(1, 12)
                .SelectMany(month =>
                    Gen.Choose(2020, 2025)
                        .SelectMany(year =>
                            Gen.Elements("Club A", "Club B", "Test Club", "Student Club", "Câu lạc bộ Sinh viên")
                                .Select(clubName => CreateValidMonthlyReportDto(month, year, clubName))
                        )
                )
        );
    }

    private static MonthlyReportDto CreateValidMonthlyReportDto(int month, int year, string clubName)
    {
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;

        return new MonthlyReportDto
        {
            Id = 1,
            ClubId = 1,
            ClubName = clubName,
            DepartmentName = "Test Department",
            Status = "PendingApproval",
            ReportMonth = month,
            ReportYear = year,
            NextMonth = nextMonth,
            NextYear = nextYear,
            Header = new HeaderDto
            {
                ClubName = clubName,
                Location = "TP.Đà Nẵng",
                ReportDate = new DateTime(year, month, 15)
            },
            CurrentMonthActivities = new CurrentMonthActivitiesDto
            {
                SchoolEvents = new List<SchoolEventDto>(),
                SupportActivities = new List<SupportActivityDto>(),
                Competitions = new List<CompetitionDto>(),
                InternalMeetings = new List<InternalMeetingDto>()
            },
            NextMonthPlans = new NextMonthPlansDto
            {
                Purpose = new PurposeDto
                {
                    Purpose = "Test purpose",
                    Significance = "Test significance"
                },
                PlannedEvents = new List<PlannedEventDto>(),
                PlannedCompetitions = new List<PlannedCompetitionDto>(),
                CommunicationPlan = new List<CommunicationItemDto>(),
                Budget = new BudgetDto
                {
                    SchoolFunding = new List<BudgetItemDto>(),
                    ClubFunding = new List<BudgetItemDto>(),
                    SchoolTotal = 0,
                    ClubTotal = 0,
                    SchoolTotalInWords = "Không đồng",
                    ClubTotalInWords = "Không đồng"
                },
                Facility = new FacilityDto
                {
                    Items = new List<FacilityItemDto>(),
                    ElectionTime = null
                },
                Responsibilities = new ClubResponsibilitiesDto
                {
                    Planning = true,
                    Implementation = true,
                    StaffAssignment = true,
                    SecurityOrder = true,
                    HygieneAssetMaintenance = true,
                    CustomText = ""
                }
            },
            Footer = new FooterDto
            {
                CreatorName = "Test Creator",
                CreatorPosition = "President",
                ReviewerName = "Test Reviewer",
                ApproverName = "Test Approver"
            },
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };
    }
}
