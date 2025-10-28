using BusinessObject.Models;

namespace Repositories.ClubMovementRecords;

public interface IClubMovementRecordDetailRepository
{
    Task<ClubMovementRecordDetail?> GetByIdAsync(int id);
    Task<List<ClubMovementRecordDetail>> GetByRecordIdAsync(int recordId);
    Task<ClubMovementRecordDetail> CreateAsync(ClubMovementRecordDetail detail);
    Task UpdateAsync(ClubMovementRecordDetail detail);
    Task DeleteAsync(int id);
}


