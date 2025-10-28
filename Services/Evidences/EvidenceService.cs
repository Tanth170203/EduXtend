using BusinessObject.DTOs.Evidence;
using BusinessObject.Models;
using Repositories.Evidences;
using Repositories.Students;
using Repositories.MovementCriteria;
using Services.MovementRecords;
using Microsoft.Extensions.Logging;

namespace Services.Evidences;

public class EvidenceService : IEvidenceService
{
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMovementCriterionRepository _criterionRepository;
    private readonly IMovementRecordService _movementRecordService;
    private readonly ILogger<EvidenceService> _logger;

    public EvidenceService(
        IEvidenceRepository evidenceRepository,
        IStudentRepository studentRepository,
        IMovementCriterionRepository criterionRepository,
        IMovementRecordService movementRecordService,
        ILogger<EvidenceService> logger)
    {
        _evidenceRepository = evidenceRepository;
        _studentRepository = studentRepository;
        _criterionRepository = criterionRepository;
        _movementRecordService = movementRecordService;
        _logger = logger;
    }

    public async Task<IEnumerable<EvidenceDto>> GetAllAsync()
    {
        var evidences = await _evidenceRepository.GetAllAsync();
        return evidences.Select(MapToDto);
    }

    public async Task<IEnumerable<EvidenceDto>> GetByStudentIdAsync(int studentId)
    {
        var evidences = await _evidenceRepository.GetByStudentIdAsync(studentId);
        return evidences.Select(MapToDto);
    }

    public async Task<IEnumerable<EvidenceDto>> GetPendingEvidencesAsync()
    {
        var evidences = await _evidenceRepository.GetPendingEvidencesAsync();
        return evidences.Select(MapToDto);
    }

    public async Task<IEnumerable<EvidenceDto>> GetByStatusAsync(string status)
    {
        var evidences = await _evidenceRepository.GetByStatusAsync(status);
        return evidences.Select(MapToDto);
    }

    public async Task<EvidenceDto?> GetByIdAsync(int id)
    {
        var evidence = await _evidenceRepository.GetByIdWithDetailsAsync(id);
        return evidence != null ? MapToDto(evidence) : null;
    }

    public async Task<EvidenceDto> CreateAsync(CreateEvidenceDto dto)
    {
        // Validate student exists
        var studentExists = await _studentRepository.ExistsAsync(dto.StudentId);
        if (!studentExists)
            throw new KeyNotFoundException($"Student with ID {dto.StudentId} not found");

        // Validate criterion if provided
        if (dto.CriterionId.HasValue)
        {
            var criterionExists = await _criterionRepository.ExistsAsync(dto.CriterionId.Value);
            if (!criterionExists)
                throw new KeyNotFoundException($"Criterion with ID {dto.CriterionId.Value} not found");
        }

        var evidence = new Evidence
        {
            StudentId = dto.StudentId,
            ActivityId = dto.ActivityId,
            CriterionId = dto.CriterionId,
            Title = dto.Title,
            Description = dto.Description,
            FilePath = dto.FilePath,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };

        var created = await _evidenceRepository.CreateAsync(evidence);
        var result = await _evidenceRepository.GetByIdWithDetailsAsync(created.Id);
        return MapToDto(result!);
    }

    public async Task<EvidenceDto> UpdateAsync(int id, UpdateEvidenceDto dto)
    {
        var existing = await _evidenceRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found");

        // Only allow updates if status is Pending
        if (existing.Status != "Pending")
            throw new InvalidOperationException("Cannot update evidence that has been reviewed");

        // Validate criterion if changed
        if (dto.CriterionId.HasValue && dto.CriterionId != existing.CriterionId)
        {
            var criterionExists = await _criterionRepository.ExistsAsync(dto.CriterionId.Value);
            if (!criterionExists)
                throw new KeyNotFoundException($"Criterion with ID {dto.CriterionId.Value} not found");
        }

        existing.ActivityId = dto.ActivityId;
        existing.CriterionId = dto.CriterionId;
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.FilePath = dto.FilePath;

        var updated = await _evidenceRepository.UpdateAsync(existing);
        var result = await _evidenceRepository.GetByIdWithDetailsAsync(updated.Id);
        return MapToDto(result!);
    }

    public async Task<EvidenceDto> ReviewAsync(int id, ReviewEvidenceDto dto)
    {
        var existing = await _evidenceRepository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found");

        if (existing.Status != "Pending")
            throw new InvalidOperationException("Evidence has already been reviewed");

        existing.Status = dto.Status;
        existing.ReviewerComment = dto.ReviewerComment;
        existing.Points = dto.Points;
        existing.ReviewedById = dto.ReviewedById;
        existing.ReviewedAt = DateTime.UtcNow;

        var updated = await _evidenceRepository.UpdateAsync(existing);

        // If approved and has points, add to movement record
        if (dto.Status == "Approved" && dto.Points > 0 && existing.CriterionId.HasValue)
        {
            try
            {
                _logger.LogInformation(
                    "Adding score from evidence: EvidenceId={EvidenceId}, StudentId={StudentId}, CriterionId={CriterionId}, Points={Points}",
                    id, existing.StudentId, existing.CriterionId.Value, dto.Points);

                await _movementRecordService.AddScoreFromEvidenceAsync(
                    existing.StudentId,
                    existing.CriterionId.Value,
                    dto.Points
                );

                _logger.LogInformation(
                    "Successfully added score from evidence: EvidenceId={EvidenceId}, StudentId={StudentId}, Points={Points}",
                    id, existing.StudentId, dto.Points);
            }
            catch (Exception ex)
            {
                // Log error with full details
                _logger.LogError(ex,
                    "Failed to add score to movement record: EvidenceId={EvidenceId}, StudentId={StudentId}, CriterionId={CriterionId}, Points={Points}, Error={Error}",
                    id, existing.StudentId, existing.CriterionId.Value, dto.Points, ex.Message);

                // Optionally re-throw if you want to fail the review on scoring error
                // For now, we'll log but not fail the review to allow manual fixes
                throw new InvalidOperationException(
                    $"Không thể cộng điểm vào hồ sơ phong trào: {ex.Message}. Evidence đã được duyệt nhưng điểm chưa được cộng. Vui lòng kiểm tra và cộng điểm thủ công nếu cần.", ex);
            }
        }
        else if (dto.Status == "Approved" && dto.Points > 0 && !existing.CriterionId.HasValue)
        {
            _logger.LogWarning(
                "Evidence approved with points but no CriterionId: EvidenceId={EvidenceId}, Points={Points}",
                id, dto.Points);
        }

        var result = await _evidenceRepository.GetByIdWithDetailsAsync(updated.Id);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var exists = await _evidenceRepository.ExistsAsync(id);
        if (!exists)
            throw new KeyNotFoundException($"Evidence with ID {id} not found");

        return await _evidenceRepository.DeleteAsync(id);
    }

    public async Task<int> CountByStudentIdAsync(int studentId)
    {
        return await _evidenceRepository.CountByStudentIdAsync(studentId);
    }

    public async Task<int> CountPendingAsync()
    {
        return await _evidenceRepository.CountByStatusAsync("Pending");
    }

    // Helper method for mapping
    private static EvidenceDto MapToDto(Evidence evidence)
    {
        return new EvidenceDto
        {
            Id = evidence.Id,
            StudentId = evidence.StudentId,
            StudentName = evidence.Student?.FullName,
            StudentCode = evidence.Student?.StudentCode,
            ActivityId = evidence.ActivityId,
            ActivityTitle = evidence.Activity?.Title,
            CriterionId = evidence.CriterionId,
            CriterionTitle = evidence.Criterion?.Title,
            CriterionMaxScore = evidence.Criterion?.MaxScore ?? 0, // ✅ NEW
            Title = evidence.Title,
            Description = evidence.Description,
            FilePath = evidence.FilePath,
            Status = evidence.Status,
            ReviewerComment = evidence.ReviewerComment,
            ReviewedById = evidence.ReviewedById,
            ReviewerName = evidence.ReviewedBy?.FullName,
            ReviewedAt = evidence.ReviewedAt,
            Points = evidence.Points,
            SubmittedAt = evidence.SubmittedAt
        };
    }
}


