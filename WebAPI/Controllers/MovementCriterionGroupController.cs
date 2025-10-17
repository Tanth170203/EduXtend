using BusinessObject.DTOs.MovementCriteria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.MovementCriteria;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/movement-criterion-groups")]
public class MovementCriterionGroupController : ControllerBase
{
    private readonly IMovementCriterionGroupService _service;

    public MovementCriterionGroupController(IMovementCriterionGroupService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lấy tất cả nhóm tiêu chí
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovementCriterionGroupDto>>> GetAll()
    {
        try
        {
            var groups = await _service.GetAllAsync();
            return Ok(groups);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria groups list." });
        }
    }

    /// <summary>
    /// Lấy nhóm tiêu chí theo loại đối tượng (Student hoặc Club)
    /// </summary>
    [HttpGet("by-target-type/{targetType}")]
    public async Task<ActionResult<IEnumerable<MovementCriterionGroupDto>>> GetByTargetType(string targetType)
    {
        try
        {
            var groups = await _service.GetByTargetTypeAsync(targetType);
            return Ok(groups);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria groups list." });
        }
    }

    /// <summary>
    /// Lấy nhóm tiêu chí theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MovementCriterionGroupDto>> GetById(int id)
    {
        try
        {
            var group = await _service.GetByIdAsync(id);
            if (group == null)
                return NotFound(new { message = $"Criteria group with ID {id} not found." });

            return Ok(group);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria group information." });
        }
    }

    /// <summary>
    /// Lấy nhóm tiêu chí chi tiết (bao gồm các tiêu chí con)
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<ActionResult<MovementCriterionGroupDetailDto>> GetByIdWithCriteria(int id)
    {
        try
        {
            var group = await _service.GetByIdWithCriteriaAsync(id);
            if (group == null)
                return NotFound(new { message = $"Criteria group with ID {id} not found." });

            return Ok(group);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error retrieving criteria group details." });
        }
    }

    /// <summary>
    /// Tạo nhóm tiêu chí mới (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementCriterionGroupDto>> Create([FromBody] CreateMovementCriterionGroupDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var group = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error creating criteria group." });
        }
    }

    /// <summary>
    /// Cập nhật nhóm tiêu chí (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovementCriterionGroupDto>> Update(int id, [FromBody] UpdateMovementCriterionGroupDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            var group = await _service.UpdateAsync(id, dto);
            return Ok(group);
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
            return StatusCode(500, new { message = "Error updating criteria group." });
        }
    }

    /// <summary>
    /// Xóa nhóm tiêu chí (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Criteria group with ID {id} not found." });

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
            return StatusCode(500, new { message = "Error deleting criteria group." });
        }
    }
}



