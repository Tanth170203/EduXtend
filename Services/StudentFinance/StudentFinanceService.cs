using BusinessObject.DTOs.Common;
using BusinessObject.DTOs.StudentFinance;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Services.StudentFinance;

public class StudentFinanceService : IStudentFinanceService
{
    private readonly EduXtendContext _context;

    public StudentFinanceService(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<StudentPendingPaymentDto>> GetPendingPaymentsAsync(int userId, int page, int pageSize, int? clubId)
    {
        // Get all club memberships for the user
        var query = _context.FundCollectionPayments
            .Include(p => p.FundCollectionRequest)
                .ThenInclude(r => r.Club)
            .Include(p => p.FundCollectionRequest)
                .ThenInclude(r => r.CreatedBy)
            .Include(p => p.ClubMember)
                .ThenInclude(cm => cm.Student)
            .Where(p => p.ClubMember.Student.UserId == userId)
            .Where(p => p.Status == "pending" || p.Status == "unconfirmed")
            .Where(p => p.FundCollectionRequest.Status == "active");

        // Apply club filter if provided
        if (clubId.HasValue)
        {
            query = query.Where(p => p.FundCollectionRequest.ClubId == clubId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated data
        var payments = await query
            .OrderBy(p => p.FundCollectionRequest.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTOs
        var now = DateTime.UtcNow;
        var items = payments.Select(p => new StudentPendingPaymentDto
        {
            Id = p.Id,
            ClubId = p.FundCollectionRequest.ClubId,
            ClubName = p.FundCollectionRequest.Club.Name,
            ClubLogoUrl = p.FundCollectionRequest.Club.LogoUrl,
            PaymentTitle = p.FundCollectionRequest.Title,
            Description = p.FundCollectionRequest.Description,
            Amount = p.Amount,
            DueDate = p.FundCollectionRequest.DueDate,
            DaysUntilDue = (int)(p.FundCollectionRequest.DueDate - now).TotalDays,
            Status = p.Status,
            CreatedByName = p.FundCollectionRequest.CreatedBy.FullName,
            CreatedAt = p.FundCollectionRequest.CreatedAt,
            IsOverdue = p.FundCollectionRequest.DueDate < now,
            IsDueSoon = p.FundCollectionRequest.DueDate >= now && 
                        p.FundCollectionRequest.DueDate <= now.AddDays(3)
        }).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResult<StudentPendingPaymentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public async Task<PaginatedResult<StudentPaymentHistoryDto>> GetPaymentHistoryAsync(int userId, int page, int pageSize, int? clubId, DateTime? startDate, DateTime? endDate)
    {
        // Get all paid payments for the user
        var query = _context.FundCollectionPayments
            .Include(p => p.FundCollectionRequest)
                .ThenInclude(r => r.Club)
            .Include(p => p.ClubMember)
                .ThenInclude(cm => cm.Student)
            .Include(p => p.ConfirmedBy)
            .Where(p => p.ClubMember.Student.UserId == userId)
            .Where(p => p.Status == "paid")
            .Where(p => p.PaidAt.HasValue);

        // Apply club filter if provided
        if (clubId.HasValue)
        {
            query = query.Where(p => p.FundCollectionRequest.ClubId == clubId.Value);
        }

        // Apply date range filter if provided
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaidAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaidAt <= endDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated data ordered by payment date descending
        var payments = await query
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTOs
        var items = payments.Select(p => new StudentPaymentHistoryDto
        {
            Id = p.Id,
            ClubId = p.FundCollectionRequest.ClubId,
            ClubName = p.FundCollectionRequest.Club.Name,
            ClubLogoUrl = p.FundCollectionRequest.Club.LogoUrl,
            PaymentTitle = p.FundCollectionRequest.Title,
            Description = p.FundCollectionRequest.Description,
            Amount = p.Amount,
            PaidAt = p.PaidAt!.Value,
            PaymentMethod = p.PaymentMethod ?? "N/A",
            Status = p.Status,
            ConfirmedByName = p.ConfirmedBy?.FullName,
            PaymentTransactionId = p.PaymentTransactionId,
            Notes = p.Notes
        }).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedResult<StudentPaymentHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };
    }

    public async Task<StudentFinanceStatisticsDto> GetFinanceStatisticsAsync(int userId)
    {
        // Get active semester
        var activeSemester = await _context.Semesters
            .FirstOrDefaultAsync(s => s.IsActive);

        // Get all payments for the user
        var allPayments = await _context.FundCollectionPayments
            .Include(p => p.FundCollectionRequest)
            .Include(p => p.ClubMember)
                .ThenInclude(cm => cm.Student)
            .Where(p => p.ClubMember.Student.UserId == userId)
            .ToListAsync();

        // Calculate pending statistics
        var pendingPayments = allPayments
            .Where(p => (p.Status == "pending" || p.Status == "unconfirmed") && 
                       p.FundCollectionRequest.Status == "active")
            .ToList();

        var totalPendingAmount = pendingPayments.Sum(p => p.Amount);
        var totalPendingCount = pendingPayments.Count;
        var overdueCount = pendingPayments.Count(p => p.FundCollectionRequest.DueDate < DateTime.UtcNow);
        var clubsWithPending = pendingPayments
            .Select(p => p.FundCollectionRequest.ClubId)
            .Distinct()
            .Count();

        // Calculate paid statistics for current semester
        var paidPayments = allPayments
            .Where(p => p.Status == "paid" && p.PaidAt.HasValue)
            .ToList();

        var paidThisSemester = activeSemester != null
            ? paidPayments.Where(p => p.FundCollectionRequest.SemesterId == activeSemester.Id).ToList()
            : new List<FundCollectionPayment>();

        var totalPaidThisSemester = paidThisSemester.Sum(p => p.Amount);
        var totalPaidCount = paidPayments.Count;

        return new StudentFinanceStatisticsDto
        {
            TotalPendingAmount = totalPendingAmount,
            TotalPendingCount = totalPendingCount,
            OverdueCount = overdueCount,
            ClubsWithPendingPayments = clubsWithPending,
            TotalPaidThisSemester = totalPaidThisSemester,
            TotalPaidCount = totalPaidCount
        };
    }

    public async Task<byte[]> ExportPaymentHistoryToCsvAsync(int userId, int? clubId, DateTime? startDate, DateTime? endDate)
    {
        // Get all payment history (no pagination for export)
        var query = _context.FundCollectionPayments
            .Include(p => p.FundCollectionRequest)
                .ThenInclude(r => r.Club)
            .Include(p => p.ClubMember)
                .ThenInclude(cm => cm.Student)
            .Include(p => p.ConfirmedBy)
            .Where(p => p.ClubMember.Student.UserId == userId)
            .Where(p => p.Status == "paid")
            .Where(p => p.PaidAt.HasValue);

        // Apply filters
        if (clubId.HasValue)
        {
            query = query.Where(p => p.FundCollectionRequest.ClubId == clubId.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaidAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaidAt <= endDate.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        // Generate CSV
        var csv = new StringBuilder();
        csv.AppendLine("Club Name,Payment Title,Amount,Payment Date,Payment Method,Confirmed By,Transaction ID,Notes");

        foreach (var payment in payments)
        {
            csv.AppendLine($"\"{payment.FundCollectionRequest.Club.Name}\"," +
                          $"\"{payment.FundCollectionRequest.Title}\"," +
                          $"{payment.Amount}," +
                          $"{payment.PaidAt:yyyy-MM-dd}," +
                          $"\"{payment.PaymentMethod ?? "N/A"}\"," +
                          $"\"{payment.ConfirmedBy?.FullName ?? "N/A"}\"," +
                          $"{payment.PaymentTransactionId?.ToString() ?? "N/A"}," +
                          $"\"{payment.Notes?.Replace("\"", "\"\"") ?? ""}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
