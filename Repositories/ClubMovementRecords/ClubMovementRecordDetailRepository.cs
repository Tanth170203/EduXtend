using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ClubMovementRecords;

public class ClubMovementRecordDetailRepository : IClubMovementRecordDetailRepository
{
    private readonly EduXtendContext _context;

    public ClubMovementRecordDetailRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<ClubMovementRecordDetail?> GetByIdAsync(int id)
    {
        return await _context.ClubMovementRecordDetails
            .Include(d => d.Criterion)
                .ThenInclude(c => c.Group)
            .Include(d => d.Activity)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<ClubMovementRecordDetail>> GetByRecordIdAsync(int recordId)
    {
        return await _context.ClubMovementRecordDetails
            .Include(d => d.Criterion)
                .ThenInclude(c => c.Group)
            .Include(d => d.Activity)
            .Include(d => d.CreatedByUser)
            .Where(d => d.ClubMovementRecordId == recordId)
            .OrderByDescending(d => d.AwardedAt)
            .ToListAsync();
    }

    public async Task<ClubMovementRecordDetail> CreateAsync(ClubMovementRecordDetail detail)
    {
        detail.AwardedAt = DateTime.UtcNow;
        _context.ClubMovementRecordDetails.Add(detail);
        await _context.SaveChangesAsync();
        return detail;
    }

    public async Task UpdateAsync(ClubMovementRecordDetail detail)
    {
        _context.ClubMovementRecordDetails.Update(detail);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var detail = await _context.ClubMovementRecordDetails.FindAsync(id);
        if (detail != null)
        {
            _context.ClubMovementRecordDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }
    }
}



