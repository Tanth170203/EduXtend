using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.JoinRequests;
using BusinessObject.DTOs.JoinRequest;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JoinRequestController : ControllerBase
    {
        private readonly IJoinRequestService _service;
        private readonly ILogger<JoinRequestController> _logger;

        public JoinRequestController(IJoinRequestService service, ILogger<JoinRequestController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET api/joinrequest/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var request = await _service.GetByIdAsync(id);
                if (request == null)
                    return NotFound(new { message = "Join request not found" });

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting join request {Id}", id);
                return StatusCode(500, new { message = "Failed to retrieve join request" });
            }
        }

        // GET api/joinrequest/club/{clubId}
        [HttpGet("club/{clubId:int}")]
        [Authorize]
        public async Task<IActionResult> GetByClubId(int clubId)
        {
            try
            {
                var requests = await _service.GetByClubIdAsync(clubId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting join requests for club {ClubId}", clubId);
                return StatusCode(500, new { message = "Failed to retrieve join requests" });
            }
        }

        // GET api/joinrequest/user/{userId}
        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            try
            {
                // Verify the requesting user matches the userId or is an admin
                var requestingUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(requestingUserId) || !int.TryParse(requestingUserId, out var currentUserId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                if (currentUserId != userId)
                {
                    // Check if user is admin/manager (optional - for now we just check if it's the same user)
                    return Forbid();
                }

                var requests = await _service.GetByUserIdAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting join requests for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to retrieve join requests" });
            }
        }

        // GET api/joinrequest/club/{clubId}/pending
        [HttpGet("club/{clubId:int}/pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingByClubId(int clubId)
        {
            try
            {
                var requests = await _service.GetPendingByClubIdAsync(clubId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending join requests for club {ClubId}", clubId);
                return StatusCode(500, new { message = "Failed to retrieve pending requests" });
            }
        }

        // GET api/joinrequest/club/{clubId}/departments
        [HttpGet("club/{clubId:int}/departments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClubDepartments(int clubId)
        {
            try
            {
                var departments = await _service.GetClubDepartmentsAsync(clubId);
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments for club {ClubId}", clubId);
                return StatusCode(500, new { message = "Failed to retrieve departments" });
            }
        }

        // POST api/joinrequest
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateJoinRequestDto dto)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // Check if user can apply
                if (!await _service.CanApplyAsync(userId, dto.ClubId))
                {
                    return BadRequest(new { message = "You cannot apply to this club at this time" });
                }

                var request = await _service.CreateAsync(userId, dto);
                _logger.LogInformation("User {UserId} applied to club {ClubId}", userId, dto.ClubId);

                return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating join request");
                return StatusCode(500, new { message = "Failed to create join request" });
            }
        }

        // POST api/joinrequest/{id}/process
        [HttpPost("{id:int}/process")]
        [Authorize]
        public async Task<IActionResult> ProcessRequest(int id, [FromBody] ProcessJoinRequestDto dto)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var success = await _service.ProcessRequestAsync(id, userId, dto.Action);
                if (!success)
                {
                    return NotFound(new { message = "Join request not found" });
                }

                _logger.LogInformation("User {UserId} {Action}d join request {RequestId}", userId, dto.Action.ToLower(), id);
                return Ok(new { message = $"Request {dto.Action.ToLower()}d successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing join request {Id}", id);
                return StatusCode(500, new { message = "Failed to process join request" });
            }
        }

        // GET api/joinrequest/can-apply/{clubId}
        [HttpGet("can-apply/{clubId:int}")]
        [Authorize]
        public async Task<IActionResult> CanApply(int clubId)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var canApply = await _service.CanApplyAsync(userId, clubId);
                return Ok(new { canApply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can apply to club {ClubId}", clubId);
                return StatusCode(500, new { message = "Failed to check application status" });
            }
        }

        // GET api/joinrequest/my-request/{clubId}
        [HttpGet("my-request/{clubId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyRequestForClub(int clubId)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var request = await _service.GetMyRequestForClubAsync(userId, clubId);
                if (request == null)
                {
                    return NotFound(new { message = "No request found" });
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user request for club {ClubId}", clubId);
                return StatusCode(500, new { message = "Failed to retrieve request" });
            }
        }
    }
}

