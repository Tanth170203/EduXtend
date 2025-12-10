using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Evidences;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;
        private readonly ICloudinaryService _cloudinaryService;

        public FileUploadController(ILogger<FileUploadController> logger, ICloudinaryService cloudinaryService)
        {
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        // POST api/fileupload/cv
        [HttpPost("cv")]
        [Authorize]
        public async Task<IActionResult> UploadCV(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Validate file size (10MB for Cloudinary)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size must be less than 10MB" });
                }

                // ONLY allow PDF
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                {
                    return BadRequest(new { message = "Only PDF files are allowed" });
                }

                // Get user info for folder organization
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var studentCode = userIdClaim ?? "unknown";

                // Upload to Cloudinary (uses evidences folder)
                var cloudinaryUrl = await _cloudinaryService.UploadEvidenceFileAsync(file, studentCode);
                
                _logger.LogInformation("CV uploaded to Cloudinary: {Url}", cloudinaryUrl);

                return Ok(new { url = cloudinaryUrl, fileName = file.FileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CV to Cloudinary");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // DELETE api/fileupload/cv?url=cloudinary_url
        [HttpDelete("cv")]
        [Authorize]
        public async Task<IActionResult> DeleteCV([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest(new { message = "URL is required" });
                }

                // Delete from Cloudinary
                var success = await _cloudinaryService.DeleteFileAsync(url);
                
                if (success)
                {
                    _logger.LogInformation("CV deleted from Cloudinary: {Url}", url);
                    return Ok(new { message = "File deleted successfully" });
                }

                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CV from Cloudinary");
                return StatusCode(500, new { message = "Failed to delete file" });
            }
        }
    }
}

