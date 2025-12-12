using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.StudentFinance;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentFinanceController : ControllerBase
{
    private readonly IStudentFinanceService _financeService;
    private readonly ILogger<StudentFinanceController> _logger;

    public StudentFinanceController(
        IStudentFinanceService financeService,
        ILogger<StudentFinanceController> logger)
    {
        _financeService = financeService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // GET: api/StudentFinance/pending-payments
    [HttpGet("pending-payments")]
    public async Task<IActionResult> GetPendingPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? clubId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _financeService.GetPendingPaymentsAsync(userId, page, pageSize, clubId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending payments for user");
            return StatusCode(500, new { message = "An error occurred while retrieving pending payments" });
        }
    }

    // GET: api/StudentFinance/payment-history
    [HttpGet("payment-history")]
    public async Task<IActionResult> GetPaymentHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] int? clubId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 15;

            var result = await _financeService.GetPaymentHistoryAsync(userId, page, pageSize, clubId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history for user");
            return StatusCode(500, new { message = "An error occurred while retrieving payment history" });
        }
    }

    // GET: api/StudentFinance/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _financeService.GetFinanceStatisticsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting finance statistics for user");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }

    // GET: api/StudentFinance/export-history
    [HttpGet("export-history")]
    public async Task<IActionResult> ExportPaymentHistory(
        [FromQuery] int? clubId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var csvData = await _financeService.ExportPaymentHistoryToCsvAsync(userId, clubId, startDate, endDate);
            var fileName = $"payment-history-{DateTime.UtcNow:yyyy-MM-dd}.csv";

            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payment history for user");
            return StatusCode(500, new { message = "An error occurred while exporting payment history" });
        }
    }
}
