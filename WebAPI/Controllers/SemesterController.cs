using BusinessObject.DTOs.Semester;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Semesters;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/semesters")]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        /// <summary>
        /// Get all semesters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemesterDto>>> GetAll()
        {
            try
            {
                var semesters = await _semesterService.GetAllAsync();
                return Ok(semesters);
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Get semester by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SemesterDto>> GetById(int id)
        {
            try
            {
                var semester = await _semesterService.GetByIdAsync(id);
                if (semester == null)
                    return NotFound($"Semester with ID {id} not found.");

                return Ok(semester);
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Get active semester
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<SemesterDto>> GetActive()
        {
            try
            {
                var semester = await _semesterService.GetActiveAsync();
                if (semester == null)
                    return NotFound("No active semester found.");

                return Ok(semester);
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Create new semester (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SemesterDto>> Create([FromBody] CreateSemesterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var semester = await _semesterService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = semester.Id }, semester);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("WARNING:"))
            {
                // Return 400 với warning message để frontend xử lý
                var message = ex.Message.Substring("WARNING:".Length);
                return BadRequest(new { isWarning = true, message = message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isWarning = false, message = ex.Message });
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Update semester (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SemesterDto>> Update(int id, [FromBody] UpdateSemesterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var semester = await _semesterService.UpdateAsync(id, dto);
                return Ok(semester);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("WARNING:"))
            {
                // Return 400 với warning message để frontend xử lý
                var message = ex.Message.Substring("WARNING:".Length);
                return BadRequest(new { isWarning = true, message = message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isWarning = false, message = ex.Message });
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Update all semesters' IsActive status based on current date (Admin only)
        /// </summary>
        [HttpPost("update-active-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateActiveStatus()
        {
            try
            {
                await _semesterService.UpdateAllActiveStatusAsync();
                return Ok(new { message = "Semester active status updated successfully." });
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Check for overlapping semesters
        /// </summary>
        [HttpGet("check-overlap")]
        public async Task<IActionResult> CheckOverlap([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int? excludeId = null)
        {
            try
            {
                var overlappingSemesters = await _semesterService.GetOverlappingSemestersAsync(startDate, endDate, excludeId);
                
                return Ok(new 
                { 
                    hasOverlap = overlappingSemesters.Any(),
                    overlappingSemesters = overlappingSemesters,
                    message = overlappingSemesters.Any() 
                        ? $"Phát hiện {overlappingSemesters.Count()} học kỳ trùng thời gian." 
                        : "Không có học kỳ nào trùng thời gian."
                });
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }

        /// <summary>
        /// Delete semester (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _semesterService.DeleteAsync(id);
                if (!deleted)
                    return NotFound($"Semester with ID {id} not found.");

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error.");
        }
        }
    }
}

