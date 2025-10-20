using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Users;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/user-management")]
[Authorize(Roles = "Admin")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UserManagementController(IUserManagementService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users with roles
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserWithRolesDto>>> GetAll()
    {
        try
        {
            var users = await _userService.GetAllWithRolesAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID with roles
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserWithRolesDto>> GetById(int id)
    {
        try
        {
            var user = await _userService.GetByIdWithRolesAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }

    /// <summary>
    /// Ban user (set IsActive = false)
    /// </summary>
    [HttpPost("{id}/ban")]
    public async Task<IActionResult> BanUser(int id)
    {
        try
        {
            await _userService.BanUserAsync(id);
            return Ok(new { message = $"User {id} has been banned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error banning user", error = ex.Message });
        }
    }

    /// <summary>
    /// Unban user (set IsActive = true)
    /// </summary>
    [HttpPost("{id}/unban")]
    public async Task<IActionResult> UnbanUser(int id)
    {
        try
        {
            await _userService.UnbanUserAsync(id);
            return Ok(new { message = $"User {id} has been unbanned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error unbanning user", error = ex.Message });
        }
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(int id, [FromBody] UpdateUserRolesDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.UserId)
                return BadRequest(new { message = "User ID mismatch" });

            await _userService.UpdateUserRolesAsync(id, dto.RoleIds);
            return Ok(new { message = "User roles updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating user roles", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        try
        {
            var roles = await _userService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving roles", error = ex.Message });
        }
    }
}

