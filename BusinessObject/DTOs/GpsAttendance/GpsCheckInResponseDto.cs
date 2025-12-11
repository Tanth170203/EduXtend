namespace BusinessObject.DTOs.GpsAttendance;

public class GpsCheckInResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double? DistanceMeters { get; set; }
    public double? AllowedRadius { get; set; }
    public DateTime? CheckedAt { get; set; }
    public GpsCoordinateDto? ActivityLocation { get; set; }
    public GpsCoordinateDto? UserLocation { get; set; }
}

public class GpsCoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
