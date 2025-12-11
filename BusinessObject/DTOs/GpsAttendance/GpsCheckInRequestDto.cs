using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.GpsAttendance;

public class GpsCheckInRequestDto
{
    [Required]
    public int ActivityId { get; set; }

    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
    public double Longitude { get; set; }

    /// <summary>
    /// GPS accuracy in meters (optional)
    /// </summary>
    public double? Accuracy { get; set; }
}
