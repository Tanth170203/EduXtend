namespace BusinessObject.Models;

public class MovementRecordDetail
{
    public int Id { get; set; }
    
    public int MovementRecordId { get; set; }
    public MovementRecord MovementRecord { get; set; } = null!;
    
    public int CriterionId { get; set; }
    public MovementCriterion Criterion { get; set; } = null!;
    
    // Link về Activity (sự kiện/thi) để chống trùng tuyệt đối
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    
    // Nguồn điểm: Auto | Manual | Imported
    public string ScoreType { get; set; } = "Auto";
    
    // Người thêm điểm (manual/imported)
    public int? CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
    
    // Ghi chú nghiệp vụ (bắt buộc khi Manual)
    public string? Note { get; set; }
    
    public double Score { get; set; } // điểm sinh viên đạt cho tiêu chí này
    
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
