using BusinessObject.DTOs.MovementRecord;
using BusinessObject.Models;
using Repositories.MovementRecords;
using Repositories.Students;
using Repositories.Semesters;
using Repositories.MovementCriteria;
using Microsoft.AspNetCore.Http;

namespace Services.MovementRecords;

public class MovementRecordService : IMovementRecordService
{
    private readonly IMovementRecordRepository _recordRepository;
    private readonly IMovementRecordDetailRepository _detailRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMovementCriterionRepository _criterionRepository;
    private readonly IMovementCriterionGroupRepository _criterionGroupRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MovementRecordService(
        IMovementRecordRepository recordRepository,
        IMovementRecordDetailRepository detailRepository,
        IStudentRepository studentRepository,
        ISemesterRepository semesterRepository,
        IMovementCriterionRepository criterionRepository,
        IMovementCriterionGroupRepository criterionGroupRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _recordRepository = recordRepository;
        _detailRepository = detailRepository;
        _studentRepository = studentRepository;
        _semesterRepository = semesterRepository;
        _criterionRepository = criterionRepository;
        _criterionGroupRepository = criterionGroupRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<MovementRecordDto>> GetAllAsync()
    {
        var records = await _recordRepository.GetAllAsync();
        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementRecordDto>> GetByStudentIdAsync(int studentId)
    {
        var records = await _recordRepository.GetByStudentIdAsync(studentId);
        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<MovementRecordDto>> GetBySemesterIdAsync(int semesterId)
    {
        var records = await _recordRepository.GetBySemesterIdAsync(semesterId);
        return records.Select(MapToDto);
    }

    public async Task<MovementRecordDto?> GetByIdAsync(int id)
    {
        var record = await _recordRepository.GetByIdWithDetailsAsync(id);
        return record != null ? MapToDto(record) : null;
    }

    public async Task<MovementRecordDetailedDto?> GetDetailedByIdAsync(int id)
    {
        var record = await _recordRepository.GetByIdWithDetailsAsync(id);
        return record != null ? await MapToDetailedDtoAsync(record) : null;
    }

    public async Task<MovementRecordDto?> GetByStudentAndSemesterAsync(int studentId, int semesterId)
    {
        var record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, semesterId);
        return record != null ? MapToDto(record) : null;
    }

    public async Task<MovementRecordDto> CreateAsync(CreateMovementRecordDto dto)
    {
        // Validate student exists
        var studentExists = await _studentRepository.ExistsAsync(dto.StudentId);
        if (!studentExists)
            throw new KeyNotFoundException($"Student with ID {dto.StudentId} not found");

        // Validate semester exists
        var semesterExists = await _semesterRepository.ExistsAsync(dto.SemesterId);
        if (!semesterExists)
            throw new KeyNotFoundException($"Semester with ID {dto.SemesterId} not found");

        // Check if record already exists
        var exists = await _recordRepository.ExistsForStudentInSemesterAsync(dto.StudentId, dto.SemesterId);
        if (exists)
            throw new InvalidOperationException($"Movement record already exists for student {dto.StudentId} in semester {dto.SemesterId}");

        var record = new MovementRecord
        {
            StudentId = dto.StudentId,
            SemesterId = dto.SemesterId,
            TotalScore = 0,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _recordRepository.CreateAsync(record);
        var result = await _recordRepository.GetByIdWithDetailsAsync(created.Id);
        return MapToDto(result!);
    }

    public async Task<MovementRecordDto> AddScoreAsync(AddScoreDto dto)
    {
        // Validate record exists
        var record = await _recordRepository.GetByIdAsync(dto.MovementRecordId);
        if (record == null)
            throw new KeyNotFoundException($"Movement record with ID {dto.MovementRecordId} not found");

        // Validate criterion exists
        var criterion = await _criterionRepository.GetByIdAsync(dto.CriterionId);
        if (criterion == null)
            throw new KeyNotFoundException($"Criterion with ID {dto.CriterionId} not found");

        // Validate score doesn't exceed max
        if (dto.Score > criterion.MaxScore)
            throw new ArgumentException($"Score {dto.Score} exceeds maximum allowed {criterion.MaxScore} for this criterion");

        // Check if detail already exists for this criterion
        var detailExists = await _detailRepository.ExistsAsync(dto.MovementRecordId, dto.CriterionId);
        if (detailExists)
        {
            throw new InvalidOperationException($"Score already exists for criterion {dto.CriterionId}. Use update instead.");
        }

        // Create detail
        var detail = new MovementRecordDetail
        {
            MovementRecordId = dto.MovementRecordId,
            CriterionId = dto.CriterionId,
            Score = dto.Score,
            AwardedAt = DateTime.UtcNow
        };

        await _detailRepository.CreateAsync(detail);

        // Update total score
        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(dto.MovementRecordId);
        record.TotalScore = Math.Min(totalScore, 100); // Cap at 100 per Decision 414
        await _recordRepository.UpdateAsync(record);

        // Apply category-level caps and adjustments per Decision 414
        await CapAndAdjustScoresAsync(record.Id);

        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        return MapToDto(result!);
    }

    /// <summary>
    /// Cap and adjust scores according to Decision 414
    /// Category limits: 1=35, 2=50, 3=25, 4=30; Total=140
    /// </summary>
    public async Task CapAndAdjustScoresAsync(int recordId)
    {
        var record = await _recordRepository.GetByIdWithDetailsAsync(recordId);
        if (record == null)
            return;

        var details = record.Details.Where(d => d.Criterion != null).ToList();
        
        // Get all criterion groups (categories) with their max scores
        var allGroups = await _criterionGroupRepository.GetAllAsync();
        var categoryMap = allGroups.ToDictionary(g => g.Id, g => g.MaxScore);

        // Group details by category (GroupId)
        var detailsByCategory = details
            .Where(d => d.Criterion != null)
            .GroupBy(d => d.Criterion!.GroupId)
            .ToList();

        // Calculate total score with category caps applied (for display)
        // BUT do NOT modify individual detail scores
        double cappedTotal = 0;

        foreach (var categoryGroup in detailsByCategory)
        {
            var categoryId = categoryGroup.Key;
            if (!categoryMap.TryGetValue(categoryId, out var categoryMax))
            {
                // If no max defined, use actual total
                cappedTotal += categoryGroup.Sum(d => d.Score);
                continue;
            }

            var categoryTotal = categoryGroup.Sum(d => d.Score);
            
            // Apply cap to category total (for calculation only, not modifying details)
            var cappedCategoryTotal = Math.Min(categoryTotal, categoryMax);
            cappedTotal += cappedCategoryTotal;

            if (categoryTotal > categoryMax)
            {
                Console.WriteLine($"[CapAndAdjust] Category {categoryId} total {categoryTotal} > {categoryMax}, capped to {categoryMax} (details unchanged)");
            }
        }

        // Calculate uncapped total (sum of all details as-is)
        var uncappedTotal = details.Sum(d => d.Score);
        
        // Apply overall cap of 100
        var finalCappedTotal = Math.Min(cappedTotal, 100);
        
        Console.WriteLine($"[CapAndAdjust] RecordId={recordId}, UncappedTotal={uncappedTotal}, CappedTotal={finalCappedTotal}");
        
        // Update record.TotalScore with capped value (but keep details unchanged)
        if (record != null)
        {
            var roundedTotal = Math.Round(finalCappedTotal, 1);
            if (record.TotalScore != roundedTotal)
            {
                record.TotalScore = roundedTotal;
                record.LastUpdated = DateTime.UtcNow;
                await _recordRepository.UpdateAsync(record);
                Console.WriteLine($"[CapAndAdjust] Updated record.TotalScore to {roundedTotal} (details unchanged)");
            }
        }
    }

    public async Task<MovementRecordDto> AdjustScoreAsync(int id, AdjustScoreDto dto)
    {
        var record = await _recordRepository.GetByIdAsync(id);
        if (record == null)
            throw new KeyNotFoundException($"Movement record with ID {id} not found");

        record.TotalScore = dto.TotalScore;
        var updated = await _recordRepository.UpdateAsync(record);

        var result = await _recordRepository.GetByIdWithDetailsAsync(updated.Id);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var exists = await _recordRepository.ExistsAsync(id);
        if (!exists)
            throw new KeyNotFoundException($"Movement record with ID {id} not found");

        return await _recordRepository.DeleteAsync(id);
    }

    public async Task<StudentMovementSummaryDto?> GetStudentSummaryAsync(int studentId)
    {
        var records = await _recordRepository.GetByStudentIdAsync(studentId);
        var recordsList = records.ToList();

        if (!recordsList.Any())
            return null;

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            return null;

        return new StudentMovementSummaryDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            StudentCode = student.StudentCode,
            TotalSemesters = recordsList.Count,
            AverageScore = recordsList.Average(r => r.TotalScore),
            HighestScore = recordsList.Max(r => r.TotalScore),
            LowestScore = recordsList.Min(r => r.TotalScore),
            Records = recordsList.Select(MapToDto).ToList()
        };
    }

    public async Task<IEnumerable<MovementRecordDto>> GetTopScoresBySemesterAsync(int semesterId, int count)
    {
        var records = await _recordRepository.GetTopScoresBySemesterAsync(semesterId, count);
        return records.Select(MapToDto);
    }

    public async Task<MovementRecordDto> AddScoreFromEvidenceAsync(int studentId, int criterionId, double points)
    {
        // Get current semester
        var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
        if (currentSemester == null)
            throw new InvalidOperationException("No active semester found");

        // Get or create movement record
        var record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, currentSemester.Id);
        if (record == null)
        {
            var createDto = new CreateMovementRecordDto
            {
                StudentId = studentId,
                SemesterId = currentSemester.Id
            };
            await CreateAsync(createDto);
            record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, currentSemester.Id);
        }

        // Validate criterion exists
        var criterion = await _criterionRepository.GetByIdAsync(criterionId);
        if (criterion == null)
            throw new KeyNotFoundException($"Criterion with ID {criterionId} not found");

        // Validate score doesn't exceed max
        if (points > criterion.MaxScore)
            throw new ArgumentException($"Score {points} exceeds maximum allowed {criterion.MaxScore} for this criterion");

        // Create detail directly - allow multiple evidences for same criterion (accumulate points)
        var detail = new MovementRecordDetail
        {
            MovementRecordId = record!.Id,
            CriterionId = criterionId,
            Score = points,
            ScoreType = "Auto", // Evidence-based scoring is considered Auto
            AwardedAt = DateTime.UtcNow
        };

        await _detailRepository.CreateAsync(detail);

        // Update total score
        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
        record.TotalScore = Math.Min(totalScore, 100); // Cap at 100
        await _recordRepository.UpdateAsync(record);

        // Apply category-level caps and adjustments per Decision 414
        await CapAndAdjustScoresAsync(record.Id);

        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        return MapToDto(result!);
    }

    public async Task<MovementRecordDto> AddScoreFromAttendanceAsync(int studentId, int criterionId, double points, int activityId)
    {
        // Get current active semester
        var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
        if (currentSemester == null)
            throw new InvalidOperationException("No active semester found");

        // Validate student exists
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {studentId} not found");

        // Validate criterion
        var criterion = await _criterionRepository.GetByIdAsync(criterionId);
        if (criterion == null)
            throw new KeyNotFoundException($"Criterion with ID {criterionId} not found");

        // Get or create movement record
        var record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, currentSemester.Id);
        if (record == null)
        {
            var createDto = new CreateMovementRecordDto
            {
                StudentId = studentId,
                SemesterId = currentSemester.Id
            };
            await CreateAsync(createDto);
            record = await _recordRepository.GetByStudentAndSemesterAsync(studentId, currentSemester.Id);
        }

        if (record == null)
            throw new InvalidOperationException("Failed to create movement record");

        // Validate score doesn't exceed criterion max
        if (points > criterion.MaxScore)
            throw new ArgumentException($"Score {points} exceeds maximum allowed {criterion.MaxScore} for this criterion");

        // Kiểm tra đã điểm danh activity này chưa (chống trùng)
        var existingDetail = await _detailRepository.GetByRecordCriterionActivityAsync(
            record.Id, 
            criterionId,
            activityId
        );
        
        if (existingDetail != null)
        {
            // Đã có điểm cho activity này rồi → Cập nhật điểm thay vì tạo mới
            existingDetail.Score = points;
            existingDetail.AwardedAt = DateTime.UtcNow;
            await _detailRepository.UpdateAsync(existingDetail);
        }
        else
        {
            // Tạo detail mới
            var detail = new MovementRecordDetail
            {
                MovementRecordId = record.Id,
                CriterionId = criterionId,
                ActivityId = activityId, // Link để chống trùng
                Score = points,
                ScoreType = "Auto",
                AwardedAt = DateTime.UtcNow
            };

            await _detailRepository.CreateAsync(detail);
        }

        // Tính lại tổng điểm
        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
        record.TotalScore = Math.Min(totalScore, 100); // Cap tại 100
        record.LastUpdated = DateTime.UtcNow;
        await _recordRepository.UpdateAsync(record);

        // Áp dụng capping theo nhóm (Group 2 = 50 điểm max)
        await CapAndAdjustScoresAsync(record.Id);

        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        return MapToDto(result!);
    }

    public async Task<MovementRecordDto> AddManualScoreAsync(AddManualScoreDto dto)
    {
        // Get current active semester
        var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
        if (currentSemester == null)
            throw new InvalidOperationException("No active semester found");

        // Validate student exists
        var student = await _studentRepository.GetByIdAsync(dto.StudentId);
        if (student == null)
            throw new KeyNotFoundException($"Student with ID {dto.StudentId} not found");

        // Get or create movement record for student in current semester
        var record = await _recordRepository.GetByStudentAndSemesterAsync(dto.StudentId, currentSemester.Id);
        if (record == null)
        {
            var createDto = new CreateMovementRecordDto
            {
                StudentId = dto.StudentId,
                SemesterId = currentSemester.Id
            };
            await CreateAsync(createDto);
            record = await _recordRepository.GetByStudentAndSemesterAsync(dto.StudentId, currentSemester.Id);
        }

        // Create or get a specific criterion for this behavior
        // Each behavior should have its own criterion to allow accumulation
        var criterionTitle = GetCriterionTitleForBehavior(dto.CategoryId, dto.Score);
        var criterion = await GetOrCreateCriterionForBehavior(dto.CategoryId, criterionTitle, dto.Score);

        // Validate score doesn't exceed category max based on Decision 414
        var categoryMaxScores = new Dictionary<int, int> { { 1, 35 }, { 2, 50 }, { 3, 25 }, { 4, 30 } };
        var categoryMax = categoryMaxScores.GetValueOrDefault(dto.CategoryId, 100);
        
        // Check current total for this category
        var currentCategoryTotal = await GetCurrentCategoryTotalAsync(record.Id, dto.CategoryId);
        if (currentCategoryTotal + dto.Score > categoryMax)
        {
            // Auto-adjust to category max (don't throw error, just cap the score)
            var originalScore = dto.Score;
            dto.Score = Math.Max(0, categoryMax - currentCategoryTotal);
            Console.WriteLine($"[AddManualScore] Category {dto.CategoryId} cap: {currentCategoryTotal} + {originalScore} > {categoryMax}, adjusted to {dto.Score}");
            
            if (dto.Score <= 0)
            {
                // If no room left, still add 0 but don't throw error
                dto.Score = 0;
                Console.WriteLine($"[AddManualScore] Category {dto.CategoryId} already at max {categoryMax}, adding 0 points");
            }
        }

        // Check if detail already exists for this specific criterion (same behavior)
        var existingDetail = await _detailRepository.GetByRecordAndCriterionAsync(record!.Id, criterion.Id);
        bool wasUpdate = false;
        
        if (existingDetail != null)
        {
            // Update existing detail (same behavior, different score)
            existingDetail.Score = dto.Score;
            existingDetail.AwardedAt = dto.AwardedDate ?? DateTime.UtcNow;
            await _detailRepository.UpdateAsync(existingDetail);
            wasUpdate = true;
            Console.WriteLine($"[AddManualScore] Updated existing detail for criterion {criterion.Id} (same behavior)");
        }
        else
        {
            // Create new detail (new behavior) - ACCUMULATE
            var detail = new MovementRecordDetail
            {
                MovementRecordId = record.Id,
                CriterionId = criterion.Id,
                Score = dto.Score,
                AwardedAt = dto.AwardedDate ?? DateTime.UtcNow
            };

            await _detailRepository.CreateAsync(detail);
            wasUpdate = false;
            Console.WriteLine($"[AddManualScore] Created new detail for criterion {criterion.Id} (new behavior - ACCUMULATE)");
        }

        // Update total score
        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
        record.TotalScore = Math.Min(totalScore, 100); // Cap at 100
        record.LastUpdated = DateTime.UtcNow;
        
        // Log for debugging
        Console.WriteLine($"[AddManualScore] Before cap: TotalScore = {totalScore}, Record.TotalScore = {record.TotalScore}");
        
        await _recordRepository.UpdateAsync(record);

        // Apply category-level caps and adjustments
        await CapAndAdjustScoresAsync(record.Id);

        // Reload to get updated score after capping
        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        
        Console.WriteLine($"[AddManualScore] After cap: Result.TotalScore = {result?.TotalScore}");
        
        var resultDto = MapToDto(result!);
        
        // Add message to indicate if this was an update or new creation
        if (wasUpdate)
        {
            resultDto.Message = "Updated existing detail for criterion";
        }
        else
        {
            resultDto.Message = "Created new detail for criterion";
        }
        
        // Add info about score adjustment if it was capped
        var finalCategoryTotal = await GetCurrentCategoryTotalAsync(record.Id, dto.CategoryId);
        if (finalCategoryTotal >= categoryMax)
        {
            resultDto.Message += $" (Category {dto.CategoryId} capped at {categoryMax})";
        }
        
        return resultDto;
    }

    // Helper methods for behavior-based scoring
    private static string GetCriterionTitleForBehavior(int categoryId, double score)
    {
        // Create unique titles with timestamp to ensure different behaviors get different criteria
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        
        return categoryId switch
        {
            1 => score switch
            {
                2.0 => $"Tuyên dương công khai (2 điểm) - {timestamp}",
                10.0 => $"Olympic/ACM/CPC/Robocon (10 điểm) - {timestamp}",
                5.0 => $"Cuộc thi cấp Trường (5 điểm) - {timestamp}",
                _ => $"Hành vi học tập khác ({score} điểm) - {timestamp}"
            },
            2 => score switch
            {
                3.0 => $"Sự kiện nhỏ (3 điểm) - {timestamp}",
                5.0 => $"Sự kiện trung (5 điểm) - {timestamp}",
                8.0 => $"CLB sinh hoạt (8 điểm) - {timestamp}",
                15.0 => $"Sự kiện vừa (50-100 sv) (15 điểm) - {timestamp}",
                20.0 => $"Sự kiện lớn (100-200 sv) (20 điểm) - {timestamp}",
                _ => $"Hoạt động chính trị khác ({score} điểm) - {timestamp}"
            },
            3 => score switch
            {
                5.0 => $"Hành vi tốt/Tình nguyện (5 điểm) - {timestamp}",
                _ => $"Phẩm chất công dân khác ({score} điểm) - {timestamp}"
            },
            4 => score switch
            {
                8.0 => $"Vai trò BTC (8 điểm) - {timestamp}",
                10.0 => $"Lớp trưởng/BCH Đoàn (10 điểm) - {timestamp}",
                _ => $"Công tác phụ trách khác ({score} điểm) - {timestamp}"
            },
            _ => $"Hành vi danh mục {categoryId} ({score} điểm) - {timestamp}"
        };
    }

    private async Task<MovementCriterion> GetOrCreateCriterionForBehavior(int categoryId, string title, double maxScore)
    {
        // Always create new criterion for each behavior to ensure accumulation
        // This ensures different behaviors get different criteria and can accumulate
        var newCriterion = new MovementCriterion
        {
            Title = title,
            MaxScore = (int)maxScore,
            GroupId = categoryId,
            IsActive = true
        };

        return await _criterionRepository.CreateAsync(newCriterion);
    }

    private async Task<double> GetCurrentCategoryTotalAsync(int recordId, int categoryId)
    {
        var details = await _detailRepository.GetByRecordIdAsync(recordId);
        var categoryDetails = details.Where(d => d.Criterion?.GroupId == categoryId);
        return categoryDetails.Sum(d => d.Score);
    }

    private async Task<double> GetCurrentCriterionTotalAsync(int recordId, int criterionId)
    {
        var details = await _detailRepository.GetByRecordIdAsync(recordId);
        var criterionDetails = details.Where(d => d.CriterionId == criterionId);
        return criterionDetails.Sum(d => d.Score);
    }

    // Helper methods for mapping
    private static MovementRecordDto MapToDto(MovementRecord record)
    {
        return new MovementRecordDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = record.Student?.FullName,
            StudentCode = record.Student?.StudentCode,
            SemesterId = record.SemesterId,
            SemesterName = record.Semester?.Name,
            TotalScore = record.TotalScore,
            CreatedAt = record.CreatedAt,
            LastUpdated = record.LastUpdated,
            DetailCount = record.Details?.Count ?? 0
        };
    }

    private async Task<MovementRecordDetailedDto> MapToDetailedDtoAsync(MovementRecord record)
    {
        var details = record.Details?.Select(d => new MovementRecordDetailItemDto
        {
            Id = d.Id,
            CriterionId = d.CriterionId,
            CriterionTitle = d.Criterion?.Title,
            GroupName = d.Criterion?.Group?.Name,
            CriterionMaxScore = d.Criterion?.MaxScore ?? 0,
            Score = d.Score,
            AwardedAt = d.AwardedAt,
            ScoreType = string.IsNullOrWhiteSpace(d.ScoreType) ? "Auto" : d.ScoreType,
            Note = d.Note,
            ActivityId = d.ActivityId
        }).ToList() ?? new List<MovementRecordDetailItemDto>();

        // Calculate category scores with caps
        var categoryScores = new List<CategoryScoreDto>();
        var allGroups = await _criterionGroupRepository.GetAllAsync();
        var categoryMap = allGroups.ToDictionary(g => g.Id, g => new { g.Name, g.MaxScore });

        // Get GroupId from actual record details
        var detailsByGroup = details
            .Where(d => !string.IsNullOrEmpty(d.GroupName))
            .GroupBy(d => {
                // Find the corresponding detail in record to get GroupId
                var recordDetail = record.Details?.FirstOrDefault(d2 => d2.Id == d.Id);
                var groupId = recordDetail?.Criterion?.GroupId ?? 0;
                return new { 
                    GroupName = d.GroupName!, 
                    GroupId = groupId
                };
            })
            .ToList();

        foreach (var group in detailsByGroup)
        {
            var actualScore = group.Sum(d => d.Score);
            var groupId = group.Key.GroupId;
            
            if (categoryMap.TryGetValue(groupId, out var groupInfo))
            {
                var cappedScore = Math.Min(actualScore, groupInfo.MaxScore);
                categoryScores.Add(new CategoryScoreDto
                {
                    GroupName = group.Key.GroupName,
                    GroupId = groupId,
                    ActualScore = actualScore,
                    CappedScore = cappedScore,
                    MaxScore = groupInfo.MaxScore
                });
            }
            else
            {
                // If group not found in map, use actual score
                categoryScores.Add(new CategoryScoreDto
                {
                    GroupName = group.Key.GroupName,
                    GroupId = groupId,
                    ActualScore = actualScore,
                    CappedScore = actualScore,
                    MaxScore = 0
                });
            }
        }

        return new MovementRecordDetailedDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = record.Student?.FullName,
            StudentCode = record.Student?.StudentCode,
            SemesterId = record.SemesterId,
            SemesterName = record.Semester?.Name,
            TotalScore = record.TotalScore,
            CreatedAt = record.CreatedAt,
            LastUpdated = record.LastUpdated,
            Details = details,
            CategoryScores = categoryScores.OrderByDescending(c => c.CappedScore).ToList()
        };
    }

    /// <summary>
    /// Add manual score with specific criterion (Admin only)
    /// FIXED: Cộng dồn nhiều lần theo quy định
    /// </summary>
    public async Task<MovementRecordDto> AddManualScoreWithCriterionAsync(AddManualScoreWithCriterionDto dto)
    {
        // Get current active semester
        var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
        if (currentSemester == null)
            throw new InvalidOperationException("No active semester found");

        // Get or create movement record
        var record = await _recordRepository.GetByStudentAndSemesterAsync(dto.StudentId, currentSemester.Id);
        if (record == null)
        {
            var createDto = new CreateMovementRecordDto
            {
                StudentId = dto.StudentId,
                SemesterId = currentSemester.Id
            };
            await CreateAsync(createDto);
            record = await _recordRepository.GetByStudentAndSemesterAsync(dto.StudentId, currentSemester.Id);
        }

        // Validate criterion exists and belongs to the category
        var criterion = await _criterionRepository.GetByIdAsync(dto.CriterionId);
        if (criterion == null)
            throw new KeyNotFoundException($"Criterion with ID {dto.CriterionId} not found");

        if (criterion.GroupId != dto.CategoryId)
            throw new ArgumentException($"Criterion {dto.CriterionId} does not belong to category {dto.CategoryId}");

        var criterionMax = criterion.MaxScore; // Giới hạn của tiêu chí cụ thể
        var currentCriterionTotal = await GetCurrentCriterionTotalAsync(record!.Id, dto.CriterionId);

        if (dto.Score > criterionMax)
        {
            throw new ArgumentException($"Điểm nhập ({dto.Score}) vượt quá giới hạn tiêu chí ({criterionMax} điểm). Vui lòng nhập lại.");
        }

        // Check category max for logging/notification, but allow exceeding (will be auto-adjusted)
        var category = await _criterionGroupRepository.GetByIdAsync(dto.CategoryId);
        if (category != null)
        {
            var categoryMax = category.MaxScore;
            var currentCategoryTotal = await GetCurrentCategoryTotalAsync(record!.Id, dto.CategoryId);
            var newCategoryTotal = currentCategoryTotal + dto.Score;
            if (newCategoryTotal > categoryMax)
            {
                Console.WriteLine($"[AddManualScoreWithCriterion] Warning: Category {dto.CategoryId} will exceed max ({categoryMax}). Current: {currentCategoryTotal}, Adding: {dto.Score}, New Total: {newCategoryTotal}. Will auto-adjust after adding.");
            }
        }

        // Resolve CreatedBy from HttpContext if available
        int? createdBy = dto.CreatedById;
        if (createdBy == null)
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var uid)) createdBy = uid;
            }
            catch { /* ignore */ }
        }

        // Dedupe by (record, criterion, activityId) if ActivityId provided
        if (dto.ActivityId.HasValue)
        {
            var existing = await _detailRepository.GetByRecordCriterionActivityAsync(record.Id, dto.CriterionId, dto.ActivityId.Value);
            if (existing != null)
            {
                existing.Score = dto.Score;
                existing.AwardedAt = dto.AwardedDate ?? DateTime.UtcNow;
                existing.ScoreType = "Manual";
                existing.CreatedBy = createdBy;
                existing.Note = dto.Comments;
                await _detailRepository.UpdateAsync(existing);

                var totalExisting = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
                record.TotalScore = Math.Min(totalExisting, 100);
                record.LastUpdated = DateTime.UtcNow;
                await _recordRepository.UpdateAsync(record);

                var res1 = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
                var dtoRes1 = MapToDto(res1!);
                dtoRes1.Message = "Updated existing detail by ActivityId";
                return dtoRes1;
            }
        }

        var detail = new MovementRecordDetail
        {
            MovementRecordId = record.Id,
            CriterionId = dto.CriterionId,
            Score = dto.Score,
            AwardedAt = dto.AwardedDate ?? DateTime.UtcNow,
            ScoreType = "Manual",
            CreatedBy = createdBy,
            Note = dto.Comments,
            ActivityId = dto.ActivityId
        };

        await _detailRepository.CreateAsync(detail);
        Console.WriteLine($"[AddManualScoreWithCriterion] Created new detail for criterion {dto.CriterionId} - Score: {dto.Score}");

        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
        record.TotalScore = Math.Min(totalScore, 100); // Cap at 100
        record.LastUpdated = DateTime.UtcNow;
        
        await _recordRepository.UpdateAsync(record);

        // Apply category-level caps and adjustments per Decision 414
        // This will automatically adjust scores if category totals exceed limits
        await CapAndAdjustScoresAsync(record.Id);

        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        var resultDto = MapToDto(result!);
        
        // Check if adjustment was made and add message
        var finalCategoryTotal = await GetCurrentCategoryTotalAsync(record.Id, dto.CategoryId);
        if (category != null && finalCategoryTotal >= category.MaxScore)
        {
            resultDto.Message = $"Đã cộng điểm. Nhóm đã đạt giới hạn ({category.MaxScore} điểm) và đã được tự động điều chỉnh.";
        }
        else
        {
            resultDto.Message = "Đã cộng điểm thành công";
        }
        
        return resultDto;
    }
}



