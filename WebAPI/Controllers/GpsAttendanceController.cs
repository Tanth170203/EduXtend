using BusinessObject.DTOs.GpsAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.GpsAttendance;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GpsAttendanceController : ControllerBase
{
    private readonly IGpsAttendanceService _gpsAttendanceService;

    public GpsAttendanceController(IGpsAttendanceService gpsAttendanceService)
    {
        _gpsAttendanceService = gpsAttendanceService;
    }

    /// <summary>
    /// GPS Check-in to an activity
    /// Check-in is allowed within the first 10 minutes after activity starts
    /// </summary>
    [HttpPost("checkin")]
    public async Task<ActionResult<GpsCheckInResponseDto>> CheckIn([FromBody] GpsCheckInRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _gpsAttendanceService.CheckInAsync(userId.Value, request);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// GPS Check-out from an activity
    /// Check-out is allowed within the last 10 minutes before activity ends
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<GpsCheckOutResponseDto>> CheckOut([FromBody] GpsCheckOutRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _gpsAttendanceService.CheckOutAsync(userId.Value, request);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }


    /// <summary>
    /// Get current attendance status for an activity
    /// </summary>
    [HttpGet("status/{activityId}")]
    public async Task<ActionResult<GpsAttendanceStatusDto>> GetStatus(int activityId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _gpsAttendanceService.GetAttendanceStatusAsync(userId.Value, activityId);
        
        if (result == null)
            return NotFound("Activity not found");

        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}
