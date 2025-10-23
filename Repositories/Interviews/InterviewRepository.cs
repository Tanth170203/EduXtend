using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Interviews
{
    public class InterviewRepository : IInterviewRepository
    {
        private readonly EduXtendContext _context;

        public InterviewRepository(EduXtendContext context)
        {
            _context = context;
        }

        public async Task<Interview?> GetByIdAsync(int id)
        {
            return await _context.Interviews
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.User)
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.Club)
                .Include(i => i.CreatedBy)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Interview?> GetByJoinRequestIdAsync(int joinRequestId)
        {
            return await _context.Interviews
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.User)
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.Club)
                .Include(i => i.CreatedBy)
                .FirstOrDefaultAsync(i => i.JoinRequestId == joinRequestId);
        }

        public async Task<List<Interview>> GetByUserIdAsync(int userId)
        {
            return await _context.Interviews
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.User)
                .Include(i => i.JoinRequest)
                    .ThenInclude(jr => jr.Club)
                .Include(i => i.CreatedBy)
                .Where(i => i.JoinRequest.UserId == userId)
                .OrderByDescending(i => i.ScheduledDate)
                .ToListAsync();
        }

        public async Task<Interview> CreateAsync(Interview interview)
        {
            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<Interview> UpdateAsync(Interview interview)
        {
            _context.Interviews.Update(interview);
            await _context.SaveChangesAsync();
            return interview;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var interview = await _context.Interviews.FindAsync(id);
            if (interview == null) return false;

            _context.Interviews.Remove(interview);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsForJoinRequestAsync(int joinRequestId)
        {
            return await _context.Interviews.AnyAsync(i => i.JoinRequestId == joinRequestId);
        }
    }
}

