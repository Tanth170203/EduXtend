using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Users;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _service;

    public ProfileController(IUserProfileService service)
    {
        _service = service;
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
}


