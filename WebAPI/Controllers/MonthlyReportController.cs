using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.DTOs.MonthlyReport;
using Services.MonthlyReports;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/monthly-reports")]
    [Authorize]
    public class MonthlyReportController : ControllerBase
    {
        private readonly IMonthlyReportService _service;
        private readonly IMonthlyReportPdfService _pdfService;
        private readonly ILogger<MonthlyReportController> _logger;

        public MonthlyReportController(
            IMonthlyReportService service,
            IMonthlyReportPdfService pdfService,
            ILogger<MonthlyReportController> logger)
        {
            _service = service;
            _pdfService = pdfService;
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
        /// GET api/monthly-reports?clubId={clubId}
        /// Get all monthly reports for a club
        /// Requirements: 2.1, 2.2, 2.3, 2.4
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllReports([FromQuery] int? clubId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // If no clubId provided and user is Admin, return all reports
                if (!clubId.HasValue)
                {
                    var isAdmin = User.IsInRole("Admin");
                    if (isAdmin)
                    {
                        var allReports = await _service.GetAllReportsForAdminAsync();
                        return Ok(new { data = allReports, count = allReports.Count });
                    }
                    else
                    {
                        return BadRequest(new { message = "Club ID is required for non-admin users" });
                    }
                }

                if (clubId.Value <= 0)
                    return BadRequest(new { message = "Invalid club ID" });

                var reports = await _service.GetAllReportsAsync(clubId.Value);
                return Ok(new { data = reports, count = reports.Count });
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
                _logger.LogError(ex, "Error getting monthly reports for club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred while retrieving monthly reports" });
            }
        }

        /// <summary>
        /// GET api/monthly-reports/{id}
        /// Get report by ID with fresh data aggregation
        /// Requirements: 3.1-3.10
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReport(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var report = await _service.GetReportWithFreshDataAsync(id);
                if (report == null)
                    return NotFound(new { message = "Monthly report not found" });

                return Ok(report);
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
                _logger.LogError(ex, "Error getting monthly report {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the monthly report" });
            }
        }

        /// <summary>
        /// POST api/monthly-reports
        /// Create a new monthly report
        /// Requirements: 1.1, 1.2, 1.3, 1.4
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> CreateReport([FromBody] CreateMonthlyReportDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                if (dto.ClubId <= 0)
                    return BadRequest(new { message = "Invalid club ID" });

                if (dto.Month < 1 || dto.Month > 12)
                    return BadRequest(new { message = "Month must be between 1 and 12" });

                if (dto.Year < 2000 || dto.Year > 2100)
                    return BadRequest(new { message = "Invalid year" });

                var reportId = await _service.CreateMonthlyReportAsync(dto.ClubId, dto.Month, dto.Year);
                var report = await _service.GetReportWithFreshDataAsync(reportId);
                
                return CreatedAtAction(
                    nameof(GetReport), 
                    new { id = reportId }, 
                    report);
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
                _logger.LogError(ex, "Error creating monthly report for club {ClubId}", dto.ClubId);
                return StatusCode(500, new { message = "An error occurred while creating the monthly report" });
            }
        }

        /// <summary>
        /// PUT api/monthly-reports/{id}
        /// Update editable sections of a report
        /// Requirements: 9.1-9.2, 15.1-15.6
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateMonthlyReportDto dto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                await _service.UpdateReportAsync(id, dto);
                var report = await _service.GetReportWithFreshDataAsync(id);
                
                return Ok(report);
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
                _logger.LogError(ex, "Error updating monthly report {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the monthly report" });
            }
        }

        /// <summary>
        /// POST api/monthly-reports/{id}/submit
        /// Submit report for approval
        /// Requirements: 17.1, 17.2, 17.3, 17.4
        /// </summary>
        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> SubmitReport(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                await _service.SubmitReportAsync(id, userId.Value);
                return Ok(new { message = "Báo cáo đã được nộp thành công và đang chờ phê duyệt" });
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
                _logger.LogError(ex, "Error submitting monthly report {ReportId}", id);
                return StatusCode(500, new { message = "An error occurred while submitting the monthly report" });
            }
        }

        /// <summary>
        /// GET api/monthly-reports/{id}/pdf
        /// Export report to PDF
        /// Requirements: 20.1, 20.2, 20.3, 20.4
        /// </summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> ExportToPdf(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var pdfBytes = await _pdfService.ExportToPdfAsync(id);
                
                // Get report info for filename
                var report = await _service.GetReportByIdAsync(id);
                
                // Calculate next month for filename
                var nextMonth = report.ReportMonth == 12 ? 1 : report.ReportMonth + 1;
                var clubNameSafe = report.ClubName.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
                
                // Format: BÁO CÁO HOẠT ĐỘNG THÁNG 12 VÀ KẾ HOẠCH HOẠT ĐỘNG THÁNG 1_ClubName.pdf
                var fileName = $"BAO_CAO_HOAT_DONG_THANG_{report.ReportMonth}_VA_KE_HOACH_HOAT_DONG_THANG_{nextMonth}_{clubNameSafe}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
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
                _logger.LogError(ex, "Error exporting monthly report {ReportId} to PDF", id);
                return StatusCode(500, new { message = "An error occurred while exporting the monthly report to PDF" });
            }
        }
    }
}
