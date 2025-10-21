using BusinessObject.Models;

namespace Repositories.MovementRecords;

public interface IMovementRecordRepository
{
    Task<IEnumerable<MovementRecord>> GetAllAsync();
    Task<IEnumerable<MovementRecord>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<MovementRecord>> GetBySemesterIdAsync(int semesterId);
    Task<MovementRecord?> GetByIdAsync(int id);
    Task<MovementRecord?> GetByIdWithDetailsAsync(int id);
    Task<MovementRecord?> GetByStudentAndSemesterAsync(int studentId, int semesterId);
    Task<MovementRecord> CreateAsync(MovementRecord record);
    Task<MovementRecord> UpdateAsync(MovementRecord record);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsForStudentInSemesterAsync(int studentId, int semesterId);
    Task<double> GetAverageScoreByStudentIdAsync(int studentId);
    Task<IEnumerable<MovementRecord>> GetTopScoresBySemesterAsync(int semesterId, int count);
}


