using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;
        private readonly IWebHostEnvironment _environment;

        public FileUploadController(ILogger<FileUploadController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
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

                // Validate file size (5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size must be less than 5MB" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Only PDF, DOC, DOCX files are allowed" });
                }

                // Create uploads directory if not exists
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "cv");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL
                var fileUrl = $"/uploads/cv/{uniqueFileName}";
                
                _logger.LogInformation("CV uploaded successfully: {FileName}", uniqueFileName);

                return Ok(new { url = fileUrl, fileName = file.FileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CV");
                return StatusCode(500, new { message = "Failed to upload file" });
            }
        }

        // GET api/fileupload/cv/{fileName}
        [HttpGet("cv/{fileName}")]
        public IActionResult ViewCV(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "cv", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "File not found" });
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing CV: {FileName}", fileName);
                return StatusCode(500, new { message = "Failed to retrieve file" });
            }
        }

        // DELETE api/fileupload/cv?url=/uploads/cv/xxx.pdf
        [HttpDelete("cv")]
        [Authorize]
        public IActionResult DeleteCV([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest(new { message = "URL is required" });
                }

                // Extract filename from URL
                var fileName = Path.GetFileName(url);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "cv", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("CV deleted: {FileName}", fileName);
                    return Ok(new { message = "File deleted successfully" });
                }

                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CV");
                return StatusCode(500, new { message = "Failed to delete file" });
            }
        }
    }
}

