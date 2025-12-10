using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Users;
using Repositories.Clubs;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/user-management")]
[Authorize(Roles = "Admin")]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly IClubRepository _clubRepo;

    public UserManagementController(IUserManagementService userService, IClubRepository clubRepo)
    {
        _userService = userService;
        _clubRepo = clubRepo;
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
    /// Update user role (single role per user)
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != request.UserId)
                return BadRequest(new { message = "User ID mismatch" });

            // UpdateUserRolesAsync now takes a list but uses only the first role
            await _userService.UpdateUserRolesAsync(id, new List<int> { request.RoleId }, request.ClubId);
            return Ok(new { message = "User role updated successfully" });
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
            return StatusCode(500, new { message = "Error updating user role", error = ex.Message });
        }
    }

    // Simple DTO for role update
    public class UpdateUserRoleRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int? ClubId { get; set; } // Required when role is ClubMember or ClubManager
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

    /// <summary>
    /// Get all active clubs for role assignment
    /// </summary>
    [HttpGet("clubs")]
    public async Task<ActionResult<IEnumerable<ClubSimpleDto>>> GetAllClubs()
    {
        try
        {
            var clubs = await _clubRepo.GetAllAsync();
            var activeClubs = clubs.Where(c => c.IsActive).Select(c => new ClubSimpleDto
            {
                Id = c.Id,
                Name = c.Name,
                SubName = c.SubName
            }).ToList();
            return Ok(activeClubs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving clubs", error = ex.Message });
        }
    }

    public class ClubSimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string SubName { get; set; } = null!;
    }
}

