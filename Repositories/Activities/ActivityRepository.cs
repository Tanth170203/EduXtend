using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Activities
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly EduXtendContext _ctx;
        public ActivityRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<List<Activity>> GetAllAsync()
            => await _ctx.Activities
                .AsNoTracking()
                .Include(a => a.Club)
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

        public async Task<List<Activity>> SearchActivitiesAsync(
            string? searchTerm, 
            string? type, 
            string? status, 
            bool? isPublic, 
            int? clubId)
        {
            var query = _ctx.Activities
                .AsNoTracking()
                .Include(a => a.Club)
                .Include(a => a.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.Title.Contains(searchTerm) ||
                    (a.Description != null && a.Description.Contains(searchTerm)) ||
                    (a.Location != null && a.Location.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.Type.ToString() == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (isPublic.HasValue)
            {
                query = query.Where(a => a.IsPublic == isPublic.Value);
            }

            if (clubId.HasValue)
            {
                query = query.Where(a => a.ClubId == clubId.Value);
            }

            return await query.OrderByDescending(a => a.StartTime).ToListAsync();
        }

        public async Task<Activity?> GetByIdAsync(int id)
            => await _ctx.Activities
                .AsNoTracking()
                .Include(a => a.Club)
                .Include(a => a.CreatedBy)
                .Include(a => a.ApprovedBy)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<Activity?> GetByIdWithDetailsAsync(int id)
            => await _ctx.Activities
                .AsNoTracking()
                .Include(a => a.Club)
                .Include(a => a.CreatedBy)
                .Include(a => a.ApprovedBy)
                .Include(a => a.Registrations)
                .Include(a => a.Attendances)
                .Include(a => a.Feedbacks)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<List<Activity>> GetActivitiesByClubIdAsync(int clubId)
            => await _ctx.Activities
                .AsNoTracking()
                .Where(a => a.ClubId == clubId)
                .Include(a => a.Club)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

        public async Task<int> GetRegistrationCountAsync(int activityId)
            => await _ctx.ActivityRegistrations
                .Where(r => r.ActivityId == activityId)
                .CountAsync();

        public async Task<int> GetAttendanceCountAsync(int activityId)
            => await _ctx.ActivityAttendances
                .Where(a => a.ActivityId == activityId && a.IsPresent)
                .CountAsync();

        public async Task<int> GetFeedbackCountAsync(int activityId)
            => await _ctx.ActivityFeedbacks
                .Where(f => f.ActivityId == activityId)
                .CountAsync();
    }
}



