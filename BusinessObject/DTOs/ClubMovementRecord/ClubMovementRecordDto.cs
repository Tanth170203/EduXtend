namespace BusinessObject.DTOs.ClubMovementRecord;

/// <summary>
/// DTO để hiển thị điểm phong trào của CLB
/// </summary>
public class ClubMovementRecordDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int Month { get; set; }
    
    // Scores by category
    public double ClubMeetingScore { get; set; }
    public double EventScore { get; set; }
    public double CompetitionScore { get; set; }
    public double PlanScore { get; set; }
    public double CollaborationScore { get; set; }
    
    public double TotalScore { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    
    // Additional info
    public string PresidentName { get; set; } = string.Empty;
    public string PresidentCode { get; set; } = string.Empty;
    public string PresidentEmail { get; set; } = string.Empty;
    
    public List<ClubMovementRecordDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO để hiển thị chi tiết từng lần chấm điểm
/// </summary>
public class ClubMovementRecordDetailDto
{
    public int Id { get; set; }
    public int ClubMovementRecordId { get; set; }
    public int CriterionId { get; set; }
    public string CriterionTitle { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int? ActivityId { get; set; }
    public string? ActivityTitle { get; set; }
    public double Score { get; set; }
    public string ScoreType { get; set; } = "Auto"; // Auto or Manual
    public string? Note { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime AwardedAt { get; set; }
}

/// <summary>
/// DTO để thêm điểm thủ công cho CLB
/// </summary>
public class AddClubManualScoreDto
{
    public int ClubId { get; set; }
    public int SemesterId { get; set; }
    public int Month { get; set; } // 9, 10, 11, 12
    public int CriterionId { get; set; }
    public double Score { get; set; }
    public string Note { get; set; } = string.Empty; // Bắt buộc với điểm manual
    public int? CreatedById { get; set; } // Admin who created this score (set by API)
}

/// <summary>
/// DTO để cập nhật điểm thủ công
/// </summary>
public class UpdateClubManualScoreDto
{
    public int DetailId { get; set; }
    public double Score { get; set; }
    public string Note { get; set; } = string.Empty;
}

/// <summary>
/// DTO để hiển thị tổng điểm theo category (dùng cho UI)
/// </summary>
public class ClubCategoryScoreDto
{
    public string CategoryName { get; set; } = string.Empty;
    public double CurrentScore { get; set; }
    public double MaxScore { get; set; }
    public bool IsMax => CurrentScore >= MaxScore;
}


