using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.MovementCriteria;

public class MovementCriterionGroupRepository : IMovementCriterionGroupRepository
{
    private readonly EduXtendContext _context;

    public MovementCriterionGroupRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovementCriterionGroup>> GetAllAsync()
    {
        return await _context.MovementCriterionGroups
            .Include(g => g.Criteria)
            .OrderBy(g => g.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementCriterionGroup>> GetByTargetTypeAsync(string targetType)
    {
        return await _context.MovementCriterionGroups
            .Include(g => g.Criteria)
            .Where(g => g.TargetType == targetType)
            .OrderBy(g => g.Id)
            .ToListAsync();
    }

    public async Task<MovementCriterionGroup?> GetByIdAsync(int id)
    {
        return await _context.MovementCriterionGroups
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<MovementCriterionGroup?> GetByIdWithCriteriaAsync(int id)
    {
        return await _context.MovementCriterionGroups
            .Include(g => g.Criteria.Where(c => c.IsActive))
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<MovementCriterionGroup> CreateAsync(MovementCriterionGroup group)
    {
        _context.MovementCriterionGroups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<MovementCriterionGroup> UpdateAsync(MovementCriterionGroup group)
    {
        _context.MovementCriterionGroups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await _context.MovementCriterionGroups.FindAsync(id);
        if (group == null)
            return false;

        _context.MovementCriterionGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.MovementCriterionGroups.AnyAsync(g => g.Id == id);
    }

    public async Task<bool> HasCriteriaAsync(int groupId)
    {
        return await _context.MovementCriteria
            .AnyAsync(c => c.GroupId == groupId);
    }
}


