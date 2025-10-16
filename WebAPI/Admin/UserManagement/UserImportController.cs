using BusinessObject.DTOs.ImportFile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.UserImport;

namespace WebAPI.Admin.UserManagement
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserImportController : ControllerBase
    {
        private readonly IUserImportService _userImportService;
        private readonly ILogger<UserImportController> _logger;

        public UserImportController(
            IUserImportService userImportService,
            ILogger<UserImportController> logger)
        {
            _userImportService = userImportService;
            _logger = logger;
        }

        /// <summary>
        /// Import users from Excel file (Admin only)
        /// Expected Excel format:
        /// Column A: Email (required)
        /// Column B: Full Name (required)
        /// Column C: Phone Number (optional)
        /// Column D: Roles (optional, comma-separated, e.g., "Student,ClubMember")
        /// Column E: Is Active (optional, true/false/1/0/yes/no, default: true)
        /// Column F: Student Code (required if role includes "Student")
        /// Column G: Cohort (required if role includes "Student", e.g., "K17", "K18", "K20")
        /// Column H: Date of Birth (optional, format: YYYY-MM-DD)
        /// Column I: Gender (optional, Male/Female/Other, default: Other)
        /// Column J: Enrollment Date (optional, format: YYYY-MM-DD, default: current date)
        /// Column K: Major Code (required if role includes "Student", e.g., "SE", "IA", "AI")
        /// Column L: Student Status (optional, Active/Inactive/Graduated, default: Active)
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> ImportUsers([FromForm] ImportUsersRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid data", errors = ModelState });
                }

                var result = await _userImportService.ImportUsersFromExcelAsync(request.File);

                _logger.LogInformation(
                    "User import completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                    result.TotalRows,
                    result.SuccessCount,
                    result.FailureCount
                );

                if (result.FailureCount > 0)
                {
                    return Ok(new
                    {
                        message = $"Import completed with some errors. Success: {result.SuccessCount}/{result.TotalRows}",
                        data = result
                    });
                }

                return Ok(new
                {
                    message = $"Successfully imported {result.SuccessCount} users",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid import file: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users");
                return StatusCode(500, new { message = "Error importing users. Please try again." });
            }
        }

        /// <summary>
        /// Download sample Excel template for user import
        /// </summary>
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                // Create a simple CSV template
                var csv = "Email,FullName,Roles,IsActive\n";
                csv += "student1@fpt.edu.vn,John Doe,Student,true\n";
                csv += "student2@fpt.edu.vn,Jane Smith,Student,true\n";
                csv += "admin@fpt.edu.vn,Admin User,\"Admin,Student\",true\n";

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", "user_import_template.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template");
                return StatusCode(500, new { message = "Error generating template" });
            }
        }

        /// <summary>
        /// Get available roles for import
        /// </summary>
        [HttpGet("roles")]
        public IActionResult GetAvailableRoles()
        {
            try
            {
                // Return common role names
                var roles = new List<string>
                {
                    "Admin",
                    "Student",
                    "ClubManager",
                    "ClubMember"
                };

                return Ok(new
                {
                    message = "List of available roles for import",
                    roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { message = "Error getting roles list" });
            }
        }
    }
}

