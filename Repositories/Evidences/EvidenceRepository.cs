using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Evidences;

public class EvidenceRepository : IEvidenceRepository
{
    private readonly EduXtendContext _context;

    public EvidenceRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Evidence>> GetAllAsync()
    {
        return await _context.Evidences
            .Include(e => e.Student)
            .Include(e => e.Activity)
            .Include(e => e.Criterion)
                .ThenInclude(c => c.Group)
            .Include(e => e.ReviewedBy)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evidence>> GetByStudentIdAsync(int studentId)
    {
        return await _context.Evidences
            .Include(e => e.Activity)
            .Include(e => e.Criterion)
                .ThenInclude(c => c.Group)
            .Include(e => e.ReviewedBy)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evidence>> GetByCriterionIdAsync(int criterionId)
    {
        return await _context.Evidences
            .Include(e => e.Student)
            .Include(e => e.Activity)
            .Include(e => e.Criterion)
            .Include(e => e.ReviewedBy)
            .Where(e => e.CriterionId == criterionId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evidence>> GetByStatusAsync(string status)
    {
        return await _context.Evidences
            .Include(e => e.Student)
            .Include(e => e.Activity)
            .Include(e => e.Criterion)
                .ThenInclude(c => c.Group)
            .Include(e => e.ReviewedBy)
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evidence>> GetPendingEvidencesAsync()
    {
        return await GetByStatusAsync("Pending");
    }

    public async Task<Evidence?> GetByIdAsync(int id)
    {
        return await _context.Evidences
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Evidence?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Evidences
            .Include(e => e.Student)
            .Include(e => e.Activity)
            .Include(e => e.Criterion)
                .ThenInclude(c => c.Group)
            .Include(e => e.ReviewedBy)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Evidence> CreateAsync(Evidence evidence)
    {
        _context.Evidences.Add(evidence);
        await _context.SaveChangesAsync();
        return evidence;
    }

    public async Task<Evidence> UpdateAsync(Evidence evidence)
    {
        _context.Evidences.Update(evidence);
        await _context.SaveChangesAsync();
        return evidence;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var evidence = await GetByIdAsync(id);
        if (evidence == null)
            return false;

        _context.Evidences.Remove(evidence);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Evidences.AnyAsync(e => e.Id == id);
    }

    public async Task<int> CountByStudentIdAsync(int studentId)
    {
        return await _context.Evidences
            .CountAsync(e => e.StudentId == studentId);
    }

    public async Task<int> CountByStatusAsync(string status)
    {
        return await _context.Evidences
            .CountAsync(e => e.Status == status);
    }
}


