using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.DTOs.ActivityMemberEvaluation;
using Services.ActivityMemberEvaluations;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/activity-member-evaluations")]
    [Authorize]
    public class ActivityMemberEvaluationController : ControllerBase
    {
        private readonly IActivityMemberEvaluationService _service;
        private readonly ILogger<ActivityMemberEvaluationController> _logger;

        public ActivityMemberEvaluationController(
            IActivityMemberEvaluationService service,
            ILogger<ActivityMemberEvaluationController> logger)
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

        // POST api/activity-member-evaluations
        // Tạo đánh giá mới (Manager, Admin)
        [HttpPost]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> CreateEvaluation([FromBody] CreateActivityMemberEvaluationDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.CreateEvaluationAsync(userId.Value, dto);
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
                _logger.LogError(ex, "Error creating evaluation");
                return StatusCode(500, new { message = "An error occurred while creating the evaluation" });
            }
        }

        // PUT api/activity-member-evaluations/{id}
        // Cập nhật đánh giá (Manager, Admin - chỉ người tạo)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> UpdateEvaluation(int id, [FromBody] CreateActivityMemberEvaluationDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.UpdateEvaluationAsync(userId.Value, id, dto);
                if (result == null)
                    return NotFound(new { message = "Evaluation not found" });

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
                _logger.LogError(ex, "Error updating evaluation {EvaluationId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the evaluation" });
            }
        }

        // GET api/activity-member-evaluations/{id}
        // Xem chi tiết đánh giá (Manager, Admin, hoặc người được đánh giá)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEvaluationById(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetEvaluationByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = "Evaluation not found" });

                // Check authorization: Admin, Manager, or the person being evaluated
                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("ClubManager");
                var isEvaluatee = result.UserId.HasValue && result.UserId.Value == userId.Value;

                if (!isAdmin && !isManager && !isEvaluatee)
                    return StatusCode(403, new { message = "You do not have permission to view this evaluation" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluation {EvaluationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the evaluation" });
            }
        }

        // GET api/activity-member-evaluations/assignment/{assignmentId}
        // Xem đánh giá theo assignment (Manager, Admin)
        [HttpGet("assignment/{assignmentId:int}")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> GetEvaluationByAssignment(int assignmentId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetEvaluationByAssignmentIdAsync(assignmentId);
                if (result == null)
                    return NotFound(new { message = "Evaluation not found for this assignment" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluation for assignment {AssignmentId}", assignmentId);
                return StatusCode(500, new { message = "An error occurred while retrieving the evaluation" });
            }
        }

        // GET api/activities/{activityId}/evaluation-assignments
        // Lấy danh sách assignments để đánh giá (Manager, Admin)
        [HttpGet("/api/activities/{activityId:int}/evaluation-assignments")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> GetAssignmentsForEvaluation(int activityId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetAssignmentsForEvaluationAsync(activityId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for activity {ActivityId}", activityId);
                return StatusCode(500, new { message = "An error occurred while retrieving assignments" });
            }
        }

        // GET api/activities/{activityId}/evaluation-report
        // Báo cáo tổng hợp (Manager, Admin)
        [HttpGet("/api/activities/{activityId:int}/evaluation-report")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> GetActivityEvaluationReport(int activityId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetActivityEvaluationReportAsync(activityId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluation report for activity {ActivityId}", activityId);
                return StatusCode(500, new { message = "An error occurred while retrieving the evaluation report" });
            }
        }

        // GET api/users/{userId}/evaluation-history
        // Lịch sử đánh giá của thành viên (Manager, Admin, hoặc chính user đó)
        [HttpGet("/api/users/{targetUserId:int}/evaluation-history")]
        public async Task<IActionResult> GetMemberEvaluationHistory(int targetUserId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Check authorization: Admin, Manager, or the user themselves
                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("ClubManager");
                var isSelf = targetUserId == userId.Value;

                if (!isAdmin && !isManager && !isSelf)
                    return StatusCode(403, new { message = "You do not have permission to view this evaluation history" });

                var result = await _service.GetMemberEvaluationHistoryAsync(targetUserId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluation history for user {UserId}", targetUserId);
                return StatusCode(500, new { message = "An error occurred while retrieving evaluation history" });
            }
        }

        // GET api/activity-member-evaluations/my-evaluations
        // Xem đánh giá của chính mình (Authenticated user)
        [HttpGet("my-evaluations")]
        public async Task<IActionResult> GetMyEvaluations()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.GetMyEvaluationsAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluations for user {UserId}", userId.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving your evaluations" });
            }
        }
    }
}
