using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.MovementCriteria;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/movement-criteria")]
public class MovementCriterionController : ControllerBase
{
    private readonly IMovementCriterionService _service;

    public MovementCriterionController(IMovementCriterionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lấy tất cả tiêu chí
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovementCriterionDto>>> GetAll()
    {
        try
        {
            var criteria = await _service.GetAllAsync();
            return Ok(criteria);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria list." });
        }
    }

    /// <summary>
    /// Lấy tiêu chí theo nhóm
    /// </summary>
    [HttpGet("by-group/{groupId}")]
    public async Task<ActionResult<IEnumerable<MovementCriterionDto>>> GetByGroupId(int groupId)
    {
        try
        {
            var criteria = await _service.GetByGroupIdAsync(groupId);
            return Ok(criteria);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria by group." });
        }
    }

    /// <summary>
    /// Lấy tiêu chí theo loại đối tượng (Student hoặc Club)
    /// </summary>
    [HttpGet("by-target-type/{targetType}")]
    public async Task<ActionResult<IEnumerable<MovementCriterionDto>>> GetByTargetType(string targetType)
    {
        try
        {
            var criteria = await _service.GetByTargetTypeAsync(targetType);
            return Ok(criteria);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria list." });
        }
    }

    /// <summary>
    /// Lấy tất cả tiêu chí đang hoạt động
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<MovementCriterionDto>>> GetActive()
    {
        try
        {
            var criteria = await _service.GetActiveAsync();
            return Ok(criteria);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving activity criteria." });
        }
    }

    /// <summary>
    /// Lấy tiêu chí theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MovementCriterionDto>> GetById(int id)
    {
        try
        {
            var criterion = await _service.GetByIdAsync(id);
            if (criterion == null)
                return NotFound(new { message = $"Criteria with ID {id} not found." });

            return Ok(criterion);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria information." });
        }
    }

    /// <summary>
    /// Tạo tiêu chí mới (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementCriterionDto>> Create([FromBody] CreateMovementCriterionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var criterion = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = criterion.Id }, criterion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error creating criteria." });
        }
    }

    /// <summary>
    /// Cập nhật tiêu chí (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementCriterionDto>> Update(int id, [FromBody] UpdateMovementCriterionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            var criterion = await _service.UpdateAsync(id, dto);
            return Ok(criterion);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error updating criteria." });
        }
    }

    /// <summary>
    /// Xóa tiêu chí (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Criteria with ID {id} not found." });

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error deleting criteria." });
        }
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của tiêu chí (Admin only)
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        try
        {
            var isActive = await _service.ToggleActiveAsync(id);
            return Ok(new 
            { 
                message = $"Criteria has been {(isActive ? "activated" : "deactivated")}.",
                isActive = isActive
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error changing criteria status." });
        }
    }
}



