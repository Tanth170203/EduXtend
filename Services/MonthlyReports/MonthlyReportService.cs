using BusinessObject.DTOs.MonthlyReport;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Repositories.MonthlyReports;
using Services.Notifications;

namespace Services.MonthlyReports;

public class MonthlyReportService : IMonthlyReportService
{
    private readonly IMonthlyReportRepository _reportRepo;
    private readonly IMonthlyReportDataAggregator _dataAggregator;
    private readonly INotificationService _notificationService;
    private readonly EduXtendContext _context;

    public MonthlyReportService(
        IMonthlyReportRepository reportRepo,
        IMonthlyReportDataAggregator dataAggregator,
        INotificationService notificationService,
        EduXtendContext context)
    {
        _reportRepo = reportRepo;
        _dataAggregator = dataAggregator;
        _notificationService = notificationService;
        _context = context;
    }

    public async Task<List<MonthlyReportListDto>> GetAllReportsAsync(int clubId)
    {
        var reports = await _reportRepo.GetAllByClubIdAsync(clubId);
        
        return reports.Select(r => new MonthlyReportListDto
        {
            Id = r.Id,
            ClubId = r.ClubId,
            ClubName = r.Club?.Name ?? "",
            Status = r.Status,
            ReportMonth = r.ReportMonth ?? 0,
            ReportYear = r.ReportYear ?? 0,
            CreatedAt = r.CreatedAt,
            SubmittedAt = r.SubmittedAt
        }).ToList();
    }

    public async Task<MonthlyReportDto> GetReportByIdAsync(int reportId)
    {
        var plan = await _reportRepo.GetByIdAsync(reportId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Monthly report with ID {reportId} not found");
        }

        return await BuildMonthlyReportDto(plan, includeAggregatedData: false);
    }

    public async Task<MonthlyReportDto> GetReportWithFreshDataAsync(int reportId)
    {
        var plan = await _reportRepo.GetByIdAsync(reportId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Monthly report with ID {reportId} not found");
        }

        return await BuildMonthlyReportDto(plan, includeAggregatedData: true);
    }

    public async Task<int> CreateMonthlyReportAsync(int clubId, int month, int year)
    {
        // Validate month range (Requirements: 19.1, 19.2, 19.3)
        var validationError = ValidateMonthSequence(month, year);
        if (!string.IsNullOrEmpty(validationError))
        {
            throw new InvalidOperationException(validationError);
        }

        // Validate: Check for duplicate
        var existing = await _reportRepo.GetByClubAndMonthAsync(clubId, month, year);
        if (existing != null)
        {
            throw new InvalidOperationException($"Monthly report for {month}/{year} already exists for this club");
        }

        // Get club info
        var club = await _context.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clubId);
        
        if (club == null)
        {
            throw new InvalidOperationException($"Club with ID {clubId} not found");
        }

        // Calculate next month for description
        int nextMonth = month == 12 ? 1 : month + 1;

        // Create new Plan with Monthly report type
        var plan = new Plan
        {
            ClubId = clubId,
            Title = $"Báo cáo tháng {month}/{year}",
            Description = $"Báo cáo hoạt động tháng {month} và kế hoạch tháng {nextMonth}",
            Status = "Draft",
            ReportType = "Monthly",
            ReportMonth = month,
            ReportYear = year,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _reportRepo.CreateAsync(plan);
        return created.Id;
    }

    public async Task UpdateReportAsync(int reportId, UpdateMonthlyReportDto dto)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == reportId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Monthly report with ID {reportId} not found");
        }

        // Validate: Only allow updates when status is Draft or Rejected
        if (plan.Status != "Draft" && plan.Status != "Rejected")
        {
            throw new InvalidOperationException($"Cannot update report with status {plan.Status}. Only Draft or Rejected reports can be updated.");
        }

        // Validate month logic: NextMonth must equal ReportMonth + 1
        // Requirements: 19.1, 19.2, 19.3
        if (plan.ReportMonth.HasValue && plan.ReportYear.HasValue)
        {
            var validationError = ValidateMonthSequence(plan.ReportMonth.Value, plan.ReportYear.Value);
            if (!string.IsNullOrEmpty(validationError))
            {
                throw new InvalidOperationException(validationError);
            }
        }

        // Update editable sections
        if (dto.EventMediaUrls != null)
        {
            plan.EventMediaUrls = dto.EventMediaUrls;
        }

        if (dto.NextMonthPurposeAndSignificance != null)
        {
            plan.NextMonthPurposeAndSignificance = dto.NextMonthPurposeAndSignificance;
        }

        if (dto.ClubResponsibilities != null)
        {
            plan.ClubResponsibilities = dto.ClubResponsibilities;
        }

        await _reportRepo.UpdateAsync(plan);
    }

    public async Task SubmitReportAsync(int reportId, int userId)
    {
        var plan = await _context.Plans
            .Include(p => p.Club)
            .FirstOrDefaultAsync(p => p.Id == reportId);
        
        if (plan == null)
        {
            throw new InvalidOperationException($"Monthly report with ID {reportId} not found");
        }

        // Validate: Only Draft or Rejected reports can be submitted
        if (plan.Status != "Draft" && plan.Status != "Rejected")
        {
            throw new InvalidOperationException($"Cannot submit report with status {plan.Status}");
        }

        // Update status
        plan.Status = "PendingApproval";
        plan.SubmittedAt = DateTime.UtcNow;

        await _reportRepo.UpdateAsync(plan);

        // Create notification for all Admins
        var admins = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "Admin" && u.IsActive)
            .ToListAsync();

        foreach (var admin in admins)
        {
            var message = $"Báo cáo tháng {plan.ReportMonth}/{plan.ReportYear} từ CLB {plan.Club?.Name} đã được nộp và đang chờ phê duyệt.";
            await _notificationService.SendNotificationAsync(
                admin.Id,
                "MonthlyReportSubmitted",
                message,
                reportId
            );
        }
    }

    public async Task<List<MonthlyReportListDto>> GetAllReportsForAdminAsync()
    {
        var reports = await _context.Plans
            .AsNoTracking()
            .Include(p => p.Club)
            .Where(p => p.ReportType == "Monthly")
            .OrderByDescending(p => p.ReportYear)
            .ThenByDescending(p => p.ReportMonth)
            .ToListAsync();
        
        return reports.Select(r => new MonthlyReportListDto
        {
            Id = r.Id,
            ClubId = r.ClubId,
            ClubName = r.Club?.Name ?? "",
            Status = r.Status,
            ReportMonth = r.ReportMonth ?? 0,
            ReportYear = r.ReportYear ?? 0,
            CreatedAt = r.CreatedAt,
            SubmittedAt = r.SubmittedAt
        }).ToList();
    }

    // Private helper methods

    /// <summary>
    /// Validates that the next month is exactly one month after the report month
    /// Requirements: 19.1, 19.2, 19.3
    /// </summary>
    private string? ValidateMonthSequence(int reportMonth, int reportYear)
    {
        // Calculate expected next month
        int expectedNextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
        int expectedNextYear = reportMonth == 12 ? reportYear + 1 : reportYear;

        // The validation is implicit in the BuildMonthlyReportDto method
        // which automatically calculates the correct next month
        // This method exists to provide explicit validation if needed in the future
        
        // For now, we validate that the month is within valid range
        if (reportMonth < 1 || reportMonth > 12)
        {
            return $"Tháng báo cáo không hợp lệ: {reportMonth}. Tháng phải từ 1 đến 12.";
        }

        return null; // Valid
    }

    private async Task<MonthlyReportDto> BuildMonthlyReportDto(Plan plan, bool includeAggregatedData)
    {
        if (plan.ReportMonth == null || plan.ReportYear == null)
        {
            throw new InvalidOperationException("Invalid monthly report: missing month or year");
        }

        int reportMonth = plan.ReportMonth.Value;
        int reportYear = plan.ReportYear.Value;

        // Calculate next month
        int nextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
        int nextYear = reportMonth == 12 ? reportYear + 1 : reportYear;

        // Get club and creator info
        var club = await _context.Clubs
            .AsNoTracking()
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == plan.ClubId);

        // Get club manager (creator)
        var clubManager = await _context.ClubMembers
            .AsNoTracking()
            .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
            .Where(cm => cm.ClubId == plan.ClubId && cm.RoleInClub == "Manager")
            .Select(cm => cm.Student.User)
            .FirstOrDefaultAsync();

        var dto = new MonthlyReportDto
        {
            Id = plan.Id,
            ClubId = plan.ClubId,
            ClubName = club?.Name ?? "",
            DepartmentName = club?.Category?.Name ?? "",
            Status = plan.Status,
            ReportMonth = reportMonth,
            ReportYear = reportYear,
            NextMonth = nextMonth,
            NextYear = nextYear,
            CreatedAt = plan.CreatedAt,
            SubmittedAt = plan.SubmittedAt,
            ApprovedAt = plan.ApprovedAt,
            RejectionReason = plan.RejectionReason
        };

        // Build Header
        dto.Header = new HeaderDto
        {
            DepartmentName = club?.Category?.Name ?? "",
            MainTitle = $"BÁO CÁO HOẠT ĐỘNG THÁNG {reportMonth}",
            SubTitle = $"VÀ KẾ HOẠCH THÁNG {nextMonth}",
            ClubName = club?.Name ?? "",
            Location = "FPT University HCM", // Default location
            ReportDate = DateTime.Now,
            CreatorName = clubManager?.FullName ?? "",
            CreatorPosition = "Quản lý CLB"
        };

        // Build Footer
        dto.Footer = new FooterDto
        {
            CreatorName = clubManager?.FullName ?? "",
            CreatorPosition = "Quản lý CLB"
        };

        if (plan.ApprovedBy != null)
        {
            var approver = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == plan.ApprovedById);
            
            if (approver != null)
            {
                dto.Footer.ApproverName = approver.FullName;
                dto.Footer.ApproverPosition = approver.Role?.RoleName ?? "Admin";
            }
        }

        // If includeAggregatedData is true, fetch fresh data from DataAggregator
        if (includeAggregatedData)
        {
            // Part A: Current Month Activities
            dto.CurrentMonthActivities = new CurrentMonthActivitiesDto
            {
                SchoolEvents = await _dataAggregator.GetSchoolEventsAsync(plan.ClubId, reportMonth, reportYear),
                SupportActivities = await _dataAggregator.GetSupportActivitiesAsync(plan.ClubId, reportMonth, reportYear),
                Competitions = await _dataAggregator.GetCompetitionsAsync(plan.ClubId, reportMonth, reportYear),
                InternalMeetings = await _dataAggregator.GetInternalMeetingsAsync(plan.ClubId, reportMonth, reportYear)
            };

            // Part B: Next Month Plans
            // Note: Pass reportMonth/reportYear to find the Plan record, not nextMonth
            // The Plan record stores editable sections (Purpose, Responsibilities)
            // The aggregator will query activities for nextMonth
            dto.NextMonthPlans = await _dataAggregator.GetNextMonthPlansAsync(plan.ClubId, reportMonth, reportYear, nextMonth, nextYear);
        }
        else
        {
            // Return empty structures
            dto.CurrentMonthActivities = new CurrentMonthActivitiesDto();
            dto.NextMonthPlans = new NextMonthPlansDto();
        }

        return dto;
    }
}
