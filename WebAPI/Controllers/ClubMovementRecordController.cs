using BusinessObject.DTOs.ClubMovementRecord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.ClubMovementRecords;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/club-movement-records")]
public class ClubMovementRecordController : ControllerBase
{
    private readonly IClubScoringService _service;
    private readonly ILogger<ClubMovementRecordController> _logger;

    public ClubMovementRecordController(
        IClubScoringService service, 
        ILogger<ClubMovementRecordController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get club movement record by club, semester, and month
    /// </summary>
    [HttpGet("{clubId}/{semesterId}/{month}")]
    [Authorize]
    public async Task<ActionResult<ClubMovementRecordDto>> GetByClubMonth(
        int clubId, int semesterId, int month)
    {
        try
        {
            var record = await _service.GetClubScoreAsync(clubId, semesterId, month);
            if (record == null)
                return NotFound(new { message = $"Club movement record not found for club {clubId}, semester {semesterId}, month {month}." });

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving club movement record");
            return StatusCode(500, new { message = "Error retrieving club movement record." });
        }
    }

    /// <summary>
    /// Get all club movement records for a semester and month
    /// </summary>
    [HttpGet("semester/{semesterId}/month/{month}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ClubMovementRecordDto>>> GetAllByMonth(
        int semesterId, int month)
    {
        try
        {
            var records = await _service.GetAllClubScoresAsync(semesterId, month);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving club movement records");
            return StatusCode(500, new { message = "Error retrieving club movement record list." });
        }
    }

    /// <summary>
    /// Get all club movement records for a club
    /// </summary>
    [HttpGet("club/{clubId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ClubMovementRecordDto>>> GetAllByClub(int clubId)
    {
        try
        {
            var records = await _service.GetAllByClubAsync(clubId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving club movement records for club {ClubId}", clubId);
            return StatusCode(500, new { message = "Error retrieving club movement record list." });
        }
    }

    /// <summary>
    /// Get club movement record by ID (with details)
    /// </summary>
    [HttpGet("id/{id}")]
    [Authorize]
    public async Task<ActionResult<ClubMovementRecordDto>> GetById(int id)
    {
        try
        {
            var record = await _service.GetByIdAsync(id);
            if (record == null)
                return NotFound(new { message = $"Club movement record not found with ID {id}." });

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving club movement record {Id}", id);
            return StatusCode(500, new { message = "Error retrieving club movement record." });
        }
    }

    /// <summary>
    /// Add manual score for club (Admin only)
    /// </summary>
    [HttpPost("add-manual-score")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubMovementRecordDto>> AddManualScore([FromBody] AddClubManualScoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get current user ID from claims and set to DTO
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                dto.CreatedById = userId;
                _logger.LogInformation("Set CreatedById = {UserId} from HttpContext", userId);
            }
            else
            {
                _logger.LogWarning("Could not get userId from HttpContext claims");
            }

            var record = await _service.AddManualScoreAsync(dto);
            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding manual score for club");
            return StatusCode(500, new { message = "Error adding manual score." });
        }
    }

    /// <summary>
    /// Update manual score for club (Admin only)
    /// </summary>
    [HttpPut("update-manual-score")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubMovementRecordDto>> UpdateManualScore([FromBody] UpdateClubManualScoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get current user ID from claims
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            int adminUserId = 0;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                adminUserId = userId;
            }

            var record = await _service.UpdateManualScoreAsync(dto, adminUserId);
            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating manual score");
            return StatusCode(500, new { message = "Error updating manual score." });
        }
    }

    /// <summary>
    /// Delete manual score for club (Admin only)
    /// </summary>
    [HttpDelete("manual-score/{detailId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteManualScore(int detailId)
    {
        try
        {
            await _service.DeleteManualScoreAsync(detailId);
            return Ok(new { message = "Score deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting manual score");
            return StatusCode(500, new { message = "Error deleting manual score." });
        }
    }
}

