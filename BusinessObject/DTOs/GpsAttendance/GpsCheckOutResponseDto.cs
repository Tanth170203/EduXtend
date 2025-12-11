namespace BusinessObject.DTOs.GpsAttendance;

public class GpsCheckOutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double? DistanceMeters { get; set; }
    public double? AllowedRadius { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public GpsCoordinateDto? ActivityLocation { get; set; }
    public GpsCoordinateDto? UserLocation { get; set; }
}
