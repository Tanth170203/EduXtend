using BusinessObject.DTOs.Evidence;

namespace Services.Evidences;

public interface IEvidenceService
{
    Task<IEnumerable<EvidenceDto>> GetAllAsync();
    Task<IEnumerable<EvidenceDto>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<EvidenceDto>> GetPendingEvidencesAsync();
    Task<IEnumerable<EvidenceDto>> GetByStatusAsync(string status);
    Task<EvidenceDto?> GetByIdAsync(int id);
    Task<EvidenceDto> CreateAsync(CreateEvidenceDto dto);
    Task<EvidenceDto> UpdateAsync(int id, UpdateEvidenceDto dto);
    Task<EvidenceDto> ReviewAsync(int id, ReviewEvidenceDto dto);
    Task<bool> DeleteAsync(int id);
    Task<int> CountByStudentIdAsync(int studentId);
    Task<int> CountPendingAsync();
}


