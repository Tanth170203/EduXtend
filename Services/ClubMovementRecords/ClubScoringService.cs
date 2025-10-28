using BusinessObject.DTOs.ClubMovementRecord;
using BusinessObject.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.ClubMovementRecords;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Services.ClubMovementRecords;

public class ClubScoringService : IClubScoringService
{
    private readonly EduXtendContext _context;
    private readonly IClubMovementRecordRepository _recordRepo;
    private readonly IClubMovementRecordDetailRepository _detailRepo;
    private readonly ILogger<ClubScoringService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClubScoringService(
        EduXtendContext context,
        IClubMovementRecordRepository recordRepo,
        IClubMovementRecordDetailRepository detailRepo,
        ILogger<ClubScoringService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _recordRepo = recordRepo;
        _detailRepo = detailRepo;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClubMovementRecordDto?> GetClubScoreAsync(int clubId, int semesterId, int month)
    {
        var record = await _recordRepo.GetByClubMonthAsync(clubId, semesterId, month);
        if (record == null) return null;

        return await MapToDto(record);
    }

    public async Task<List<ClubMovementRecordDto>> GetAllClubScoresAsync(int semesterId, int month)
    {
        var records = await _recordRepo.GetAllByMonthAsync(semesterId, month);
        var dtos = new List<ClubMovementRecordDto>();

        foreach (var record in records)
        {
            dtos.Add(await MapToDto(record));
        }

        return dtos;
    }

    // Services/ClubMovementRecords/ClubScoringService.cs

    public async Task<ClubMovementRecordDto> AddManualScoreAsync(AddClubManualScoreDto dto)
    {
        try
        {
            // 1. Get or create ClubMovementRecord
            var record = await _recordRepo.GetByClubMonthAsync(dto.ClubId, dto.SemesterId, dto.Month);
            // ... (Logic tạo record giữ nguyên)
            if (record == null)
            {
                record = new ClubMovementRecord
                {
                    ClubId = dto.ClubId,
                    SemesterId = dto.SemesterId,
                    Month = dto.Month,
                    ClubMeetingScore = 0,
                    EventScore = 0,
                    CompetitionScore = 0,
                    PlanScore = 0,
                    CollaborationScore = 0,
                    TotalScore = 0
                };
                record = await _recordRepo.CreateAsync(record);
            }

            // 2. Get criterion & 3. Validate score range (Giữ nguyên)
            var criterion = await _context.MovementCriteria
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == dto.CriterionId);

            if (criterion == null)
                throw new Exception($"Criterion {dto.CriterionId} not found");

            if (criterion.TargetType != "Club")
                throw new Exception("Criterion is not for Club");

            var minScore = criterion.MinScore ?? 0;
            var maxScore = criterion.MaxScore;

            if (dto.Score < minScore || dto.Score > maxScore)
                throw new Exception($"Score must be between {minScore} and {maxScore} for this criterion");

            // Get CreatedBy from DTO (set by API Controller) or fallback to HttpContext
            int? createdBy = dto.CreatedById;
            
            if (createdBy == null)
            {
                // Fallback to HttpContext if not set in DTO
                try
                {
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var uid) && uid > 0)
                    {
                        createdBy = uid;
                        _logger.LogInformation("Using CreatedBy from HttpContext fallback: {UserId}", uid);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting userId from HttpContext");
                }
            }
            
            if (createdBy.HasValue)
            {
                _logger.LogInformation("Using CreatedBy: {UserId}", createdBy.Value);
            }
            else
            {
                _logger.LogWarning("No valid CreatedBy found, proceeding with NULL CreatedBy.");
            }

            // 5. Create Movement Record Detail và gán CreatedBy
            var detail = new ClubMovementRecordDetail
            {
                ClubMovementRecordId = record.Id,
                CriterionId = dto.CriterionId,
                Score = dto.Score,
                ScoreType = "Manual",
                Note = dto.Note,
                // Gán CreatedBy đã lấy được
                CreatedBy = createdBy
            };

            // Log giá trị trước khi lưu
            _logger.LogInformation("Attempting to save score. ClubId: {ClubId}, CriterionId: {CriterionId}, Final CreatedBy: {CreatedBy}",
                                   dto.ClubId, dto.CriterionId, createdBy.HasValue ? createdBy.Value.ToString() : "NULL");

            await _detailRepo.CreateAsync(detail);

            // 6. Recalculate total
            await _recordRepo.RecalculateTotalScoreAsync(record.Id);

            // 7. Reload and return
            record = await _recordRepo.GetByIdAsync(record.Id);
            return await MapToDto(record!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding manual score for club {ClubId}", dto.ClubId);
            throw;
        }
    }

    public async Task<ClubMovementRecordDto> UpdateManualScoreAsync(UpdateClubManualScoreDto dto, int adminUserId)
    {
        try
        {
            var detail = await _detailRepo.GetByIdAsync(dto.DetailId);
            if (detail == null)
                throw new Exception($"Score detail {dto.DetailId} not found");

            if (detail.ScoreType != "Manual")
                throw new Exception("Can only update manual scores");

            // Validate score range
            var criterion = await _context.MovementCriteria
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == detail.CriterionId);

            if (criterion != null)
            {
                var minScore = criterion.MinScore ?? 0;
                var maxScore = criterion.MaxScore;

                if (dto.Score < minScore || dto.Score > maxScore)
                    throw new Exception($"Score must be between {minScore} and {maxScore}");
            }

            if (dto.Score <= 0) throw new Exception("Score must be positive");
            detail.Score = dto.Score;
            detail.Note = dto.Note;
            await _detailRepo.UpdateAsync(detail);

            // Recalculate total
            await _recordRepo.RecalculateTotalScoreAsync(detail.ClubMovementRecordId);

            // Reload and return
            var record = await _recordRepo.GetByIdAsync(detail.ClubMovementRecordId);
            return await MapToDto(record!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating manual score {DetailId}", dto.DetailId);
            throw;
        }
    }

    public async Task DeleteManualScoreAsync(int detailId)
    {
        try
        {
            var detail = await _detailRepo.GetByIdAsync(detailId);
            if (detail == null)
                throw new Exception($"Score detail {detailId} not found");

            if (detail.ScoreType != "Manual")
                throw new Exception("Can only delete manual scores");

            var recordId = detail.ClubMovementRecordId;
            await _detailRepo.DeleteAsync(detailId);

            // Recalculate total
            await _recordRepo.RecalculateTotalScoreAsync(recordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting manual score {DetailId}", detailId);
            throw;
        }
    }

    public async Task UpdatePlanScoreAsync(int clubId, int semesterId, int month, bool isCompleted)
    {
        try
        {
            // Get or create record
            var record = await _recordRepo.GetByClubMonthAsync(clubId, semesterId, month);
            if (record == null)
            {
                record = new ClubMovementRecord
                {
                    ClubId = clubId,
                    SemesterId = semesterId,
                    Month = month,
                    ClubMeetingScore = 0,
                    EventScore = 0,
                    CompetitionScore = 0,
                    PlanScore = 0,
                    CollaborationScore = 0,
                    TotalScore = 0
                };
                record = await _recordRepo.CreateAsync(record);
            }

            // Get plan criterion
            var planCriterion = await _context.MovementCriteria
                .FirstOrDefaultAsync(c => c.Title.Contains("Kế hoạch") 
                                       && c.TargetType == "Club" 
                                       && c.IsActive);

            if (planCriterion == null)
            {
                _logger.LogWarning("Plan criterion not found");
                return;
            }

            // Check if plan score already exists
            var existingDetail = await _context.ClubMovementRecordDetails
                .FirstOrDefaultAsync(d => d.ClubMovementRecordId == record.Id 
                                       && d.CriterionId == planCriterion.Id);

            if (isCompleted)
            {
                if (existingDetail == null)
                {
                    // Add plan score (10 points)
                    var detail = new ClubMovementRecordDetail
                    {
                        ClubMovementRecordId = record.Id,
                        CriterionId = planCriterion.Id,
                        Score = 10,
                        ScoreType = "Manual",
                        Note = "Hoàn thành kế hoạch đúng hạn"
                    };
                    await _detailRepo.CreateAsync(detail);
                }
            }
            else
            {
                if (existingDetail != null)
                {
                    // Remove plan score
                    await _detailRepo.DeleteAsync(existingDetail.Id);
                }
            }

            // Recalculate total
            await _recordRepo.RecalculateTotalScoreAsync(record.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan score for club {ClubId}", clubId);
            throw;
        }
    }

    private async Task<ClubMovementRecordDto> MapToDto(ClubMovementRecord record)
    {
        // Get president info
        var president = await _context.ClubMembers
            .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(cm => cm.ClubId == record.ClubId 
                                    && cm.RoleInClub == "President" 
                                    && cm.IsActive);

        var dto = new ClubMovementRecordDto
        {
            Id = record.Id,
            ClubId = record.ClubId,
            ClubName = record.Club.Name,
            SemesterId = record.SemesterId,
            SemesterName = record.Semester.Name,
            Month = record.Month,
            ClubMeetingScore = record.ClubMeetingScore,
            EventScore = record.EventScore,
            CompetitionScore = record.CompetitionScore,
            PlanScore = record.PlanScore,
            CollaborationScore = record.CollaborationScore,
            TotalScore = record.TotalScore,
            CreatedAt = record.CreatedAt,
            LastUpdated = record.LastUpdated,
            PresidentName = president?.Student?.FullName ?? "N/A",
            PresidentCode = president?.Student?.StudentCode ?? "N/A",
            PresidentEmail = president?.Student?.User?.Email ?? "N/A"
        };

        // Map details
        dto.Details = record.Details.Select(d => new ClubMovementRecordDetailDto
        {
            Id = d.Id,
            ClubMovementRecordId = d.ClubMovementRecordId,
            CriterionId = d.CriterionId,
            CriterionTitle = d.Criterion.Title,
            GroupName = d.Criterion.Group.Name,
            ActivityId = d.ActivityId,
            ActivityTitle = d.Activity?.Title,
            Score = d.Score,
            ScoreType = d.ScoreType,
            Note = d.Note,
            CreatedBy = d.CreatedBy,
            CreatedByName = d.CreatedByUser?.FullName,
            AwardedAt = d.AwardedAt
        }).OrderByDescending(d => d.AwardedAt).ToList();

        return dto;
    }
}

