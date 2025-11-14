using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;
using Services.Evidences;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _service;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ActivityController> _logger;
        
        public ActivityController(IActivityService service, ICloudinaryService cloudinaryService, ILogger<ActivityController> logger)
        {
            _service = service;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

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

        // ================= CLUB MANAGER =================
        // GET api/activity/my-club-activities
        [HttpGet("my-club-activities")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> GetMyClubActivities()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Find club managed by this user - call club service or repository
                // For now, get clubId from managed club
                var activities = await _service.GetActivitiesByManagerIdAsync(userId);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST api/activity/club-manager
        [HttpPost("club-manager")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> CreateByClubManager([FromBody] BusinessObject.DTOs.Activity.ClubCreateActivityDto dto, [FromQuery] int clubId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            if (clubId <= 0)
                return BadRequest(new { message = "Missing clubId" });

            try
            {
                var result = await _service.ClubCreateAsync(userId, clubId, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/activity/club-manager/{id}
        [HttpPut("club-manager/{id}")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> UpdateByClubManager(int id, [FromBody] BusinessObject.DTOs.Activity.ClubCreateActivityDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.ClubUpdateAsync(userId, id, dto);
                if (result == null) return NotFound(new { message = "Activity not found or you don't have permission" });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/activity/club-manager/{id}
        [HttpDelete("club-manager/{id}")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> DeleteByClubManager(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var result = await _service.ClubDeleteAsync(userId, id);
                if (!result) return NotFound(new { message = "Activity not found or you don't have permission" });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET api/activity/club-manager/{id}/registrants
        [HttpGet("club-manager/{id:int}/registrants")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> GetRegistrantsByClubManager(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var list = await _service.GetClubRegistrantsAsync(userId, id);
                return Ok(list);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        // POST api/activity/club-manager/{id}/attendance/{userId}
        [HttpPost("club-manager/{id:int}/attendance/{targetUserId:int}")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> SetAttendanceByClubManager(
            int id, 
            int targetUserId, 
            [FromQuery] bool isPresent,
            [FromQuery] int? participationScore = null)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                var (success, message) = await _service.SetClubAttendanceAsync(userId, id, targetUserId, isPresent, participationScore);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
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

	// GET api/activity/{id}/feedbacks - For ClubManager to view all feedbacks
	[HttpGet("{id:int}/feedbacks")]
	[Authorize(Roles = "ClubManager,Admin")]
	public async Task<IActionResult> GetActivityFeedbacks(int id)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		var list = await _service.GetActivityFeedbacksAsync(userId, id);
		return Ok(list);
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

	// ================= ACTIVITY IMAGE UPLOAD =================
	// POST api/activity/upload-image
	[HttpPost("upload-image")]
	[Authorize]
	public async Task<IActionResult> UploadActivityImage(IFormFile file)
	{
		try
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file provided" });

			// Upload to "activities" folder on Cloudinary (root level)
			var url = await _cloudinaryService.UploadActivityImageAsync(file);
			_logger.LogInformation("Uploaded activity image to Cloudinary: {Url}", url);
			return Ok(new { url });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading activity image");
			return BadRequest(new { message = ex.Message });
		}
	}
    }
}



