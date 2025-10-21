using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Evidences;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/evidences")]
public class EvidenceController : ControllerBase
{
    private readonly IEvidenceService _service;
    private readonly ILogger<EvidenceController> _logger;

    public EvidenceController(IEvidenceService service, ILogger<EvidenceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all evidences (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetAll()
    {
        try
        {
            var evidences = await _service.GetAllAsync();
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidences");
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    /// <summary>
    /// Get pending evidences (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetPending()
    {
        try
        {
            var evidences = await _service.GetPendingEvidencesAsync();
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending evidences");
            return StatusCode(500, new { message = "Error retrieving pending evidence list." });
        }
    }

    /// <summary>
    /// Get evidences by status
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetByStatus(string status)
    {
        try
        {
            var evidences = await _service.GetByStatusAsync(status);
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidences by status");
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    /// <summary>
    /// Get evidences by student ID
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetByStudentId(int studentId)
    {
        try
        {
            var evidences = await _service.GetByStudentIdAsync(studentId);
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student evidences");
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    /// <summary>
    /// Get evidence by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<EvidenceDto>> GetById(int id)
    {
        try
        {
            var evidence = await _service.GetByIdAsync(id);
            if (evidence == null)
                return NotFound(new { message = $"Evidence with ID {id} not found." });

            return Ok(evidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidence");
            return StatusCode(500, new { message = "Error retrieving evidence information." });
        }
    }

    /// <summary>
    /// Create new evidence (Student)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EvidenceDto>> Create([FromBody] CreateEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var evidence = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = evidence.Id }, evidence);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating evidence");
            return StatusCode(500, new { message = "Error creating evidence." });
        }
    }

    /// <summary>
    /// Update evidence (Student, only if Pending)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<EvidenceDto>> Update(int id, [FromBody] UpdateEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            var evidence = await _service.UpdateAsync(id, dto);
            return Ok(evidence);
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
            _logger.LogError(ex, "Error updating evidence");
            return StatusCode(500, new { message = "Error updating evidence." });
        }
    }

    /// <summary>
    /// Review evidence (Admin only)
    /// </summary>
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EvidenceDto>> Review(int id, [FromBody] ReviewEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            var evidence = await _service.ReviewAsync(id, dto);
            return Ok(evidence);
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
            _logger.LogError(ex, "Error reviewing evidence");
            return StatusCode(500, new { message = "Error reviewing evidence." });
        }
    }

    /// <summary>
    /// Delete evidence
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Evidence with ID {id} not found." });

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting evidence");
            return StatusCode(500, new { message = "Error deleting evidence." });
        }
    }

    /// <summary>
    /// Get pending count (Admin only)
    /// </summary>
    [HttpGet("stats/pending-count")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<int>> GetPendingCount()
    {
        try
        {
            var count = await _service.CountPendingAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending count");
            return StatusCode(500, new { message = "Error getting statistics." });
        }
    }
}


