using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.MovementCriteria;

public class MovementCriterionRepository : IMovementCriterionRepository
{
    private readonly EduXtendContext _context;

    public MovementCriterionRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovementCriterion>> GetAllAsync()
    {
        return await _context.MovementCriteria
            .Include(c => c.Group)
            .OrderBy(c => c.GroupId)
            .ThenBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementCriterion>> GetByGroupIdAsync(int groupId)
    {
        return await _context.MovementCriteria
            .Include(c => c.Group)
            .Where(c => c.GroupId == groupId)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementCriterion>> GetByTargetTypeAsync(string targetType)
    {
        return await _context.MovementCriteria
            .Include(c => c.Group)
            .Where(c => c.TargetType == targetType)
            .OrderBy(c => c.GroupId)
            .ThenBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementCriterion>> GetActiveAsync()
    {
        return await _context.MovementCriteria
            .Include(c => c.Group)
            .Where(c => c.IsActive)
            .OrderBy(c => c.GroupId)
            .ThenBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<MovementCriterion?> GetByIdAsync(int id)
    {
        return await _context.MovementCriteria
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MovementCriterion?> GetByIdWithGroupAsync(int id)
    {
        return await _context.MovementCriteria
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<MovementCriterion> CreateAsync(MovementCriterion criterion)
    {
        _context.MovementCriteria.Add(criterion);
        await _context.SaveChangesAsync();
        return criterion;
    }

    public async Task<MovementCriterion> UpdateAsync(MovementCriterion criterion)
    {
        _context.MovementCriteria.Update(criterion);
        await _context.SaveChangesAsync();
        return criterion;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var criterion = await _context.MovementCriteria.FindAsync(id);
        if (criterion == null)
            return false;

        _context.MovementCriteria.Remove(criterion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.MovementCriteria.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> HasRelatedDataAsync(int criterionId)
    {
        // Kiểm tra xem có MovementRecordDetail hoặc Evidence liên quan không
        var hasRecordDetails = await _context.MovementRecordDetails
            .AnyAsync(mrd => mrd.CriterionId == criterionId);

        var hasEvidences = await _context.Evidences
            .AnyAsync(e => e.CriterionId == criterionId);

        return hasRecordDetails || hasEvidences;
    }
}



