using BusinessObject.DTOs.Evidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Evidences;
using Repositories.Students;
using System.Text.RegularExpressions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/evidences")]
public class EvidenceController : ControllerBase
{
    private readonly IEvidenceService _service;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<EvidenceController> _logger;

    public EvidenceController(
        IEvidenceService service,
        ICloudinaryService cloudinaryService,
        IStudentRepository studentRepository,
        ILogger<EvidenceController> logger)
    {
        _service = service;
        _cloudinaryService = cloudinaryService;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetAll()
    {
        try
        {
            var evidences = await _service.GetAllAsync();
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidences");
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<EvidenceDto>> GetById(int id)
    {
        try
        {
            var evidence = await _service.GetByIdAsync(id);
            if (evidence == null)
                return NotFound(new { message = $"Evidence with ID {id} not found." });

            return Ok(evidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidence");
            return StatusCode(500, new { message = "Error retrieving evidence information." });
        }
    }

    /// <summary>
    /// Get pending evidences (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetPending()
    {
        try
        {
            var evidences = await _service.GetPendingEvidencesAsync();
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending evidences");
            return StatusCode(500, new { message = "Error retrieving pending evidence list." });
        }
    }

    /// <summary>
    /// Get evidences by status (Admin only)
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetByStatus(string status)
    {
        try
        {
            if (!new[] { "Pending", "Approved", "Rejected" }.Contains(status))
                return BadRequest(new { message = "Invalid status. Must be 'Pending', 'Approved', or 'Rejected'." });

            var evidences = await _service.GetByStatusAsync(status);
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidences by status");
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    /// <summary>
    /// Get evidences by student ID (for student's own view or admin review)
    /// </summary>
    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EvidenceDto>>> GetByStudentId(int studentId)
    {
        try
        {
            var evidences = await _service.GetByStudentIdAsync(studentId);
            return Ok(evidences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving evidences for student {StudentId}", studentId);
            return StatusCode(500, new { message = "Error retrieving evidence list." });
        }
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EvidenceDto>> Create([FromForm] CreateEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.File is { Length: > 0 })
            {
                try
                {
                    var studentCode = await GetStudentCodeAsync(dto.StudentId);
                    // Lưu URL gốc (preview) từ Cloudinary
                    var rawUrl = await _cloudinaryService.UploadEvidenceFileAsync(dto.File, studentCode);
                    dto.FilePath = StripAttachment(rawUrl);
                    _logger.LogInformation("Uploaded for student {StudentId} ({StudentCode}): {Url}",
                        dto.StudentId, studentCode, dto.FilePath);
                }
                catch (Exception uploadEx)
                {
                    _logger.LogError(uploadEx, "Error uploading file to Cloudinary for student {StudentId}", dto.StudentId);
                    return BadRequest(new { message = $"File upload failed: {uploadEx.Message}" });
                }
            }

            var evidence = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = evidence.Id }, evidence);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating evidence");
            return StatusCode(500, new { message = "Error creating evidence." });
        }
    }

    [HttpPost("{id}/upload-file")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<EvidenceDto>> UploadFile(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            var evidence = await _service.GetByIdAsync(id);
            if (evidence == null)
                return NotFound(new { message = $"Evidence with ID {id} not found" });

            if (evidence.Status != "Pending")
                return BadRequest(new { message = "Cannot upload file to evidence that has been reviewed" });

            var studentCode = evidence.StudentCode ?? await GetStudentCodeAsync(evidence.StudentId);
            
            // ===== DELETE OLD FILE IF EXISTS =====
            if (!string.IsNullOrWhiteSpace(evidence.FilePath))
            {
                try
                {
                    var deleteSuccess = await _cloudinaryService.DeleteFileAsync(evidence.FilePath);
                    if (deleteSuccess)
                    {
                        _logger.LogInformation("Deleted old file for Evidence {EvidenceId}. OldUrl={OldUrl}", id, evidence.FilePath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete old file for Evidence {EvidenceId}. OldUrl={OldUrl}", id, evidence.FilePath);
                        // Continue anyway - new file will replace old in DB
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Error deleting old file for Evidence {EvidenceId}", id);
                    // Continue anyway - don't block upload if old file delete fails
                }
            }

            // ===== UPLOAD NEW FILE =====
            var rawUrl = await _cloudinaryService.UploadEvidenceFileAsync(file, studentCode);

            var updateDto = new UpdateEvidenceDto
            {
                Id = id,
                ActivityId = evidence.ActivityId,
                CriterionId = evidence.CriterionId,
                Title = evidence.Title,
                Description = evidence.Description,
                FilePath = StripAttachment(rawUrl) // save clean URL without fl_attachment
            };

            var updated = await _service.UpdateAsync(id, updateDto);
            _logger.LogInformation("Successfully updated Evidence {EvidenceId} with new file. NewUrl={NewUrl}", id, updated.FilePath);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for evidence {EvidenceId}", id);
            return StatusCode(500, new { message = "Error uploading file." });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<EvidenceDto>> Update(int id, [FromBody] UpdateEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            // Nếu ai đó gửi lên URL có fl_attachment → strip trước khi lưu
            if (!string.IsNullOrWhiteSpace(dto.FilePath))
                dto.FilePath = StripAttachment(dto.FilePath);

            var evidence = await _service.UpdateAsync(id, dto);
            return Ok(evidence);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating evidence");
            return StatusCode(500, new { message = "Error updating evidence." });
        }
    }

    /// <summary>
    /// Review (approve/reject) evidence - Admin only
    /// </summary>
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EvidenceDto>> Review(int id, [FromBody] ReviewEvidenceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID in URL and body do not match." });

            // ===== VALIDATE ReviewedById =====
            if (dto.ReviewedById <= 0)
                return BadRequest(new { message = "Reviewer ID must be valid (greater than 0)." });

            var evidence = await _service.ReviewAsync(id, dto);
            _logger.LogInformation("Evidence {EvidenceId} reviewed as {Status} by user {ReviewedById}", 
                id, dto.Status, dto.ReviewedById);
            return Ok(evidence);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing evidence");
            return StatusCode(500, new { message = "Error reviewing evidence." });
        }
    }

    // ===== Preview & Download endpoints =====

    /// <summary>Preview: bỏ fl_attachment (nếu có), redirect sang URL gốc.</summary>
    [HttpGet("{id}/preview")]
    [Authorize]
    public async Task<IActionResult> Preview(int id)
    {
        var evi = await _service.GetByIdAsync(id);
        if (evi == null || string.IsNullOrWhiteSpace(evi.FilePath))
            return NotFound(new { message = $"Evidence with ID {id} not found or no file." });

        var url = StripAttachment(evi.FilePath);
        return Redirect(url);
    }

    /// <summary>Download: ép fl_attachment[:filename], redirect → browser tải về.</summary>
    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> Download(int id)
    {
        var evi = await _service.GetByIdAsync(id);
        if (evi == null || string.IsNullOrWhiteSpace(evi.FilePath))
            return NotFound(new { message = $"Evidence with ID {id} not found or no file." });

        var fileName = GetSuggestedFileName(evi);
        var url = ForceAttachment(evi.FilePath, fileName);
        return Redirect(url);
    }

    /// <summary>Delete Evidence and its associated file from Cloudinary (Student can delete own, Admin can delete any)</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var evidence = await _service.GetByIdAsync(id);
            if (evidence == null)
                return NotFound(new { message = $"Evidence with ID {id} not found." });

            // ===== DELETE FILE FROM CLOUDINARY =====
            if (!string.IsNullOrWhiteSpace(evidence.FilePath))
            {
                try
                {
                    var deleteSuccess = await _cloudinaryService.DeleteFileAsync(evidence.FilePath);
                    if (deleteSuccess)
                    {
                        _logger.LogInformation("Deleted file from Cloudinary for Evidence {EvidenceId}. Url={Url}", id, evidence.FilePath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete file from Cloudinary for Evidence {EvidenceId}. Url={Url}", id, evidence.FilePath);
                        // Continue anyway - DB record will still be deleted
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogError(deleteEx, "Error deleting file from Cloudinary for Evidence {EvidenceId}", id);
                    // Continue anyway - don't block deletion if Cloudinary delete fails
                }
            }

            // ===== DELETE DATABASE RECORD =====
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Evidence with ID {id} not found." });

            _logger.LogInformation("Successfully deleted Evidence {EvidenceId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting evidence");
            return StatusCode(500, new { message = "Error deleting evidence." });
        }
    }

    // ===== Helpers =====

    private async Task<string> GetStudentCodeAsync(int studentId)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student != null && !string.IsNullOrEmpty(student.StudentCode))
            {
                _logger.LogInformation("Found StudentCode: {StudentCode} for StudentId: {StudentId}", student.StudentCode, studentId);
                return student.StudentCode;
            }
            var fallbackCode = $"ST{studentId}";
            _logger.LogWarning("Student not found for StudentId: {StudentId}, using fallback: {FallbackCode}", studentId, fallbackCode);
            return fallbackCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting StudentCode for StudentId: {StudentId}", studentId);
            return $"ST{studentId}";
        }
    }

    private static string GetSuggestedFileName(EvidenceDto evi)
    {
        // Gợi ý tên: Title + đuôi hiện có trong URL (nếu lấy được)
        var title = string.IsNullOrWhiteSpace(evi.Title) ? $"evidence_{evi.Id}" : evi.Title;
        title = title.Replace("/", "_");
        var ext = Path.GetExtension(evi.FilePath ?? "");
        if (string.IsNullOrWhiteSpace(ext))
        {
            // thử bắt đuôi bằng regex
            var m = Regex.Match(evi.FilePath ?? "", @"\.(pdf|docx?|xlsx?|png|jpe?g|gif|webp)$", RegexOptions.IgnoreCase);
            ext = m.Success ? m.Value : "";
        }
        return string.IsNullOrWhiteSpace(ext) ? title : (title + ext);
    }

    private static string StripAttachment(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var marker = "/fl_attachment:";
        var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return url;

        var start = idx + marker.Length;
        var slashIdx = url.IndexOf('/', start);
        if (slashIdx < 0) return url;

        var before = url[..(idx - ("/".Length))]; // bỏ '/' trước marker
        var after = url[(slashIdx + 1)..];
        return before + "/" + after;
    }

    private static string ForceAttachment(string url, string downloadFileName)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        url = StripAttachment(url); // đảm bảo không bị chèn 2 lần

        var tokens = new[] { "/raw/upload/", "/upload/" }; // hỗ trợ cả raw & image
        foreach (var token in tokens)
        {
            var pos = url.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0)
            {
                var insert = $"{token}fl_attachment:{Uri.EscapeDataString(downloadFileName.Replace("/", "_"))}/";
                return url.Replace(token, insert);
            }
        }
        return url;
    }
}
