namespace BusinessObject.DTOs.Notification;

/// <summary>
/// DTO for notification response
/// Requirements: 7.1, 7.2, 7.3, 7.4
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string? Message { get; set; }
    
    public string Type { get; set; } = "info"; // ReportSubmitted, ReportApproved, ReportRejected
    
    public string Scope { get; set; } = "User"; // Club, System, User
    
    public int? ReportId { get; set; }
    
    public bool IsRead { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
