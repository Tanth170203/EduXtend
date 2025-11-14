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
                .Where(r => r.ActivityId == activityId && r.Status != "Cancelled")
                .CountAsync();

        public async Task<int> GetAttendanceCountAsync(int activityId)
            => await _ctx.ActivityAttendances
                .Where(a => a.ActivityId == activityId && a.IsPresent)
                .CountAsync();

        public async Task<int> GetMarkedAttendanceCountAsync(int activityId)
            => await _ctx.ActivityAttendances
                .Where(a => a.ActivityId == activityId)
                .CountAsync();

        public async Task<int> GetFeedbackCountAsync(int activityId)
            => await _ctx.ActivityFeedbacks
                .Where(f => f.ActivityId == activityId)
                .CountAsync();

        public async Task<Activity> CreateAsync(Activity activity)
        {
            _ctx.Activities.Add(activity);
            await _ctx.SaveChangesAsync();
            return activity;
        }

        public async Task<Activity?> UpdateAsync(Activity activity)
        {
            var existing = await _ctx.Activities.FirstOrDefaultAsync(a => a.Id == activity.Id);
            if (existing == null) return null;

            _ctx.Entry(existing).CurrentValues.SetValues(activity);
            await _ctx.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Activities.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null) return false;

            _ctx.Activities.Remove(existing);
            await _ctx.SaveChangesAsync();
            return true;
        }

		public async Task<bool> IsRegisteredAsync(int activityId, int userId)
			=> await _ctx.ActivityRegistrations
				.Where(r => r.ActivityId == activityId && r.UserId == userId && r.Status == "Registered")
				.AnyAsync();

		public async Task<ActivityRegistration> AddRegistrationAsync(int activityId, int userId)
		{
			var reg = new ActivityRegistration
			{
				ActivityId = activityId,
				UserId = userId,
				Status = "Registered",
				CreatedAt = DateTime.UtcNow
			};
			_ctx.ActivityRegistrations.Add(reg);
			await _ctx.SaveChangesAsync();
			return reg;
		}

	public async Task<bool> IsUserMemberOfClubAsync(int userId, int clubId)
	{
		// Map user -> student -> club member
		var studentId = await _ctx.Students
			.Where(s => s.UserId == userId)
			.Select(s => s.Id)
			.FirstOrDefaultAsync();
		if (studentId == 0) return false;

		var isMember = await _ctx.ClubMembers
			.AnyAsync(m => m.ClubId == clubId && m.StudentId == studentId && m.IsActive);
		return isMember;
	}

	public async Task<bool> IsUserManagerOfClubAsync(int userId, int clubId)
	{
		// Map user -> student -> club member with Manager or President role
		var studentId = await _ctx.Students
			.Where(s => s.UserId == userId)
			.Select(s => s.Id)
			.FirstOrDefaultAsync();
		if (studentId == 0) return false;

		var isManager = await _ctx.ClubMembers
			.AnyAsync(m => m.ClubId == clubId 
				&& m.StudentId == studentId 
				&& m.IsActive
				&& (m.RoleInClub == "Manager" || m.RoleInClub == "President"));
		return isManager;
	}

		public async Task<bool> HasAttendanceAsync(int activityId, int userId)
		{
			var attendedByAttendance = await _ctx.ActivityAttendances
				.AnyAsync(a => a.ActivityId == activityId && a.UserId == userId && a.IsPresent);
			if (attendedByAttendance) return true;

			// Also allow checking via registration status = Attended
			var attendedByRegistration = await _ctx.ActivityRegistrations
				.AnyAsync(r => r.ActivityId == activityId && r.UserId == userId && r.Status == "Attended");
			return attendedByRegistration;
		}

		public async Task<bool> HasAnyAttendanceRecordAsync(int activityId, int userId)
			=> await _ctx.ActivityAttendances.AnyAsync(a => a.ActivityId == activityId && a.UserId == userId);

		public async Task<ActivityRegistration?> GetRegistrationAsync(int activityId, int userId)
			=> await _ctx.ActivityRegistrations
				.FirstOrDefaultAsync(r => r.ActivityId == activityId && r.UserId == userId);

		public async Task<bool> CancelRegistrationAsync(int activityId, int userId)
		{
			var reg = await _ctx.ActivityRegistrations
				.FirstOrDefaultAsync(r => r.ActivityId == activityId && r.UserId == userId && r.Status == "Registered");
			if (reg == null) return false;
			reg.Status = "Cancelled";
			await _ctx.SaveChangesAsync();
			return true;
		}

		public async Task<List<ActivityRegistration>> GetUserRegistrationsAsync(int userId)
			=> await _ctx.ActivityRegistrations
				.Where(r => r.UserId == userId && (r.Status == "Registered" || r.Status == "Attended"))
				.Include(r => r.Activity)
				.ThenInclude(a => a.Club)
				.ToListAsync();

		public async Task<bool> HasFeedbackAsync(int activityId, int userId)
			=> await _ctx.ActivityFeedbacks.AnyAsync(f => f.ActivityId == activityId && f.UserId == userId);

		public async Task<ActivityFeedback> AddFeedbackAsync(int activityId, int userId, int rating, string? comment)
		{
			var fb = new ActivityFeedback
			{
				ActivityId = activityId,
				UserId = userId,
				Rating = rating,
				Comment = comment,
				CreatedAt = DateTime.UtcNow
			};
			_ctx.ActivityFeedbacks.Add(fb);
			await _ctx.SaveChangesAsync();
			return fb;
		}

		public async Task<ActivityFeedback?> GetFeedbackAsync(int activityId, int userId)
			=> await _ctx.ActivityFeedbacks.FirstOrDefaultAsync(f => f.ActivityId == activityId && f.UserId == userId);

		public async Task UpdateFeedbackAsync(ActivityFeedback feedback)
		{
			_ctx.ActivityFeedbacks.Update(feedback);
			await _ctx.SaveChangesAsync();
		}

	public async Task<List<(int UserId, string FullName, string Email, bool? IsPresent, int? ParticipationScore)>> GetRegistrantsWithAttendanceAsync(int activityId)
	{
		var regs = await _ctx.ActivityRegistrations
			.Where(r => r.ActivityId == activityId && (r.Status == "Registered" || r.Status == "Attended"))
			.Select(r => new { r.UserId, r.User.FullName, r.User.Email })
			.ToListAsync();

		var attendanceMap = await _ctx.ActivityAttendances
			.Where(a => a.ActivityId == activityId)
			.ToDictionaryAsync(a => a.UserId, a => new { a.IsPresent, a.ParticipationScore });

		return regs.Select(r => {
			var attendance = attendanceMap.ContainsKey(r.UserId) ? attendanceMap[r.UserId] : null;
			return (r.UserId, r.FullName, r.Email, 
				attendance != null ? (bool?)attendance.IsPresent : null,
				attendance?.ParticipationScore);
		}).ToList();
	}

	public async Task SetAttendanceAsync(int activityId, int userId, bool isPresent, int? participationScore, int checkedById)
	{
		var existing = await _ctx.ActivityAttendances.FirstOrDefaultAsync(a => a.ActivityId == activityId && a.UserId == userId);
		if (existing == null)
		{
			_ctx.ActivityAttendances.Add(new ActivityAttendance
			{
				ActivityId = activityId,
				UserId = userId,
				IsPresent = isPresent,
				ParticipationScore = isPresent ? participationScore : null, // Chỉ lưu khi có mặt
				CheckedAt = DateTime.UtcNow,
				CheckedById = checkedById
			});
		}
		else
		{
			existing.IsPresent = isPresent;
			existing.ParticipationScore = isPresent ? participationScore : null;
			existing.CheckedAt = DateTime.UtcNow;
			existing.CheckedById = checkedById;
		}

		// Do not change registration status; keep as 'Registered' to preserve signup count.
		await _ctx.SaveChangesAsync();
	}

		public async Task<List<(int UserId, string FullName, string Email, int Rating, string? Comment, DateTime CreatedAt)>> GetFeedbacksAsync(int activityId)
		{
			return await _ctx.ActivityFeedbacks
				.Where(f => f.ActivityId == activityId)
				.Select(f => new { f.UserId, f.User.FullName, f.User.Email, f.Rating, f.Comment, f.CreatedAt })
				.AsNoTracking()
				.ToListAsync()
				.ContinueWith(t => t.Result.Select(x => (x.UserId, x.FullName, x.Email, x.Rating, x.Comment, x.CreatedAt)).ToList());
		}

		public async Task<List<(int UserId, int StudentId)>> GetClubMemberUserIdsAsync(int clubId)
		{
			return await _ctx.ClubMembers
				.Where(cm => cm.ClubId == clubId && cm.IsActive)
				.Include(cm => cm.Student)
				.ThenInclude(s => s.User)
				.Select(cm => new { cm.Student.User.Id, cm.StudentId })
				.AsNoTracking()
				.ToListAsync()
				.ContinueWith(t => t.Result.Select(x => (x.Id, x.StudentId)).ToList());
		}
    }
}

