using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.MovementRecords;

public class MovementRecordRepository : IMovementRecordRepository
{
    private readonly EduXtendContext _context;

    public MovementRecordRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovementRecord>> GetAllAsync()
    {
        return await _context.MovementRecords
            .Include(r => r.Student)
            .Include(r => r.Semester)
            .OrderByDescending(r => r.LastUpdated ?? r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementRecord>> GetByStudentIdAsync(int studentId)
    {
        return await _context.MovementRecords
            .Include(r => r.Student)
            .Include(r => r.Semester)
            .Include(r => r.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.Semester.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovementRecord>> GetBySemesterIdAsync(int semesterId)
    {
        return await _context.MovementRecords
            .Include(r => r.Student)
            .Where(r => r.SemesterId == semesterId)
            .OrderByDescending(r => r.TotalScore)
            .ToListAsync();
    }

    public async Task<MovementRecord?> GetByIdAsync(int id)
    {
        return await _context.MovementRecords
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<MovementRecord?> GetByIdWithDetailsAsync(int id)
    {
        // Use AsNoTracking to always get fresh data from database
        // This prevents EF from returning cached entities
        return await _context.MovementRecords
            .AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.Semester)
            .Include(r => r.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<MovementRecord?> GetByStudentAndSemesterAsync(int studentId, int semesterId)
    {
        return await _context.MovementRecords
            .Include(r => r.Student)
            .Include(r => r.Semester)
            .Include(r => r.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);
    }

    public async Task<MovementRecord> CreateAsync(MovementRecord record)
    {
        _context.MovementRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<MovementRecord> UpdateAsync(MovementRecord record)
    {
        record.LastUpdated = DateTime.UtcNow;
        
        // Check if entity is already being tracked
        var trackedEntity = _context.ChangeTracker.Entries<MovementRecord>()
            .FirstOrDefault(e => e.Entity.Id == record.Id);
        
        if (trackedEntity != null)
        {
            // Entity is already tracked, update its values
            trackedEntity.CurrentValues.SetValues(record);
        }
        else
        {
            // Entity is not tracked, attach and mark as modified
            _context.MovementRecords.Update(record);
        }
        
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await GetByIdAsync(id);
        if (record == null)
            return false;

        _context.MovementRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.MovementRecords.AnyAsync(r => r.Id == id);
    }

    public async Task<bool> ExistsForStudentInSemesterAsync(int studentId, int semesterId)
    {
        return await _context.MovementRecords
            .AnyAsync(r => r.StudentId == studentId && r.SemesterId == semesterId);
    }

    public async Task<double> GetAverageScoreByStudentIdAsync(int studentId)
    {
        var records = await _context.MovementRecords
            .Where(r => r.StudentId == studentId)
            .ToListAsync();

        return records.Any() ? records.Average(r => r.TotalScore) : 0;
    }

    public async Task<IEnumerable<MovementRecord>> GetTopScoresBySemesterAsync(int semesterId, int count)
    {
        return await _context.MovementRecords
            .Include(r => r.Student)
            .Include(r => r.Semester)
            .Where(r => r.SemesterId == semesterId)
            .OrderByDescending(r => r.TotalScore)
            .Take(count)
            .ToListAsync();
    }
}


