using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _service;
        public ActivityController(IActivityService service) => _service = service;

        // GET api/activity
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllActivitiesAsync();
            return Ok(data);
        }

        // GET api/activity/search?searchTerm=workshop&type=Club&status=Approved&isPublic=true&clubId=1
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] bool? isPublic,
            [FromQuery] int? clubId)
        {
            var data = await _service.SearchActivitiesAsync(searchTerm, type, status, isPublic, clubId);
            return Ok(data);
        }

        // GET api/activity/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            int? userId = null;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdStr) && int.TryParse(userIdStr, out var parsedId))
                userId = parsedId;

            var activity = await _service.GetActivityByIdAsync(id, userId);
            if (activity == null) return NotFound();
            return Ok(activity);
        }

        // GET api/activity/club/{clubId}
        [HttpGet("club/{clubId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByClubId(int clubId)
        {
            var activities = await _service.GetActivitiesByClubIdAsync(clubId);
            return Ok(activities);
        }

		// GET api/activity/my-registrations
		[HttpGet("my-registrations")]
		[Authorize]
		public async Task<IActionResult> GetMyRegistrations()
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
				return Unauthorized(new { message = "Missing user id" });

			var items = await _service.GetMyRegistrationsAsync(userId);
			return Ok(items);
		}

		// POST api/activity/{id}/register
		[HttpPost("{id:int}/register")]
		[Authorize]
		public async Task<IActionResult> Register(int id)
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
				return Unauthorized(new { message = "Missing user id" });

			// Role gate for private (club) activities
			var detail = await _service.GetActivityByIdAsync(id, userId);
			if (detail == null) return NotFound(new { message = "Activity not found" });
			if (!detail.IsPublic)
			{
				var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
				var hasClubRole = roles.Any(r => string.Equals(r, "ClubManager", StringComparison.OrdinalIgnoreCase) ||
											  string.Equals(r, "ClubMember", StringComparison.OrdinalIgnoreCase));
				if (!hasClubRole)
					return BadRequest(new { message = "This activity is for Club members only" });
			}

			var (success, message) = await _service.RegisterAsync(userId, id);
			if (!success)
				return BadRequest(new { message });

			return Ok(new { message });
		}

		// POST api/activity/{id}/feedback
		[HttpPost("{id:int}/feedback")]
		[Authorize]
		public async Task<IActionResult> SubmitFeedback(int id, [FromBody] FeedbackRequest req)
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
				return Unauthorized(new { message = "Missing user id" });

			var (success, message) = await _service.SubmitFeedbackAsync(userId, id, req.Rating, req.Comment);
			if (!success) return BadRequest(new { message });
			return Ok(new { message });
		}

		public record FeedbackRequest(int Rating, string? Comment);

		// GET api/activity/{id}/my-feedback
		[HttpGet("{id:int}/my-feedback")]
		[Authorize]
		public async Task<IActionResult> GetMyFeedback(int id)
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
				return Unauthorized(new { message = "Missing user id" });

			var fb = await _service.GetMyFeedbackAsync(userId, id);
			if (fb == null) return NotFound();
			return Ok(fb);
		}

		// POST api/activity/{id}/unregister
		[HttpPost("{id:int}/unregister")]
		[Authorize]
		public async Task<IActionResult> Unregister(int id)
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
				return Unauthorized(new { message = "Missing user id" });

			var (success, message) = await _service.UnregisterAsync(userId, id);
			if (!success)
				return BadRequest(new { message });

			return Ok(new { message });
		}
    }
}



