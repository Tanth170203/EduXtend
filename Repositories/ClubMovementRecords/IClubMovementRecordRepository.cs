using BusinessObject.Models;

namespace Repositories.ClubMovementRecords;

public interface IClubMovementRecordRepository
{
    Task<ClubMovementRecord?> GetByClubMonthAsync(int clubId, int semesterId, int month);
    Task<List<ClubMovementRecord>> GetByClubAsync(int clubId, int semesterId);
    Task<List<ClubMovementRecord>> GetAllByClubAsync(int clubId);
    Task<List<ClubMovementRecord>> GetAllByMonthAsync(int semesterId, int month);
    Task<ClubMovementRecord?> GetByIdAsync(int id);
    Task<ClubMovementRecord> CreateAsync(ClubMovementRecord record);
    Task UpdateAsync(ClubMovementRecord record);
    Task RecalculateTotalScoreAsync(int recordId);
    Task<List<ClubMovementRecordDetail>> GetDetailsByClubAndWeekAsync(int clubId, int semesterId, DateTime weekStart, DateTime weekEnd);
}




