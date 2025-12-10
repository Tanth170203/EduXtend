using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Repositories.ClubMovementRecords;

public class ClubMovementRecordRepository : IClubMovementRecordRepository
{
    private readonly EduXtendContext _context;

    public ClubMovementRecordRepository(EduXtendContext context)
    {
        _context = context;
    }

    public async Task<ClubMovementRecord?> GetByClubMonthAsync(int clubId, int semesterId, int month)
    {
        return await _context.ClubMovementRecords
            .AsSplitQuery()
            .Include(cmr => cmr.Club)
            .Include(cmr => cmr.Semester)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Activity)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.CreatedByUser)
            .FirstOrDefaultAsync(cmr => 
                cmr.ClubId == clubId && 
                cmr.SemesterId == semesterId && 
                cmr.Month == month);
    }

    public async Task<List<ClubMovementRecord>> GetByClubAsync(int clubId, int semesterId)
    {
        return await _context.ClubMovementRecords
            .Include(cmr => cmr.Club)
            .Include(cmr => cmr.Semester)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Criterion)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Activity)  // Fix: Load Activity for weekly limit check
            .Where(cmr => cmr.ClubId == clubId && cmr.SemesterId == semesterId)
            .OrderBy(cmr => cmr.Month)
            .ToListAsync();
    }

    public async Task<List<ClubMovementRecord>> GetAllByClubAsync(int clubId)
    {
        return await _context.ClubMovementRecords
            .AsSplitQuery()
            .Include(cmr => cmr.Club)
            .Include(cmr => cmr.Semester)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Activity)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.CreatedByUser)
            .Where(cmr => cmr.ClubId == clubId)
            .OrderByDescending(cmr => cmr.Semester.StartDate)
            .ThenBy(cmr => cmr.Month)
            .ToListAsync();
    }

    public async Task<List<ClubMovementRecord>> GetAllByMonthAsync(int semesterId, int month)
    {
        return await _context.ClubMovementRecords
            .Include(cmr => cmr.Club)
            .Include(cmr => cmr.Semester)
            .Where(cmr => cmr.SemesterId == semesterId && cmr.Month == month)
            .OrderByDescending(cmr => cmr.TotalScore)
            .ToListAsync();
    }

    public async Task<ClubMovementRecord?> GetByIdAsync(int id)
    {
        return await _context.ClubMovementRecords
            .AsSplitQuery()
            .Include(cmr => cmr.Club)
            .Include(cmr => cmr.Semester)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Criterion)
                    .ThenInclude(c => c.Group)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Activity)
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.CreatedByUser)
            .FirstOrDefaultAsync(cmr => cmr.Id == id);
    }

    public async Task<ClubMovementRecord> CreateAsync(ClubMovementRecord record)
    {
        record.CreatedAt = DateTime.UtcNow;
        record.LastUpdated = DateTime.UtcNow;
        _context.ClubMovementRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task UpdateAsync(ClubMovementRecord record)
    {
        record.LastUpdated = DateTime.UtcNow;
        _context.ClubMovementRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task RecalculateTotalScoreAsync(int recordId)
    {
        var record = await _context.ClubMovementRecords
            .Include(cmr => cmr.Details)
                .ThenInclude(d => d.Criterion)
            .FirstOrDefaultAsync(cmr => cmr.Id == recordId);

        if (record == null) return;

        // Recalculate from details
        record.ClubMeetingScore = record.Details
            .Where(d => d.Criterion != null && d.Criterion.Title.Contains("Sinh hoạt CLB"))
            .Sum(d => d.Score);

        record.EventScore = record.Details
            .Where(d => d.Criterion != null && d.Criterion.Title.Contains("Sự kiện"))
            .Sum(d => d.Score);

        record.CompetitionScore = record.Details
            .Where(d => d.Criterion != null && (d.Criterion.Title.Contains("thi") || d.Criterion.Title.Contains("Thi")))
            .Sum(d => d.Score);

        record.PlanScore = record.Details
            .Where(d => d.Criterion != null && d.Criterion.Title.Contains("Kế hoạch"))
            .Sum(d => d.Score);

        record.CollaborationScore = record.Details
            .Where(d => d.Criterion != null && d.Criterion.Title.Contains("Phối hợp"))
            .Sum(d => d.Score);

        record.TotalScore = record.Details.Sum(d => d.Score);
        
        // Cap total score at 100
        if (record.TotalScore > 100)
        {
            record.TotalScore = 100;
        }

        record.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<ClubMovementRecordDetail>> GetDetailsByClubAndWeekAsync(int clubId, int semesterId, DateTime weekStart, DateTime weekEnd)
    {
        return await _context.ClubMovementRecordDetails
            .Include(d => d.ClubMovementRecord)
            .Include(d => d.Criterion)
            .Include(d => d.Activity)
            .Where(d => 
                d.ClubMovementRecord.ClubId == clubId &&
                d.ClubMovementRecord.SemesterId == semesterId &&
                d.AwardedAt >= weekStart &&
                d.AwardedAt < weekEnd)
            .ToListAsync();
    }
}

