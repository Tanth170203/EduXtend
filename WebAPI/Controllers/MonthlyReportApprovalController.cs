using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.DTOs.MonthlyReport;
using Services.MonthlyReports;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/monthly-reports")]
    [Authorize(Roles = "Admin")]
    public class MonthlyReportApprovalController : ControllerBase
    {
        private readonly IMonthlyReportApprovalService _approvalService;
        private readonly ILogger<MonthlyReportApprovalController> _logger;

        public MonthlyReportApprovalController(
            IMonthlyReportApprovalService approvalService,
            ILogger<MonthlyReportApprovalController> logger)
        {
            _approvalService = approvalService;
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

        /// <summary>
        /// POST api/monthly-reports/{id}/approve
        /// Approve a monthly report (Admin only)
        /// Requirements: 18.1, 18.2, 18.3
        /// </summary>
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                await _approvalService.ApproveReportAsync(id, userId.Value);
                return Ok(new { message = "Báo cáo đã được phê duyệt thành công" });
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
                _logger.LogError(ex, "Error approving monthly report {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while approving the monthly report" });
            }
        }

        /// <summary>
        /// POST api/monthly-reports/{id}/reject
        /// Reject a monthly report with reason (Admin only)
        /// Requirements: 18.4, 18.5, 18.6, 18.7, 18.8
        /// </summary>
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> RejectReport(int id, [FromBody] RejectMonthlyReportDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Reason))
                    return BadRequest(new { message = "Rejection reason is required" });

                await _approvalService.RejectReportAsync(id, userId.Value, dto.Reason);
                return Ok(new { message = "Báo cáo đã bị từ chối" });
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
                _logger.LogError(ex, "Error rejecting monthly report {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while rejecting the monthly report" });
            }
        }
    }
}
