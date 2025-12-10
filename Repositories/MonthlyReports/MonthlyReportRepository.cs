using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.MonthlyReports
{
    public class MonthlyReportRepository : IMonthlyReportRepository
    {
        private readonly EduXtendContext _ctx;

        public MonthlyReportRepository(EduXtendContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<List<Plan>> GetAllByClubIdAsync(int clubId)
        {
            return await _ctx.Plans
                .AsNoTracking()
                .Where(p => p.ClubId == clubId && p.ReportType == "Monthly")
                .Include(p => p.Club)
                .Include(p => p.ApprovedBy)
                .OrderByDescending(p => p.ReportYear)
                .ThenByDescending(p => p.ReportMonth)
                .ToListAsync();
        }

        public async Task<Plan?> GetByIdAsync(int id)
        {
            return await _ctx.Plans
                .AsNoTracking()
                .Include(p => p.Club)
                .Include(p => p.ApprovedBy)
                .FirstOrDefaultAsync(p => p.Id == id && p.ReportType == "Monthly");
        }

        public async Task<Plan> CreateAsync(Plan plan)
        {
            _ctx.Plans.Add(plan);
            await _ctx.SaveChangesAsync();
            return plan;
        }

        public async Task<Plan> UpdateAsync(Plan plan)
        {
            var existing = await _ctx.Plans.FirstOrDefaultAsync(p => p.Id == plan.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Plan with ID {plan.Id} not found");
            }

            _ctx.Entry(existing).CurrentValues.SetValues(plan);
            await _ctx.SaveChangesAsync();
            return existing;
        }

        public async Task<Plan?> GetByClubAndMonthAsync(int clubId, int month, int year)
        {
            return await _ctx.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => 
                    p.ClubId == clubId && 
                    p.ReportType == "Monthly" &&
                    p.ReportMonth == month &&
                    p.ReportYear == year);
        }
    }
}
