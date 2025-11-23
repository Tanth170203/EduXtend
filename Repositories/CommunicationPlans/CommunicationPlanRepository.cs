using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.CommunicationPlans
{
    public class CommunicationPlanRepository : ICommunicationPlanRepository
    {
        private readonly EduXtendContext _ctx;
        
        public CommunicationPlanRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<CommunicationPlan?> GetByIdAsync(int id)
            => await _ctx.CommunicationPlans
                .AsNoTracking()
                .Include(cp => cp.Activity)
                .Include(cp => cp.Club)
                .Include(cp => cp.CreatedBy)
                .Include(cp => cp.Items.OrderBy(i => i.Order))
                .FirstOrDefaultAsync(cp => cp.Id == id);

        public async Task<CommunicationPlan?> GetByActivityIdAsync(int activityId)
            => await _ctx.CommunicationPlans
                .AsNoTracking()
                .Include(cp => cp.Activity)
                .Include(cp => cp.Club)
                .Include(cp => cp.CreatedBy)
                .Include(cp => cp.Items.OrderBy(i => i.Order))
                .FirstOrDefaultAsync(cp => cp.ActivityId == activityId);

        public async Task<List<CommunicationPlan>> GetByClubIdAsync(int clubId)
            => await _ctx.CommunicationPlans
                .AsNoTracking()
                .Where(cp => cp.ClubId == clubId)
                .Include(cp => cp.Activity)
                .Include(cp => cp.Club)
                .Include(cp => cp.CreatedBy)
                .Include(cp => cp.Items.OrderBy(i => i.Order))
                .OrderByDescending(cp => cp.CreatedAt)
                .ToListAsync();

        public async Task<List<CommunicationPlan>> GetByClubAndMonthAsync(int clubId, int month, int year)
            => await _ctx.CommunicationPlans
                .AsNoTracking()
                .Where(cp => cp.ClubId == clubId 
                    && cp.Activity != null
                    && cp.Activity.StartTime.Month == month 
                    && cp.Activity.StartTime.Year == year)
                .Include(cp => cp.Activity)
                .Include(cp => cp.Club)
                .Include(cp => cp.CreatedBy)
                .Include(cp => cp.Items.OrderBy(i => i.Order))
                .OrderBy(cp => cp.Activity.StartTime)
                .ToListAsync();

        public async Task<CommunicationPlan> CreateAsync(CommunicationPlan plan)
        {
            _ctx.CommunicationPlans.Add(plan);
            await _ctx.SaveChangesAsync();
            return plan;
        }

        public async Task<CommunicationPlan> UpdateAsync(CommunicationPlan plan)
        {
            var existing = await _ctx.CommunicationPlans
                .Include(cp => cp.Items)
                .FirstOrDefaultAsync(cp => cp.Id == plan.Id);
            
            if (existing == null)
                throw new InvalidOperationException("Communication plan not found");

            // Remove old items
            _ctx.CommunicationItems.RemoveRange(existing.Items);

            // Update plan properties
            existing.ActivityId = plan.ActivityId;
            existing.ClubId = plan.ClubId;
            
            // Add new items
            existing.Items = plan.Items;

            await _ctx.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.CommunicationPlans.FirstOrDefaultAsync(cp => cp.Id == id);
            if (existing == null) return false;

            _ctx.CommunicationPlans.Remove(existing);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<List<int>> GetActivityIdsWithPlansAsync(int clubId)
        {
            return await _ctx.CommunicationPlans
                .Where(cp => cp.ClubId == clubId)
                .Select(cp => cp.ActivityId)
                .Distinct()
                .ToListAsync();
        }
    }
}
