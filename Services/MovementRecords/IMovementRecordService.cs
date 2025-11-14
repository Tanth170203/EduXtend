using BusinessObject.DTOs.MovementRecord;

namespace Services.MovementRecords;

public interface IMovementRecordService
{
    Task<IEnumerable<MovementRecordDto>> GetAllAsync();
    Task<IEnumerable<MovementRecordDto>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<MovementRecordDto>> GetBySemesterIdAsync(int semesterId);
    Task<MovementRecordDto?> GetByIdAsync(int id);
    Task<MovementRecordDetailedDto?> GetDetailedByIdAsync(int id);
    Task<MovementRecordDto?> GetByStudentAndSemesterAsync(int studentId, int semesterId);
    Task<MovementRecordDto> CreateAsync(CreateMovementRecordDto dto);
    Task<MovementRecordDto> AddScoreAsync(AddScoreDto dto);
    Task CapAndAdjustScoresAsync(int recordId);
    Task<MovementRecordDto> AdjustScoreAsync(int id, AdjustScoreDto dto);
    Task<bool> DeleteAsync(int id);
    Task<StudentMovementSummaryDto?> GetStudentSummaryAsync(int studentId);
    Task<IEnumerable<MovementRecordDto>> GetTopScoresBySemesterAsync(int semesterId, int count);
    Task<MovementRecordDto> AddScoreFromEvidenceAsync(int studentId, int criterionId, double points);
    Task<MovementRecordDto> AddScoreFromAttendanceAsync(int studentId, int criterionId, double points, int activityId);
    Task<MovementRecordDto> AddManualScoreAsync(AddManualScoreDto dto);
    Task<MovementRecordDto> AddManualScoreWithCriterionAsync(AddManualScoreWithCriterionDto dto);
}


