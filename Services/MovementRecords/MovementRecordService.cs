using BusinessObject.DTOs.MovementRecord;
using BusinessObject.Models;
using Repositories.MovementRecords;
using Repositories.Students;
using Repositories.Semesters;
using Repositories.MovementCriteria;

namespace Services.MovementRecords;

public class MovementRecordService : IMovementRecordService
{
    private readonly IMovementRecordRepository _recordRepository;
    private readonly IMovementRecordDetailRepository _detailRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IMovementCriterionRepository _criterionRepository;
    private readonly IMovementCriterionGroupRepository _criterionGroupRepository;

    public MovementRecordService(
        IMovementRecordRepository recordRepository,
        IMovementRecordDetailRepository detailRepository,
        IStudentRepository studentRepository,
        ISemesterRepository semesterRepository,
        IMovementCriterionRepository criterionRepository,
        IMovementCriterionGroupRepository criterionGroupRepository)
    {
        _recordRepository = recordRepository;
        _detailRepository = detailRepository;
        _studentRepository = studentRepository;
        _semesterRepository = semesterRepository;
        _criterionRepository = criterionRepository;
        _criterionGroupRepository = criterionGroupRepository;
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
        return record != null ? MapToDetailedDto(record) : null;
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
        record.TotalScore = Math.Min(totalScore, 140); // Cap at 140 as per regulations
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

        var details = record.Details.ToList();
        var adjustments = new Dictionary<int, (double oldScore, double newScore, string reason)>();

        // Get all criteria grouped by category
        var allCriteria = await _criterionRepository.GetAllAsync();
        var criteriaByCategory = new Dictionary<string, List<int>>();
        
        // Simple category mapping (assuming criterion titles contain category info)
        var category1Criteria = allCriteria.Where(c => c.Title.Contains("tuyên dương") || c.Title.Contains("Olympic") || c.Title.Contains("thi") || c.Title.Contains("ACM") || c.Title.Contains("CPC")).Select(c => c.Id).ToList();
        var category2Criteria = allCriteria.Where(c => c.Title.Contains("sự kiện") || c.Title.Contains("CLB") || c.Title.Contains("hoạt động")).Select(c => c.Id).ToList();
        var category3Criteria = allCriteria.Where(c => c.Title.Contains("hành vi") || c.Title.Contains("từ thiện") || c.Title.Contains("tình nguyện")).Select(c => c.Id).ToList();
        var category4Criteria = allCriteria.Where(c => c.Title.Contains("lớp trưởng") || c.Title.Contains("BCH") || c.Title.Contains("công tác")).Select(c => c.Id).ToList();

        // Calculate per-category totals
        var cat1Total = details.Where(d => category1Criteria.Contains(d.CriterionId)).Sum(d => d.Score);
        var cat2Total = details.Where(d => category2Criteria.Contains(d.CriterionId)).Sum(d => d.Score);
        var cat3Total = details.Where(d => category3Criteria.Contains(d.CriterionId)).Sum(d => d.Score);
        var cat4Total = details.Where(d => category4Criteria.Contains(d.CriterionId)).Sum(d => d.Score);

        // Apply category caps - CAP TO MAX instead of scaling down
        // If category exceeds max, set to max (not scale down)
        if (cat1Total > 35)
        {
            Console.WriteLine($"[CapAndAdjust] Category 1 total {cat1Total} > 35, capping to 35");
            // Find the highest scoring detail in category 1 and cap it
            var cat1Details = details.Where(d => category1Criteria.Contains(d.CriterionId)).OrderByDescending(d => d.Score).ToList();
            if (cat1Details.Any())
            {
                var excess = cat1Total - 35;
                var highestDetail = cat1Details.First();
                highestDetail.Score = Math.Max(0, highestDetail.Score - excess);
                Console.WriteLine($"[CapAndAdjust] Reduced highest detail from {highestDetail.Score + excess} to {highestDetail.Score}");
            }
        }
        
        if (cat2Total > 50)
        {
            Console.WriteLine($"[CapAndAdjust] Category 2 total {cat2Total} > 50, capping to 50");
            var cat2Details = details.Where(d => category2Criteria.Contains(d.CriterionId)).OrderByDescending(d => d.Score).ToList();
            if (cat2Details.Any())
            {
                var excess = cat2Total - 50;
                var highestDetail = cat2Details.First();
                highestDetail.Score = Math.Max(0, highestDetail.Score - excess);
                Console.WriteLine($"[CapAndAdjust] Reduced highest detail from {highestDetail.Score + excess} to {highestDetail.Score}");
            }
        }
        
        if (cat3Total > 25)
        {
            Console.WriteLine($"[CapAndAdjust] Category 3 total {cat3Total} > 25, capping to 25");
            var cat3Details = details.Where(d => category3Criteria.Contains(d.CriterionId)).OrderByDescending(d => d.Score).ToList();
            if (cat3Details.Any())
            {
                var excess = cat3Total - 25;
                var highestDetail = cat3Details.First();
                highestDetail.Score = Math.Max(0, highestDetail.Score - excess);
                Console.WriteLine($"[CapAndAdjust] Reduced highest detail from {highestDetail.Score + excess} to {highestDetail.Score}");
            }
        }
        
        if (cat4Total > 30)
        {
            Console.WriteLine($"[CapAndAdjust] Category 4 total {cat4Total} > 30, capping to 30");
            var cat4Details = details.Where(d => category4Criteria.Contains(d.CriterionId)).OrderByDescending(d => d.Score).ToList();
            if (cat4Details.Any())
            {
                var excess = cat4Total - 30;
                var highestDetail = cat4Details.First();
                highestDetail.Score = Math.Max(0, highestDetail.Score - excess);
                Console.WriteLine($"[CapAndAdjust] Reduced highest detail from {highestDetail.Score + excess} to {highestDetail.Score}");
            }
        }

        // Update details with adjusted scores (already done above)
        // No need for additional scaling since we already capped to max

        // Calculate new total
        var newTotal = details.Sum(d => d.Score);
        
        Console.WriteLine($"[CapAndAdjust] RecordId={recordId}, NewTotal={newTotal}");
        
        // Apply total cap
        bool needsDetailUpdate = false;
        
        if (newTotal > 140)
        {
            double totalScaleFactor = 140.0 / newTotal;
            foreach (var detail in details)
            {
                detail.Score = Math.Round(detail.Score * totalScaleFactor, 1);
                await _detailRepository.UpdateAsync(detail);
            }
            record.TotalScore = 140;
            needsDetailUpdate = true;
            Console.WriteLine($"[CapAndAdjust] Total > 140, capped to 140, scaled all details");
        }
        else if (newTotal < 60)
        {
            // For manual scoring, we should still keep the score even if < 60
            // The < 60 rule may be for final evaluation, not for tracking
            record.TotalScore = Math.Round(newTotal, 1);
            Console.WriteLine($"[CapAndAdjust] Total < 60, keeping actual score: {record.TotalScore}");
        }
        else
        {
            record.TotalScore = Math.Round(newTotal, 1);
            Console.WriteLine($"[CapAndAdjust] Total in range [60-140]: {record.TotalScore}");
        }

        // Update details if any adjustments were made
        if (adjustments.Any() && !needsDetailUpdate)
        {
            foreach (var detail in details)
            {
                if (adjustments.ContainsKey(detail.Id))
                {
                    await _detailRepository.UpdateAsync(detail);
                }
            }
        }

        record.LastUpdated = DateTime.UtcNow;
        await _recordRepository.UpdateAsync(record);
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
        // Get current semester (you may need to adjust this logic)
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

        // Add score
        var addScoreDto = new AddScoreDto
        {
            MovementRecordId = record!.Id,
            CriterionId = criterionId,
            Score = points
        };

        return await AddScoreAsync(addScoreDto);
    }

    public async Task<MovementRecordDto> AddScoreFromAttendanceAsync(int studentId, int criterionId, double points)
    {
        // Similar to AddScoreFromEvidenceAsync
        return await AddScoreFromEvidenceAsync(studentId, criterionId, points);
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
        record.TotalScore = Math.Min(totalScore, 140); // Cap at 140
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

    private static MovementRecordDetailedDto MapToDetailedDto(MovementRecord record)
    {
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
            Details = record.Details?.Select(d => new MovementRecordDetailItemDto
            {
                Id = d.Id,
                CriterionId = d.CriterionId,
                CriterionTitle = d.Criterion?.Title,
                GroupName = d.Criterion?.Group?.Name,
                CriterionMaxScore = d.Criterion?.MaxScore ?? 0,
                Score = d.Score,
                AwardedAt = d.AwardedAt
            }).ToList() ?? new List<MovementRecordDetailItemDto>()
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

        // FIXED: Validate theo Criterion (tiêu chí con) thay vì Category (nhóm)
        // Theo quy định: mỗi tiêu chí có giới hạn riêng
        // Ví dụ: Olympic 10 điểm/lần → có thể cộng nhiều lần, mỗi lần tối đa 10 điểm
        // Ví dụ: Tuyên dương 2 điểm/lần → có thể cộng nhiều lần, mỗi lần tối đa 2 điểm
        
        var criterionMax = criterion.MaxScore; // Giới hạn của tiêu chí cụ thể
        var currentCriterionTotal = await GetCurrentCriterionTotalAsync(record!.Id, dto.CriterionId);

        // FIXED: Validate và báo lỗi nếu vượt quá giới hạn
        // 1. Điểm tiêu chí con: Báo lỗi nếu vượt quá MaxScore
        // 2. Điểm nhóm: Báo lỗi nếu vượt quá MaxScore
        
        Console.WriteLine($"[AddManualScoreWithCriterion] Criterion {dto.CriterionId}: Current total = {currentCriterionTotal}, Adding = {dto.Score}, Criterion max = {criterionMax}");
        
        // 1. VALIDATE: Điểm tiêu chí con không được vượt quá MaxScore
        if (dto.Score > criterionMax)
        {
            throw new ArgumentException($"Điểm nhập ({dto.Score}) vượt quá giới hạn tiêu chí ({criterionMax} điểm). Vui lòng nhập lại.");
        }
        
        // 2. VALIDATE: Kiểm tra điểm nhóm
        var category = await _criterionGroupRepository.GetByIdAsync(dto.CategoryId);
        if (category != null)
        {
            var categoryMax = category.MaxScore;
            var currentCategoryTotal = await GetCurrentCategoryTotalAsync(record!.Id, dto.CategoryId);
            var newCategoryTotal = currentCategoryTotal + dto.Score;
            
            Console.WriteLine($"[AddManualScoreWithCriterion] Category {dto.CategoryId}: Current = {currentCategoryTotal}, Adding = {dto.Score}, New total = {newCategoryTotal}, Category max = {categoryMax}");
            
            if (newCategoryTotal > categoryMax)
            {
                throw new ArgumentException($"Điểm nhập sẽ làm tổng nhóm vượt quá giới hạn ({categoryMax} điểm). Hiện tại: {currentCategoryTotal} điểm, nhập thêm: {dto.Score} điểm = {newCategoryTotal} điểm > {categoryMax} điểm. Vui lòng nhập lại.");
            }
        }

        // ALWAYS CREATE NEW DETAIL - Cộng dồn nhiều lần theo quy định
        // Theo quy định: có thể cộng nhiều lần cho cùng loại tiêu chí
        // Ví dụ: Tuyên dương 2 lần = 2 x 2 = 4 điểm
        // Ví dụ: Tham gia Olympic 2 lần = 2 x 10 = 20 điểm
        // Ví dụ: Tham gia Robocon 2 lần = 2 x 10 = 20 điểm
        
        var detail = new MovementRecordDetail
        {
            MovementRecordId = record.Id,
            CriterionId = dto.CriterionId,
            Score = dto.Score,
            AwardedAt = dto.AwardedDate ?? DateTime.UtcNow
        };

        await _detailRepository.CreateAsync(detail);
        Console.WriteLine($"[AddManualScoreWithCriterion] Created new detail for criterion {dto.CriterionId} - Score: {dto.Score}");

        // Update total score
        var totalScore = await _detailRepository.GetTotalScoreByRecordIdAsync(record.Id);
        record.TotalScore = Math.Min(totalScore, 140); // Cap at 140
        record.LastUpdated = DateTime.UtcNow;
        
        Console.WriteLine($"[AddManualScoreWithCriterion] Before cap: TotalScore = {totalScore}, Record.TotalScore = {record.TotalScore}");
        
        await _recordRepository.UpdateAsync(record);

        // DISABLED: Không áp dụng category-level caps để cho phép cộng dồn thực sự
        // await CapAndAdjustScoresAsync(record.Id);
        Console.WriteLine($"[AddManualScoreWithCriterion] Skipping category caps to allow real accumulation");

        // Reload to get updated score (no capping applied)
        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        
        Console.WriteLine($"[AddManualScoreWithCriterion] After update: Result.TotalScore = {result?.TotalScore}");
        
        var resultDto = MapToDto(result!);
        
        // Add message to indicate new creation (always create new for accumulation)
        resultDto.Message = "Created new detail for criterion (accumulated)";
        
        // Add info about score adjustments
        var finalCriterionTotal = await GetCurrentCriterionTotalAsync(record.Id, dto.CriterionId);
        var finalCategoryTotal = await GetCurrentCategoryTotalAsync(record.Id, dto.CategoryId);
        
        resultDto.Message += $" (Criterion total: {finalCriterionTotal}, Category total: {finalCategoryTotal})";
        
        // Add specific adjustment messages if score was adjusted
        var originalScore = dto.Score; // This would need to be tracked from the original input
        // For now, just show the final totals
        
        return resultDto;
    }
}


