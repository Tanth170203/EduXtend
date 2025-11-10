using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Evidences;
using Services.Users;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _service;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserProfileService service, ICloudinaryService cloudinaryService, ILogger<ProfileController> logger)
    {
        _service = service;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr)) throw new UnauthorizedAccessException("Missing user id");
        return int.Parse(userIdStr);
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _service.GetMyProfileAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving profile", error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var userId = GetCurrentUserId();
            var updated = await _service.UpdateMyProfileAsync(userId, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating profile", error = ex.Message });
        }
    }

    // ================= AVATAR UPLOAD =================
    // POST api/profile/upload-avatar
    [HttpPost("upload-avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            // Upload to "avatars" folder on Cloudinary (root level)
            var url = await _cloudinaryService.UploadAvatarAsync(file);
            _logger.LogInformation("Uploaded avatar to Cloudinary: {Url}", url);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar");
            return BadRequest(new { message = ex.Message });
        }
    }
}


