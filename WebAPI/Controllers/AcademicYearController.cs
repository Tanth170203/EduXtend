using BusinessObject.DTOs.AcademicYear;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.AcademicYears;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/academic-years")]
    //[Authorize]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        /// <summary>
        /// Get all academic years
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AcademicYearDto>>> GetAll()
        {
            try
            {
                var academicYears = await _academicYearService.GetAllAsync();
                return Ok(academicYears);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting academic years: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Get academic year by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AcademicYearDto>> GetById(int id)
        {
            try
            {
                var academicYear = await _academicYearService.GetByIdAsync(id);
                if (academicYear == null)
                    return NotFound($"Academic year with ID {id} not found.");

                return Ok(academicYear);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting academic year {id}: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Get active academic year
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<AcademicYearDto>> GetActive()
        {
            try
            {
                var academicYear = await _academicYearService.GetActiveAsync();
                if (academicYear == null)
                    return NotFound("No active academic year found.");

                return Ok(academicYear);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active academic year: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Create new academic year
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AcademicYearDto>> Create([FromBody] CreateAcademicYearDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var academicYear = await _academicYearService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = academicYear.Id }, academicYear);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating academic year: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Update academic year
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AcademicYearDto>> Update(int id, [FromBody] UpdateAcademicYearDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var academicYear = await _academicYearService.UpdateAsync(id, dto);
                return Ok(academicYear);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating academic year {id}: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Delete academic year
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _academicYearService.DeleteAsync(id);
                if (!deleted)
                    return NotFound($"Academic year with ID {id} not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting academic year {id}: {ex.Message}");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
