using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.MovementRecords;

public class MovementRecordDetailRepository : IMovementRecordDetailRepository
{
    private readonly EduXtendContext _context;
        
    public MovementRecordDetailRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovementRecordDetail>> GetByRecordIdAsync(int recordId)
    {
        return await _context.MovementRecordDetails
            .Include(d => d.Criterion)
                .ThenInclude(c => c.Group)
            .Where(d => d.MovementRecordId == recordId)
            .OrderByDescending(d => d.AwardedAt)
            .ToListAsync();
    }

    public async Task<MovementRecordDetail?> GetByIdAsync(int id)
    {
        return await _context.MovementRecordDetails
            .Include(d => d.Criterion)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<MovementRecordDetail?> GetByRecordAndCriterionAsync(int recordId, int criterionId)
    {
        return await _context.MovementRecordDetails
            .Include(d => d.Criterion)
            .FirstOrDefaultAsync(d => d.MovementRecordId == recordId && d.CriterionId == criterionId);
    }

    public async Task<MovementRecordDetail> CreateAsync(MovementRecordDetail detail)
    {
        _context.MovementRecordDetails.Add(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    public async Task<MovementRecordDetail> UpdateAsync(MovementRecordDetail detail)
    {
        _context.MovementRecordDetails.Update(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var detail = await GetByIdAsync(id);
        if (detail == null)
            return false;

        _context.MovementRecordDetails.Remove(detail);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int recordId, int criterionId)
    {
        return await _context.MovementRecordDetails
            .AnyAsync(d => d.MovementRecordId == recordId && d.CriterionId == criterionId);
    }

    public async Task<double> GetTotalScoreByRecordIdAsync(int recordId)
    {
        var details = await _context.MovementRecordDetails
            .Where(d => d.MovementRecordId == recordId)
            .ToListAsync();

        return details.Sum(d => d.Score);
    }
}


