using BusinessObject.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Users;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IUserService userService, ILogger<RolesController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                var roleDtos = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description
                }).ToList();

                return Ok(new { success = true, data = roleDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}

