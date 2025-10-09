using BusinessObject.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Students;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/majors")]
    public class MajorsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<MajorsController> _logger;

        public MajorsController(IStudentService studentService, ILogger<MajorsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        /// <summary>
        /// Get active majors
        /// </summary>
        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActive()
        {
            try
            {
                var majors = await _studentService.GetActiveMajorsAsync();
                var majorDtos = majors.Select(m => new MajorDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    Name = m.Name
                }).ToList();

                return Ok(new { success = true, data = majorDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active majors");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}

