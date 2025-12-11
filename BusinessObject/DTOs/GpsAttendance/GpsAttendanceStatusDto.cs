namespace BusinessObject.DTOs.GpsAttendance;

public class GpsAttendanceStatusDto
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public DateTime ActivityStartTime { get; set; }
    public DateTime ActivityEndTime { get; set; }
    
    // GPS Config
    public bool IsGpsCheckInEnabled { get; set; }
    public GpsCoordinateDto? ActivityLocation { get; set; }
    public int AllowedRadius { get; set; }
    
    // Check-in status
    public bool HasCheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
    public double? CheckInDistance { get; set; }
    
    // Check-out status
    public bool HasCheckedOut { get; set; }
    public DateTime? CheckOutTime { get; set; }
    
    // Time windows
    public bool IsCheckInWindowOpen { get; set; }
    public bool IsCheckOutWindowOpen { get; set; }
    public DateTime CheckInWindowStart { get; set; }
    public DateTime CheckInWindowEnd { get; set; }
    public DateTime CheckOutWindowStart { get; set; }
    public DateTime CheckOutWindowEnd { get; set; }
    
    // Remaining time
    public int? CheckInRemainingSeconds { get; set; }
    public int? CheckOutRemainingSeconds { get; set; }
}
