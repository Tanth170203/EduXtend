using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.GpsAttendance;

public class ActivityGpsConfigDto
{
    public int ActivityId { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
    public double? Longitude { get; set; }

    [Range(50, 1000, ErrorMessage = "Radius must be between 50 and 1000 meters")]
    public int Radius { get; set; } = 300;

    public bool IsEnabled { get; set; }

    [Range(1, 60, ErrorMessage = "Check-in window must be between 1 and 60 minutes")]
    public int CheckInWindowMinutes { get; set; } = 10;

    [Range(1, 60, ErrorMessage = "Check-out window must be between 1 and 60 minutes")]
    public int CheckOutWindowMinutes { get; set; } = 10;
}
