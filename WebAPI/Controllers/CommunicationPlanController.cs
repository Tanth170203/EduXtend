using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.DTOs.CommunicationPlan;
using Services.CommunicationPlans;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/communication-plans")]
    [Authorize]
    public class CommunicationPlanController : ControllerBase
    {
        private readonly ICommunicationPlanService _service;
        private readonly ILogger<CommunicationPlanController> _logger;

        public CommunicationPlanController(
            ICommunicationPlanService service,
            ILogger<CommunicationPlanController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // Helper method to get current user ID
        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return null;
            return userId;
        }

        // POST api/communication-plans
        // Tạo kế hoạch truyền thông (Manager, Admin)
        [HttpPost]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> CreatePlan([FromBody] CreateCommunicationPlanDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.CreatePlanAsync(userId.Value, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating communication plan");
                return StatusCode(500, new { message = "An error occurred while creating the communication plan" });
            }
        }

        // PUT api/communication-plans/{id}
        // Cập nhật kế hoạch truyền thông (Manager, Admin)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdateCommunicationPlanDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.UpdatePlanAsync(userId.Value, id, dto);
                if (result == null)
                    return NotFound(new { message = "Communication plan not found" });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication plan {PlanId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the communication plan" });
            }
        }

        // GET api/communication-plans/{id}
        // Xem chi tiết kế hoạch truyền thông
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPlan(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetPlanAsync(userId.Value, id);
                if (result == null)
                    return NotFound(new { message = "Communication plan not found" });

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communication plan {PlanId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the communication plan" });
            }
        }

        // GET api/communication-plans/club/{clubId}
        // Lấy danh sách kế hoạch truyền thông của club
        [HttpGet("club/{clubId:int}")]
        public async Task<IActionResult> GetClubPlans(int clubId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetClubPlansAsync(userId.Value, clubId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting communication plans for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving communication plans" });
            }
        }

        // DELETE api/communication-plans/{id}
        // Xóa kế hoạch truyền thông (Manager, Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.DeletePlanAsync(userId.Value, id);
                if (!result)
                    return NotFound(new { message = "Communication plan not found" });

                return Ok(new { message = "Communication plan deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting communication plan {PlanId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the communication plan" });
            }
        }

        // GET api/communication-plans/available-activities
        // Lấy danh sách activities chưa có communication plan
        [HttpGet("available-activities")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> GetAvailableActivities()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetAvailableActivitiesAsync(userId.Value);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available activities");
                return StatusCode(500, new { message = "An error occurred while retrieving available activities" });
            }
        }
    }
}
