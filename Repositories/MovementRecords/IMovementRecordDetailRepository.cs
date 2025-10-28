using BusinessObject.Models;

namespace Repositories.MovementRecords;

public interface IMovementRecordDetailRepository
{
    Task<IEnumerable<MovementRecordDetail>> GetByRecordIdAsync(int recordId);
    Task<MovementRecordDetail?> GetByIdAsync(int id);
    Task<MovementRecordDetail?> GetByRecordAndCriterionAsync(int recordId, int criterionId);
    Task<MovementRecordDetail?> GetByRecordCriterionActivityAsync(int recordId, int criterionId, int? activityId);
    Task<MovementRecordDetail> CreateAsync(MovementRecordDetail detail);
    Task<MovementRecordDetail> UpdateAsync(MovementRecordDetail detail);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int recordId, int criterionId);
    Task<double> GetTotalScoreByRecordIdAsync(int recordId);
}


