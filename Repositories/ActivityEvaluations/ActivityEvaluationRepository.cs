using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ActivityEvaluations
{
    public class ActivityEvaluationRepository : IActivityEvaluationRepository
    {
        private readonly EduXtendContext _ctx;

        public ActivityEvaluationRepository(EduXtendContext ctx) => _ctx = ctx;

        public async Task<ActivityEvaluation?> GetByIdAsync(int id)
        {
            return await _ctx.ActivityEvaluations
                .AsNoTracking()
                .Include(e => e.Activity)
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ActivityEvaluation?> GetByActivityIdAsync(int activityId)
        {
            return await _ctx.ActivityEvaluations
                .AsNoTracking()
                .Include(e => e.Activity)
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.ActivityId == activityId);
        }

        public async Task<ActivityEvaluation> CreateAsync(ActivityEvaluation evaluation)
        {
            _ctx.ActivityEvaluations.Add(evaluation);
            await _ctx.SaveChangesAsync();
            return evaluation;
        }

        public async Task<ActivityEvaluation> UpdateAsync(ActivityEvaluation evaluation)
        {
            var existing = await _ctx.ActivityEvaluations
                .FirstOrDefaultAsync(e => e.Id == evaluation.Id);
            
            if (existing == null)
                throw new InvalidOperationException($"Evaluation with ID {evaluation.Id} not found");

            existing.CommunicationScore = evaluation.CommunicationScore;
            existing.OrganizationScore = evaluation.OrganizationScore;
            existing.HostScore = evaluation.HostScore;
            existing.SpeakerScore = evaluation.SpeakerScore;
            existing.Success = evaluation.Success;
            existing.Limitations = evaluation.Limitations;
            existing.ImprovementMeasures = evaluation.ImprovementMeasures;
            existing.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.ActivityEvaluations
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (existing == null)
                return false;

            _ctx.ActivityEvaluations.Remove(existing);
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int activityId)
        {
            return await _ctx.ActivityEvaluations
                .AnyAsync(e => e.ActivityId == activityId);
        }
    }
}
