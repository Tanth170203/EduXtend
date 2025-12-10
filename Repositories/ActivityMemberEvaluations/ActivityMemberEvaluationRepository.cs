using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ActivityMemberEvaluations
{
    public class ActivityMemberEvaluationRepository : IActivityMemberEvaluationRepository
    {
        private readonly EduXtendContext _ctx;

        public ActivityMemberEvaluationRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<ActivityMemberEvaluation?> GetByIdAsync(int id)
        {
            return await _ctx.ActivityMemberEvaluations
                .AsNoTracking()
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.ActivitySchedule)
                        .ThenInclude(s => s.Activity)
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.User)
                .Include(e => e.Evaluator)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ActivityMemberEvaluation?> GetByAssignmentIdAsync(int assignmentId)
        {
            return await _ctx.ActivityMemberEvaluations
                .AsNoTracking()
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.ActivitySchedule)
                        .ThenInclude(s => s.Activity)
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.User)
                .Include(e => e.Evaluator)
                .FirstOrDefaultAsync(e => e.ActivityScheduleAssignmentId == assignmentId);
        }

        public async Task<List<ActivityMemberEvaluation>> GetByActivityIdAsync(int activityId)
        {
            return await _ctx.ActivityMemberEvaluations
                .AsNoTracking()
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.ActivitySchedule)
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.User)
                .Include(e => e.Evaluator)
                .Where(e => e.Assignment.ActivitySchedule.ActivityId == activityId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ActivityMemberEvaluation>> GetByUserIdAsync(int userId)
        {
            return await _ctx.ActivityMemberEvaluations
                .AsNoTracking()
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.ActivitySchedule)
                        .ThenInclude(s => s.Activity)
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.User)
                .Include(e => e.Evaluator)
                .Where(e => e.Assignment.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<ActivityMemberEvaluation> CreateAsync(ActivityMemberEvaluation evaluation)
        {
            _ctx.ActivityMemberEvaluations.Add(evaluation);
            await _ctx.SaveChangesAsync();
            return evaluation;
        }

        public async Task<ActivityMemberEvaluation> UpdateAsync(ActivityMemberEvaluation evaluation)
        {
            var existing = await _ctx.ActivityMemberEvaluations
                .FirstOrDefaultAsync(e => e.Id == evaluation.Id);
            
            if (existing == null)
                throw new InvalidOperationException($"Evaluation with ID {evaluation.Id} not found");

            existing.ResponsibilityScore = evaluation.ResponsibilityScore;
            existing.SkillScore = evaluation.SkillScore;
            existing.AttitudeScore = evaluation.AttitudeScore;
            existing.EffectivenessScore = evaluation.EffectivenessScore;
            existing.AverageScore = evaluation.AverageScore;
            existing.Comments = evaluation.Comments;
            existing.Strengths = evaluation.Strengths;
            existing.Improvements = evaluation.Improvements;
            existing.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.ActivityMemberEvaluations
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (existing == null)
                return false;

            _ctx.ActivityMemberEvaluations.Remove(existing);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int assignmentId)
        {
            return await _ctx.ActivityMemberEvaluations
                .AnyAsync(e => e.ActivityScheduleAssignmentId == assignmentId);
        }
    }
}
