using BusinessObject.DTOs.CVExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Clubs;
using Services.CVExport;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CVExportController : ControllerBase
    {
        private readonly ICVExportService _cvExportService;
        private readonly IClubRepository _clubRepo;
        private readonly ILogger<CVExportController> _logger;

        public CVExportController(
            ICVExportService cvExportService,
            IClubRepository clubRepo,
            ILogger<CVExportController> logger)
        {
            _cvExportService = cvExportService;
            _clubRepo = clubRepo;
            _logger = logger;
        }

        /// <summary>
        /// Extract CV data from unscheduled join requests
        /// </summary>
        [HttpPost("extract")]
        public async Task<ActionResult<CVExportResultDto>> ExtractCVData([FromBody] CVExportRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Verify user is a club manager for this club
                var club = await _clubRepo.GetByIdWithDetailsAsync(request.ClubId);
                if (club == null)
                {
                    return NotFound(new { message = "Club not found" });
                }

                // Check if user is manager of this club
                var isManager = club.Members.Any(m => 
                    m.Student.UserId == userId && 
                    m.IsActive && 
                    (m.RoleInClub == "Manager" || m.RoleInClub == "President"));

                if (!isManager)
                {
                    return Forbid();
                }

                request.RequestedByUserId = userId;

                _logger.LogInformation("User {UserId} extracting CVs for Club {ClubId}", userId, request.ClubId);

                var result = await _cvExportService.ExtractCVDataAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting CV data for Club {ClubId}", request.ClubId);
                return StatusCode(500, new { message = "An error occurred while extracting CV data", error = ex.Message });
            }
        }

        /// <summary>
        /// Download Excel file with extracted CV data
        /// </summary>
        [HttpPost("download")]
        public async Task<IActionResult> DownloadExcel([FromBody] DownloadExcelRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Get club info for filename
                var club = await _clubRepo.GetByIdWithDetailsAsync(request.ClubId);
                if (club == null)
                {
                    return NotFound(new { message = "Club not found" });
                }

                // Verify user is manager
                var isManager = club.Members.Any(m => 
                    m.Student.UserId == userId && 
                    m.IsActive && 
                    (m.RoleInClub == "Manager" || m.RoleInClub == "President"));

                if (!isManager)
                {
                    return Forbid();
                }

                _logger.LogInformation("User {UserId} downloading Excel for Club {ClubId}", userId, request.ClubId);

                // Generate Excel
                var excelBytes = await _cvExportService.GenerateExcelAsync(request.Data, club.Name);

                // Generate filename
                var sanitizedClubName = string.Join("_", club.Name.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"CV_Extracted_{sanitizedClubName}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading Excel for Club {ClubId}", request.ClubId);
                return StatusCode(500, new { message = "An error occurred while generating Excel file", error = ex.Message });
            }
        }

        /// <summary>
        /// Get count of unscheduled join requests for a club
        /// </summary>
        [HttpGet("unscheduled-count/{clubId}")]
        public async Task<ActionResult<int>> GetUnscheduledCount(int clubId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Verify user is manager
                var club = await _clubRepo.GetByIdWithDetailsAsync(clubId);
                if (club == null)
                {
                    return NotFound(new { message = "Club not found" });
                }

                var isManager = club.Members.Any(m => 
                    m.Student.UserId == userId && 
                    m.IsActive && 
                    (m.RoleInClub == "Manager" || m.RoleInClub == "President"));

                if (!isManager)
                {
                    return Forbid();
                }

                var request = new CVExportRequestDto
                {
                    ClubId = clubId,
                    RequestedByUserId = userId
                };

                var result = await _cvExportService.ExtractCVDataAsync(request);

                return Ok(new { count = result.TotalRequests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unscheduled count for Club {ClubId}", clubId);
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }
    }

    public class DownloadExcelRequest
    {
        public int ClubId { get; set; }
        public CVExportResultDto Data { get; set; } = new();
    }
}
