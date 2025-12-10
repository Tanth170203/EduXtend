using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ActivityScheduleAssignments
{
    public class ActivityScheduleAssignmentRepository : IActivityScheduleAssignmentRepository
    {
        private readonly EduXtendContext _ctx;
        
        public ActivityScheduleAssignmentRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<ActivityScheduleAssignment?> GetByIdAsync(int id)
            => await _ctx.ActivityScheduleAssignments
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<List<ActivityScheduleAssignment>> GetByScheduleIdAsync(int scheduleId)
            => await _ctx.ActivityScheduleAssignments
                .AsNoTracking()
                .Where(a => a.ActivityScheduleId == scheduleId)
                .Include(a => a.User)
                .ToListAsync();

        public async Task<ActivityScheduleAssignment> AddAsync(ActivityScheduleAssignment assignment)
        {
            _ctx.ActivityScheduleAssignments.Add(assignment);
            await _ctx.SaveChangesAsync();
            return assignment;
        }

        public async Task UpdateAsync(ActivityScheduleAssignment assignment)
        {
            // Detach any existing tracked entity with the same key
            var existingEntity = _ctx.ChangeTracker.Entries<ActivityScheduleAssignment>()
                .FirstOrDefault(e => e.Entity.Id == assignment.Id);
            
            if (existingEntity != null)
            {
                existingEntity.State = EntityState.Detached;
            }
            
            _ctx.ActivityScheduleAssignments.Update(assignment);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var assignment = await _ctx.ActivityScheduleAssignments.FindAsync(id);
            if (assignment != null)
            {
                _ctx.ActivityScheduleAssignments.Remove(assignment);
                await _ctx.SaveChangesAsync();
            }
        }
    }
}
