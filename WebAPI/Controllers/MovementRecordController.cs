using BusinessObject.DTOs.MovementRecord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.MovementRecords;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/movement-records")]
public class MovementRecordController : ControllerBase
{
    private readonly IMovementRecordService _service;
    private readonly ILogger<MovementRecordController> _logger;

    public MovementRecordController(IMovementRecordService service, ILogger<MovementRecordController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all movement records (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MovementRecordDto>>> GetAll()
    {
        try
        {
            var records = await _service.GetAllAsync();
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movement records");
            return StatusCode(500, new { message = "Error retrieving movement record list." });
        }
    }

    /// <summary>
    /// Get movement records by student ID
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MovementRecordDto>>> GetByStudentId(int studentId)
    {
        try
        {
            var records = await _service.GetByStudentIdAsync(studentId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student movement records");
            return StatusCode(500, new { message = "Error retrieving movement record list." });
        }
    }

    /// <summary>
    /// Get movement records by semester ID
    /// </summary>
    [HttpGet("semester/{semesterId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MovementRecordDto>>> GetBySemesterId(int semesterId)
    {
        try
        {
            var records = await _service.GetBySemesterIdAsync(semesterId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving semester movement records");
            return StatusCode(500, new { message = "Error retrieving movement record list." });
        }
    }

    /// <summary>
    /// Get movement record by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<MovementRecordDto>> GetById(int id)
    {
        try
        {
            var record = await _service.GetByIdAsync(id);
            if (record == null)
                return NotFound(new { message = $"Movement record with ID {id} not found." });

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movement record");
            return StatusCode(500, new { message = "Error retrieving movement record information." });
        }
    }

    /// <summary>
    /// Get detailed movement record by ID
    /// </summary>
    [HttpGet("{id}/detailed")]
    [Authorize]
    public async Task<ActionResult<MovementRecordDetailedDto>> GetDetailedById(int id)
    {
        try
        {
            var record = await _service.GetDetailedByIdAsync(id);
            if (record == null)
                return NotFound(new { message = $"Movement record with ID {id} not found." });

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving detailed movement record");
            return StatusCode(500, new { message = "Error retrieving movement record details." });
        }
    }

    /// <summary>
    /// Get movement record by student and semester
    /// </summary>
    [HttpGet("student/{studentId}/semester/{semesterId}")]
    [Authorize]
    public async Task<ActionResult<MovementRecordDto>> GetByStudentAndSemester(int studentId, int semesterId)
    {
        try
        {
            var record = await _service.GetByStudentAndSemesterAsync(studentId, semesterId);
            if (record == null)
                return NotFound(new { message = $"Movement record not found for student {studentId} in semester {semesterId}." });

            return Ok(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movement record");
            return StatusCode(500, new { message = "Error retrieving movement record." });
        }
    }

    /// <summary>
    /// Create new movement record (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementRecordDto>> Create([FromBody] CreateMovementRecordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var record = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
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
            _logger.LogError(ex, "Error creating movement record");
            return StatusCode(500, new { message = "Error creating movement record." });
        }
    }

    /// <summary>
    /// Add score to movement record (Admin only)
    /// </summary>
    [HttpPost("add-score")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementRecordDto>> AddScore([FromBody] AddScoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var record = await _service.AddScoreAsync(dto);
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
            _logger.LogError(ex, "Error adding score");
            return StatusCode(500, new { message = "Error adding score." });
        }
    }

    /// <summary>
    /// Add manual score by admin (simplified - no need for criterion ID)
    /// </summary>
    [HttpPost("add-manual-score")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementRecordDto>> AddManualScore([FromBody] AddManualScoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
            _logger.LogError(ex, "Error adding manual score");
            return StatusCode(500, new { message = "Error adding manual score." });
        }
    }

    /// <summary>
    /// Add manual score with specific criterion (Admin only)
    /// </summary>
    [HttpPost("add-manual-score-with-criterion")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementRecordDto>> AddManualScoreWithCriterion([FromBody] AddManualScoreWithCriterionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var record = await _service.AddManualScoreWithCriterionAsync(dto);
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
            _logger.LogError(ex, "Error adding manual score with criterion");
            return StatusCode(500, new { message = "Error adding manual score with criterion." });
        }
    }

    /// <summary>
    /// Adjust total score manually (Admin only)
    /// </summary>
    [HttpPatch("{id}/adjust-score")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementRecordDto>> AdjustScore(int id, [FromBody] AdjustScoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            var record = await _service.AdjustScoreAsync(id, dto);
            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting score");
            return StatusCode(500, new { message = "Error adjusting score." });
        }
    }

    /// <summary>
    /// Delete movement record (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Movement record with ID {id} not found." });

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting movement record");
            return StatusCode(500, new { message = "Error deleting movement record." });
        }
    }

    /// <summary>
    /// Get student movement summary
    /// </summary>
    [HttpGet("student/{studentId}/summary")]
    [Authorize]
    public async Task<ActionResult<StudentMovementSummaryDto>> GetStudentSummary(int studentId)
    {
        try
        {
            var summary = await _service.GetStudentSummaryAsync(studentId);
            if (summary == null)
                return NotFound(new { message = $"No movement records found for student {studentId}." });

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student summary");
            return StatusCode(500, new { message = "Error retrieving student summary." });
        }
    }

    /// <summary>
    /// Get top scores by semester
    /// </summary>
    [HttpGet("semester/{semesterId}/top/{count}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<MovementRecordDto>>> GetTopScores(int semesterId, int count = 10)
    {
        try
        {
            var records = await _service.GetTopScoresBySemesterAsync(semesterId, count);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top scores");
            return StatusCode(500, new { message = "Error retrieving top scores." });
        }
    }
}


