using BusinessObject.Models;

namespace Repositories.Evidences;

public interface IEvidenceRepository
{
    Task<IEnumerable<Evidence>> GetAllAsync();
    Task<IEnumerable<Evidence>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Evidence>> GetByCriterionIdAsync(int criterionId);
    Task<IEnumerable<Evidence>> GetByStatusAsync(string status);
    Task<IEnumerable<Evidence>> GetPendingEvidencesAsync();
    Task<Evidence?> GetByIdAsync(int id);
    Task<Evidence?> GetByIdWithDetailsAsync(int id);
    Task<Evidence> CreateAsync(Evidence evidence);
    Task<Evidence> UpdateAsync(Evidence evidence);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> CountByStudentIdAsync(int studentId);
    Task<int> CountByStatusAsync(string status);
}


