using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models;

public class ActivityEvaluation
{
    public int Id { get; set; }
    
    // Foreign key to Activity
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    
    // Expected participants (A)
    [Range(0, 10000)]
    public int ExpectedParticipants { get; set; }
    
    // Actual participants (B)
    [Range(0, 10000)]
    public int ActualParticipants { get; set; }
    
    // Reason if B < A
    [MaxLength(1000)]
    public string? Reason { get; set; }
    
    // Communication score (scale 10)
    [Range(0, 10)]
    public int CommunicationScore { get; set; }
    
    // Organization score (scale 10)
    [Range(0, 10)]
    public int OrganizationScore { get; set; }
    
    // MC/Host score (scale 10)
    [Range(0, 10)]
    public int HostScore { get; set; }
    
    // Speaker/Performer score (scale 10)
    [Range(0, 10)]
    public int SpeakerScore { get; set; }
    
    // Success score (scale 10)
    [Range(0, 10)]
    public int Success { get; set; }
    
    // Limitations
    [MaxLength(2000)]
    public string? Limitations { get; set; }
    
    // Improvement measures
    [MaxLength(2000)]
    public string? ImprovementMeasures { get; set; }
    
    // Average score (automatically calculated)
    public double AverageScore { get; set; }
    
    // Creator of evaluation
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    
    // Creation and update timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
