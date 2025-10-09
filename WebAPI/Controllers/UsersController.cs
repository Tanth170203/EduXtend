using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Users;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var userDtos = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    GoogleSubject = u.GoogleSubject,
                    AvatarUrl = u.AvatarUrl,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    UserRoles = u.UserRoles?.Select(ur => new UserRoleDto
                    {
                        Id = ur.Id,
                        UserId = ur.UserId,
                        RoleId = ur.RoleId,
                        AssignedAt = ur.AssignedAt,
                        Role = ur.Role != null ? new RoleDto
                        {
                            Id = ur.Role.Id,
                            RoleName = ur.Role.RoleName,
                            Description = ur.Role.Description
                        } : null
                    }).ToList()
                }).ToList();

                return Ok(new { success = true, data = userDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    GoogleSubject = user.GoogleSubject,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    UserRoles = user.UserRoles?.Select(ur => new UserRoleDto
                    {
                        Id = ur.Id,
                        UserId = ur.UserId,
                        RoleId = ur.RoleId,
                        AssignedAt = ur.AssignedAt,
                        Role = ur.Role != null ? new RoleDto
                        {
                            Id = ur.Role.Id,
                            RoleName = ur.Role.RoleName,
                            Description = ur.Role.Description
                        } : null
                    }).ToList()
                };

                return Ok(new { success = true, data = userDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Toggle user active status (ban/unban)
        /// </summary>
        [HttpPut("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var success = await _userService.ToggleUserActiveStatusAsync(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                return Ok(new { success = true, message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update user role
        /// </summary>
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var success = await _userService.UpdateUserRoleAsync(id, request.RoleId);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Failed to update user role. User or role may not exist." });
                }

                return Ok(new { success = true, message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}

