using System.Security.Claims;
using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/activities")]
    [Authorize(Roles = "Admin")]
    public class AdminActivitiesController : ControllerBase
    {
        private readonly IActivityService _service;

        public AdminActivitiesController(IActivityService service)
        {
            _service = service;
        }

        private int GetAdminUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr)) throw new UnauthorizedAccessException("Missing user id");
            return int.Parse(userIdStr);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminCreateActivityDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var adminId = GetAdminUserId();
            var created = await _service.AdminCreateAsync(adminId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? searchTerm, [FromQuery] string? type, [FromQuery] string? status, [FromQuery] bool? isPublic)
        {
            var items = await _service.SearchActivitiesAsync(searchTerm, type, status, isPublic, null);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetActivityByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateActivityDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var adminId = GetAdminUserId();
            var updated = await _service.AdminUpdateAsync(adminId, id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPut("{id:int}/with-schedules")]
        public async Task<IActionResult> UpdateWithSchedules(int id, [FromBody] AdminUpdateActivityWithSchedulesRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var adminId = GetAdminUserId();
            
            // Update activity
            var updated = await _service.AdminUpdateAsync(adminId, id, request.Activity);
            if (updated == null) return NotFound();
            
            // Update schedules
            await _service.UpdateActivitySchedulesAsync(id, request.Schedules);
            
            // Return updated activity with schedules
            var result = await _service.GetActivityByIdAsync(id);
            return Ok(result);
        }

		// GET api/admin/activities/{id}/registrants
		[HttpGet("{id:int}/registrants")]
		public async Task<IActionResult> GetRegistrants(int id)
		{
			var adminId = GetAdminUserId();
			var list = await _service.GetRegistrantsAsync(adminId, id);
			return Ok(list);
		}

	// POST api/admin/activities/{id}/attendance/{userId}
	[HttpPost("{id:int}/attendance/{userId:int}")]
	public async Task<IActionResult> SetAttendance(
		int id, 
		int userId, 
		[FromQuery] bool isPresent,
		[FromQuery] int? participationScore = null)
	{
		var adminId = GetAdminUserId();
		var (success, message) = await _service.SetAttendanceAsync(adminId, id, userId, isPresent, participationScore);
		if (!success) return BadRequest(new { message });
		return Ok(new { message });
	}

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.AdminDeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("available-clubs")]
        public async Task<IActionResult> GetAvailableCollaboratingClubs([FromQuery] int excludeClubId = 0)
        {
            var clubs = await _service.GetAvailableCollaboratingClubsAsync(excludeClubId);
            return Ok(clubs);
        }

		// GET api/admin/activities/{id}/feedbacks
		[HttpGet("{id:int}/feedbacks")]
		public async Task<IActionResult> GetFeedbacks(int id)
		{
			var adminId = GetAdminUserId();
			var list = await _service.GetActivityFeedbacksAsync(adminId, id);
			return Ok(list);
		}

		// POST api/admin/activities/{id}/approve
		[HttpPost("{id:int}/approve")]
		public async Task<IActionResult> Approve(int id, [FromBody] ApprovalRequest request)
		{
			var adminId = GetAdminUserId();
			var result = await _service.ApproveActivityAsync(adminId, id);
			if (result == null) return NotFound(new { message = "Activity not found" });
			return Ok(result);
		}

	// POST api/admin/activities/{id}/reject
	[HttpPost("{id:int}/reject")]
	public async Task<IActionResult> Reject(int id, [FromBody] ApprovalRequest request)
	{
		// Validate rejection reason is required
		if (string.IsNullOrWhiteSpace(request.RejectionReason))
		{
			return BadRequest(new { message = "Rejection reason is required" });
		}

		var adminId = GetAdminUserId();
		var result = await _service.RejectActivityAsync(adminId, id, request.RejectionReason);
		if (result == null) return NotFound(new { message = "Activity not found" });
		return Ok(result);
	}

	// POST api/admin/activities/{id}/complete
	[HttpPost("{id:int}/complete")]
	public async Task<IActionResult> CompleteActivity(int id)
	{
		var adminId = GetAdminUserId();
		var result = await _service.CompleteActivityAsync(id, adminId);
		
		if (!result.success)
		{
			return BadRequest(new { message = result.message });
		}
		
		return Ok(new 
		{ 
			message = result.message,
			organizingClubPoints = result.organizingClubPoints,
			collaboratingClubPoints = result.collaboratingClubPoints
		});
	}
    }

	public class ApprovalRequest
	{
		public int AdminUserId { get; set; }
		public string Action { get; set; } = string.Empty;
		public string? RejectionReason { get; set; }
	}
}



