namespace BusinessObject.Models;

public class MovementRecordDetail
{
    public int Id { get; set; }
    
    public int MovementRecordId { get; set; }
    public MovementRecord MovementRecord { get; set; } = null!;
    
    public int CriterionId { get; set; }
    public MovementCriterion Criterion { get; set; } = null!;
    
    public double Score { get; set; } // điểm sinh viên đạt cho tiêu chí này
    
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
