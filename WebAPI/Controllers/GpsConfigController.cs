using BusinessObject.DTOs.GpsAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.GpsAttendance;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GpsConfigController : ControllerBase
{
    private readonly IGpsAttendanceService _gpsAttendanceService;

    public GpsConfigController(IGpsAttendanceService gpsAttendanceService)
    {
        _gpsAttendanceService = gpsAttendanceService;
    }

    /// <summary>
    /// Configure GPS settings for an activity (Club Manager only)
    /// </summary>
    [HttpPost("{activityId}")]
    [Authorize(Roles = "ClubManager,Admin")]
    public async Task<ActionResult> ConfigureGps(int activityId, [FromBody] ActivityGpsConfigDto config)
    {
        config.ActivityId = activityId;

        // Validate coordinates if enabling GPS
        if (config.IsEnabled)
        {
            if (!config.Latitude.HasValue || !config.Longitude.HasValue)
                return BadRequest("Please enter GPS coordinates when enabling GPS attendance");

            if (!_gpsAttendanceService.ValidateCoordinates(config.Latitude.Value, config.Longitude.Value))
                return BadRequest("Invalid GPS coordinates. Latitude: -90 to 90, Longitude: -180 to 180");

            if (!_gpsAttendanceService.ValidateRadius(config.Radius))
                return BadRequest("Radius must be between 50 and 1000 meters");
        }

        var result = await _gpsAttendanceService.ConfigureActivityGpsAsync(config);
        
        if (!result)
            return NotFound("Activity not found");

        return Ok(new { message = "GPS configuration saved successfully" });
    }

    /// <summary>
    /// Get GPS configuration for an activity
    /// </summary>
    [HttpGet("{activityId}")]
    public async Task<ActionResult<ActivityGpsConfigDto>> GetConfig(int activityId)
    {
        var result = await _gpsAttendanceService.GetGpsConfigAsync(activityId);
        
        if (result == null)
            return NotFound("Activity not found");

        return Ok(result);
    }
}
