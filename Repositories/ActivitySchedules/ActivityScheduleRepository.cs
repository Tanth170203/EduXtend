using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ActivitySchedules
{
    public class ActivityScheduleRepository : IActivityScheduleRepository
    {
        private readonly EduXtendContext _ctx;
        
        public ActivityScheduleRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<ActivitySchedule?> GetByIdAsync(int id)
            => await _ctx.ActivitySchedules
                .AsNoTracking()
                .Include(s => s.Assignments)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<List<ActivitySchedule>> GetByActivityIdAsync(int activityId)
            => await _ctx.ActivitySchedules
                .AsNoTracking()
                .Where(s => s.ActivityId == activityId)
                .Include(s => s.Assignments)
                    .ThenInclude(a => a.User)
                .OrderBy(s => s.Order)
                .ToListAsync();

        public async Task<ActivitySchedule> AddAsync(ActivitySchedule schedule)
        {
            _ctx.ActivitySchedules.Add(schedule);
            await _ctx.SaveChangesAsync();
            return schedule;
        }

        public async Task UpdateAsync(ActivitySchedule schedule)
        {
            // Detach any existing tracked entity with the same key
            var existingEntity = _ctx.ChangeTracker.Entries<ActivitySchedule>()
                .FirstOrDefault(e => e.Entity.Id == schedule.Id);
            
            if (existingEntity != null)
            {
                existingEntity.State = EntityState.Detached;
            }
            
            _ctx.ActivitySchedules.Update(schedule);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var schedule = await _ctx.ActivitySchedules.FindAsync(id);
            if (schedule != null)
            {
                _ctx.ActivitySchedules.Remove(schedule);
                await _ctx.SaveChangesAsync();
            }
        }
    }
}
