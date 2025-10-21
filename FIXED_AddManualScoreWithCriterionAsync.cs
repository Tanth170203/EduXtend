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

        // Check category max score
        var category = await _criterionGroupRepository.GetByIdAsync(dto.CategoryId);
        if (category == null)
            throw new KeyNotFoundException($"Category with ID {dto.CategoryId} not found");

        var categoryMax = category.MaxScore;
        var currentCategoryTotal = await GetCurrentCategoryTotalAsync(record!.Id, dto.CategoryId);

        // Auto-adjust score if it would exceed category max
        if (currentCategoryTotal + dto.Score > categoryMax)
        {
            var originalScore = dto.Score;
            dto.Score = Math.Max(0, categoryMax - currentCategoryTotal);
            Console.WriteLine($"[AddManualScoreWithCriterion] Category {dto.CategoryId} cap: {currentCategoryTotal} + {originalScore} > {categoryMax}, adjusted to {dto.Score}");
            
            if (dto.Score <= 0)
            {
                dto.Score = 0;
                Console.WriteLine($"[AddManualScoreWithCriterion] Category {dto.CategoryId} already at max {categoryMax}, adding 0 points");
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

        // Apply category-level caps and adjustments
        await CapAndAdjustScoresAsync(record.Id);

        // Reload to get updated score after capping
        var result = await _recordRepository.GetByIdWithDetailsAsync(record.Id);
        
        Console.WriteLine($"[AddManualScoreWithCriterion] After cap: Result.TotalScore = {result?.TotalScore}");
        
        var resultDto = MapToDto(result!);
        
        // Add message to indicate new creation (always create new for accumulation)
        resultDto.Message = "Created new detail for criterion (accumulated)";
        
        // Add info about score adjustment if it was capped
        var finalCategoryTotal = await GetCurrentCategoryTotalAsync(record.Id, dto.CategoryId);
        if (finalCategoryTotal >= categoryMax)
        {
            resultDto.Message += $" (Category {dto.CategoryId} capped at {categoryMax})";
        }
        
        return resultDto;
    }
