using BusinessObject.DTOs.Interview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interviews;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly IInterviewService _interviewService;
        private readonly ILogger<InterviewController> _logger;

        public InterviewController(IInterviewService interviewService, ILogger<InterviewController> logger)
        {
            _interviewService = interviewService;
            _logger = logger;
        }

        // POST api/interview/schedule
        [HttpPost("schedule")]
        [Authorize]
        public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var interview = await _interviewService.ScheduleInterviewAsync(dto, userId);
                return Ok(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling interview");
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/interview/request/{joinRequestId}
        [HttpGet("request/{joinRequestId}")]
        [Authorize]
        public async Task<IActionResult> GetByJoinRequestId(int joinRequestId)
        {
            try
            {
                var interview = await _interviewService.GetByJoinRequestIdAsync(joinRequestId);
                if (interview == null)
                {
                    return NotFound(new { message = "No interview scheduled for this request" });
                }
                return Ok(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview");
                return StatusCode(500, new { message = "Failed to get interview" });
            }
        }

        // GET api/interview/my-interviews
        [HttpGet("my-interviews")]
        [Authorize]
        public async Task<IActionResult> GetMyInterviews()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var interviews = await _interviewService.GetMyInterviewsAsync(userId);
                return Ok(interviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my interviews");
                return StatusCode(500, new { message = "Failed to get interviews" });
            }
        }

        // PUT api/interview/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInterview(int id, [FromBody] UpdateInterviewDto dto)
        {
            try
            {
                var interview = await _interviewService.UpdateInterviewAsync(id, dto);
                return Ok(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating interview");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/interview/{id}/evaluation
        [HttpPut("{id}/evaluation")]
        [Authorize]
        public async Task<IActionResult> UpdateEvaluation(int id, [FromBody] UpdateEvaluationDto dto)
        {
            try
            {
                var interview = await _interviewService.UpdateEvaluationAsync(id, dto);
                return Ok(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating evaluation");
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/interview/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var interview = await _interviewService.GetByIdAsync(id);
                if (interview == null)
                {
                    return NotFound(new { message = "Interview not found" });
                }
                return Ok(interview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview");
                return StatusCode(500, new { message = "Failed to get interview" });
            }
        }

        // DELETE api/interview/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInterview(int id)
        {
            try
            {
                var result = await _interviewService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Interview not found" });
                }
                return Ok(new { message = "Interview deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting interview");
                return StatusCode(500, new { message = "Failed to delete interview" });
            }
        }
    }
}

