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
        private readonly IActivityExtractorService _extractorService;
        private readonly ILogger<ActivityController> _logger;
        
        public ActivityController(
            IActivityService service, 
            ICloudinaryService cloudinaryService, 
            IActivityExtractorService extractorService,
            ILogger<ActivityController> logger)
        {
            _service = service;
            _cloudinaryService = cloudinaryService;
            _extractorService = extractorService;
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

        // GET api/activity/search?searchTerm=workshop&type=Club&status=Approved&isPublic=true&clubId=1&page=1&pageSize=6
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] bool? isPublic,
            [FromQuery] int? clubId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6)
        {
            var data = await _service.SearchActivitiesAsync(searchTerm, type, status, isPublic, clubId, page, pageSize);
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
            
            // Filter AttendanceCode visibility (Requirements 6.1, 6.2, 6.3)
            // Only show AttendanceCode to Admin or Manager of the club
            if (!string.IsNullOrWhiteSpace(activity.AttendanceCode))
            {
                bool canViewCode = false;
                
                // Check if user is Admin
                if (User.IsInRole("Admin"))
                {
                    canViewCode = true;
                }
                // Check if user is Manager of the club
                else if (userId.HasValue && activity.ClubId.HasValue && User.IsInRole("ClubManager"))
                {
                    canViewCode = await _service.IsUserManagerOfClubAsync(userId.Value, activity.ClubId.Value);
                }
                
                // Hide code from students and non-authorized users
                if (!canViewCode)
                {
                    activity.AttendanceCode = null;
                }
            }
            
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

        // GET api/activity/available-clubs?excludeClubId=1
        [HttpGet("available-clubs")]
        [Authorize(Roles = "ClubManager,Admin")]
        public async Task<IActionResult> GetAvailableCollaboratingClubs([FromQuery] int excludeClubId = 0)
        {
            var clubs = await _service.GetAvailableCollaboratingClubsAsync(excludeClubId);
            return Ok(clubs);
        }

        // POST api/activity/admin
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateByAdmin([FromBody] BusinessObject.DTOs.Activity.AdminCreateActivityWithSchedulesRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Create activity
                var result = await _service.AdminCreateAsync(userId, request.Activity);
                
                // Add schedules if provided
                if (request.Schedules != null && request.Schedules.Any())
                {
                    await _service.AddSchedulesToActivityAsync(result.Id, request.Schedules);
                    
                    // Reload activity with schedules
                    result = await _service.GetActivityByIdAsync(result.Id, userId);
                }
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/activity/club-manager
        [HttpPost("club-manager")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> CreateByClubManager([FromBody] BusinessObject.DTOs.Activity.CreateActivityWithSchedulesRequest request, [FromQuery] int clubId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            if (clubId <= 0)
                return BadRequest(new { message = "Missing clubId" });

            // Log GPS config for debugging
            _logger.LogInformation("[CreateByClubManager] GPS Config received: Lat={Lat}, Lng={Lng}, Enabled={Enabled}, Radius={Radius}, CheckIn={CheckIn}, CheckOut={CheckOut}",
                request.Activity.GpsLatitude, request.Activity.GpsLongitude, request.Activity.IsGpsCheckInEnabled,
                request.Activity.GpsCheckInRadius, request.Activity.CheckInWindowMinutes, request.Activity.CheckOutWindowMinutes);

            try
            {
                // Create activity
                var result = await _service.ClubCreateAsync(userId, clubId, request.Activity);
                
                // Add schedules if provided
                if (request.Schedules != null && request.Schedules.Any())
                {
                    await _service.AddSchedulesToActivityAsync(result.Id, request.Schedules);
                    
                    // Reload activity with schedules
                    result = await _service.GetActivityByIdAsync(result.Id, userId);
                }
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/activity/admin/{id}
        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateByAdmin(int id, [FromBody] BusinessObject.DTOs.Activity.AdminUpdateActivityWithSchedulesRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Update activity
                var result = await _service.AdminUpdateAsync(userId, id, request.Activity);
                if (result == null) return NotFound(new { message = "Activity not found" });
                
                // Update schedules if provided
                if (request.Schedules != null)
                {
                    await _service.UpdateActivitySchedulesAsync(id, request.Schedules);
                    
                    // Reload activity with schedules
                    result = await _service.GetActivityByIdAsync(id, userId);
                }
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/activity/club-manager/{id}
        [HttpPut("club-manager/{id}")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> UpdateByClubManager(int id, [FromBody] BusinessObject.DTOs.Activity.UpdateActivityWithSchedulesRequest request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Update activity
                var result = await _service.ClubUpdateAsync(userId, id, request.Activity);
                if (result == null) return NotFound(new { message = "Activity not found or you don't have permission" });
                
                // Update schedules if provided
                if (request.Schedules != null)
                {
                    await _service.UpdateActivitySchedulesAsync(id, request.Schedules);
                    
                    // Reload activity with schedules
                    result = await _service.GetActivityByIdAsync(id, userId);
                }
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
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

        // POST api/activity/club-manager/{id}/complete
        [HttpPost("club-manager/{id:int}/complete")]
        [Authorize(Roles = "ClubManager")]
        public async Task<IActionResult> CompleteActivityByClubManager(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Missing user id" });

            try
            {
                // Get activity to verify user is manager of the club
                var activity = await _service.GetActivityByIdAsync(id, userId);
                if (activity == null)
                    return NotFound(new { message = "Activity not found" });

                // Verify user is manager of the activity's club (Requirement 2.1)
                if (activity.ClubId.HasValue)
                {
                    var isManager = await _service.IsUserManagerOfClubAsync(userId, activity.ClubId.Value);
                    if (!isManager)
                        return StatusCode(403, new { message = "You don't have permission to complete this activity" });
                }
                else
                {
                    // Activity has no club, ClubManager cannot complete it
                    return StatusCode(403, new { message = "You don't have permission to complete this activity" });
                }

                // Call service to complete activity (Requirement 2.1, 8.1)
                var result = await _service.CompleteActivityAsync(id, userId);
                
                if (!result.success)
                {
                    return BadRequest(new { message = result.message });
                }
                
                // Return success with point details (Requirement 8.4)
                return Ok(new 
                { 
                    message = result.message,
                    organizingClubPoints = result.organizingClubPoints,
                    collaboratingClubPoints = result.collaboratingClubPoints
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing activity {ActivityId} by ClubManager {UserId}", id, userId);
                return StatusCode(500, new { message = "An error occurred while completing the activity" });
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

	// ================= EXTRACT ACTIVITY FROM FILE (AI) =================
	// POST api/activity/extract-from-file
	[HttpPost("extract-from-file")]
	[Authorize(Roles = "ClubManager,Admin")]
	public async Task<IActionResult> ExtractActivityFromFile(IFormFile file)
	{
		try
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file provided" });

			// Max file size: 10MB
			if (file.Length > 10 * 1024 * 1024)
				return BadRequest(new { message = "File size exceeds 10MB limit" });

			_logger.LogInformation("Extracting activity from file: {FileName}, Size: {Size}", 
				file.FileName, file.Length);

			var result = await _extractorService.ExtractActivityFromFileAsync(file);
			return Ok(result);
		}
		catch (NotSupportedException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error extracting activity from file");
			return StatusCode(500, new { message = "Failed to extract activity information from file" });
		}
	}

	// ================= STUDENT SELF CHECK-IN =================
	// POST api/activity/{id}/check-in
	[HttpPost("{id:int}/check-in")]
	[Authorize] // Allow all authenticated users (Student, ClubManager, Admin can test)
	public async Task<IActionResult> CheckInWithCode(int id, [FromBody] BusinessObject.DTOs.Activity.CheckInDto dto)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		var (success, message) = await _service.CheckInWithCodeAsync(userId, id, dto.AttendanceCode);
		if (!success) 
			return BadRequest(new { message });
		
		return Ok(new { message });
	}

	// ================= UPDATE PARTICIPATION SCORE =================
	// PATCH api/activity/{activityId}/attendance/{userId}
	[HttpPatch("{activityId:int}/attendance/{targetUserId:int}")]
	[Authorize(Roles = "Admin,ClubManager")]
	public async Task<IActionResult> UpdateParticipationScore(
		int activityId, 
		int targetUserId, 
		[FromBody] BusinessObject.DTOs.Activity.UpdateScoreDto dto)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
			return Unauthorized(new { message = "Missing user id" });

		var (success, message) = await _service.UpdateParticipationScoreAsync(
			currentUserId, activityId, targetUserId, dto.ParticipationScore);
		
		if (!success) 
			return BadRequest(new { message });
		
		return Ok(new { message });
	}

	// ================= AUTO MARK ABSENT =================
	// POST api/activity/{activityId}/auto-mark-absent
	[HttpPost("{activityId:int}/auto-mark-absent")]
	[Authorize(Roles = "Admin,ClubManager")]
	public async Task<IActionResult> AutoMarkAbsent(int activityId)
	{
		try
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			int? userId = null;
			if (!string.IsNullOrWhiteSpace(userIdStr) && int.TryParse(userIdStr, out var parsedUserId))
				userId = parsedUserId;
			
			_logger.LogInformation("[AUTO MARK ABSENT] Request for activityId: {ActivityId}, userId: {UserId}", activityId, userId);
			var (markedCount, message) = await _service.AutoMarkAbsentAsync(activityId, userId);
			_logger.LogInformation("[AUTO MARK ABSENT] Success: {Message}, Count: {Count}", message, markedCount);
			return Ok(new { markedCount, message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "[AUTO MARK ABSENT] Error for activityId: {ActivityId}", activityId);
			return StatusCode(500, new { message = "Internal server error: " + ex.Message });
		}
	}

	// ================= FIX PENDING FOR COMPLETED ACTIVITIES =================
	// POST api/activity/fix-pending-completed
	// One-time migration to fix pending registrants for already completed activities
	[HttpPost("fix-pending-completed")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> FixPendingForCompletedActivities()
	{
		try
		{
			_logger.LogInformation("[FIX PENDING] Admin triggered fix for completed activities");
			var (activitiesFixed, totalMarked) = await _service.FixPendingForCompletedActivitiesAsync();
			return Ok(new { 
				activitiesFixed, 
				totalMarked, 
				message = $"Fixed {activitiesFixed} activities, marked {totalMarked} registrants as absent" 
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "[FIX PENDING] Error fixing pending for completed activities");
			return StatusCode(500, new { message = "Internal server error: " + ex.Message });
		}
	}

	// ================= ACTIVITY EVALUATION =================
	// POST api/activity/{activityId}/evaluation
	// Authorization: Only ClubManager role can create evaluations (Requirement 6.1)
	// Service layer verifies user is manager of the specific club (Requirement 6.2)
	[HttpPost("{activityId:int}/evaluation")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> CreateEvaluation(int activityId, [FromBody] BusinessObject.DTOs.Activity.CreateActivityEvaluationDto dto)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		try
		{
			var result = await _service.CreateEvaluationAsync(userId, activityId, dto);
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (UnauthorizedAccessException ex)
		{
			// Return 403 Forbidden when unauthorized user attempts to access (Requirement 6.3)
			return StatusCode(403, new { message = ex.Message });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating evaluation for activity {ActivityId}", activityId);
			return StatusCode(500, new { message = "An error occurred while creating the evaluation" });
		}
	}

	// PUT api/activity/{activityId}/evaluation
	// Authorization: Only ClubManager role can update evaluations (Requirement 6.1)
	// Service layer verifies user is manager of the specific club (Requirement 6.2)
	[HttpPut("{activityId:int}/evaluation")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> UpdateEvaluation(int activityId, [FromBody] BusinessObject.DTOs.Activity.CreateActivityEvaluationDto dto)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		try
		{
			var result = await _service.UpdateEvaluationAsync(userId, activityId, dto);
			if (result == null)
				return NotFound(new { message = "Evaluation not found" });
			
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { message = ex.Message });
		}
		catch (UnauthorizedAccessException ex)
		{
			// Return 403 Forbidden when unauthorized user attempts to access (Requirement 6.3)
			return StatusCode(403, new { message = ex.Message });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating evaluation for activity {ActivityId}", activityId);
			return StatusCode(500, new { message = "An error occurred while updating the evaluation" });
		}
	}

	// GET api/activity/{activityId}/evaluation
	// Authorization: ClubManager and Admin roles can view evaluations (Requirement 6.4)
	// ClubManager can only view evaluations for their club (Requirement 6.2)
	// Admin can view all evaluations (read-only) (Requirement 6.4)
	[HttpGet("{activityId:int}/evaluation")]
	[Authorize(Roles = "ClubManager,Admin")]
	public async Task<IActionResult> GetEvaluation(int activityId)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		try
		{
			// Check if user is Admin (Requirement 6.4: Admin can view all evaluations)
			var isAdmin = User.IsInRole("Admin");
			
			var result = await _service.GetEvaluationAsync(userId, activityId, isAdmin);
			if (result == null)
				return NotFound(new { message = "Evaluation not found" });
			
			return Ok(result);
		}
		catch (UnauthorizedAccessException ex)
		{
			// Return 403 Forbidden when unauthorized user attempts to access (Requirement 6.3)
			return StatusCode(403, new { message = ex.Message });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving evaluation for activity {ActivityId}", activityId);
			return StatusCode(500, new { message = "An error occurred while retrieving the evaluation" });
		}
	}

	// ================= COLLABORATION INVITATIONS =================
	
	// GET api/activity/collaboration-invitations
	[HttpGet("collaboration-invitations")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> GetCollaborationInvitations()
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		try
		{
			// Get user's managed club
			// This should be done through a club service, but for now we'll need to get it from activity service
			// You may need to add a method to get user's managed club ID
			// For now, assuming we can get it from the first activity or a separate call
			
			// Temporary: Get club ID from query parameter or implement proper club service
			// This is a placeholder - you should implement proper club ID retrieval
			return BadRequest(new { message = "Club ID retrieval not implemented yet. Please pass clubId as query parameter." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting collaboration invitations");
			return StatusCode(500, new { message = "Internal server error" });
		}
	}

	// GET api/activity/collaboration-invitations/count?clubId=1
	[HttpGet("collaboration-invitations/count")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> GetPendingInvitationCount([FromQuery] int clubId)
	{
		if (clubId <= 0)
			return BadRequest(new { message = "Invalid club ID" });

		try
		{
			var count = await _service.GetPendingInvitationCountAsync(clubId);
			return Ok(new { count });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting invitation count for club {ClubId}", clubId);
			return StatusCode(500, new { message = "Internal server error" });
		}
	}

	// GET api/activity/collaboration-invitations/list?clubId=1
	[HttpGet("collaboration-invitations/list")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> GetCollaborationInvitationsList([FromQuery] int clubId)
	{
		if (clubId <= 0)
			return BadRequest(new { message = "Invalid club ID" });

		try
		{
			var invitations = await _service.GetCollaborationInvitationsAsync(clubId);
			return Ok(invitations);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting invitations for club {ClubId}", clubId);
			return StatusCode(500, new { message = "Internal server error" });
		}
	}

	// POST api/activity/{activityId}/collaboration/accept?clubId=1
	[HttpPost("{activityId:int}/collaboration/accept")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> AcceptCollaboration(int activityId, [FromQuery] int clubId)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		if (clubId <= 0)
			return BadRequest(new { message = "Invalid club ID" });

		try
		{
			var (success, message) = await _service.AcceptCollaborationAsync(activityId, userId, clubId);
			if (!success)
				return BadRequest(new { message });

			return Ok(new { message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error accepting collaboration for activity {ActivityId}", activityId);
			return StatusCode(500, new { message = "Internal server error" });
		}
	}

	// POST api/activity/{activityId}/collaboration/reject?clubId=1
	[HttpPost("{activityId:int}/collaboration/reject")]
	[Authorize(Roles = "ClubManager")]
	public async Task<IActionResult> RejectCollaboration(int activityId, [FromQuery] int clubId, [FromBody] BusinessObject.DTOs.Activity.RejectCollaborationDto dto)
	{
		var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
			return Unauthorized(new { message = "Missing user id" });

		if (clubId <= 0)
			return BadRequest(new { message = "Invalid club ID" });

		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var (success, message) = await _service.RejectCollaborationAsync(activityId, userId, clubId, dto.Reason);
			if (!success)
				return BadRequest(new { message });

			return Ok(new { message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error rejecting collaboration for activity {ActivityId}", activityId);
			return StatusCode(500, new { message = "Internal server error" });
		}
	}

	// ==================== Attendance Evaluation Endpoints ====================

	public class RejectAttendanceDto
	{
		public string Reason { get; set; } = string.Empty;
	}
    }
}



