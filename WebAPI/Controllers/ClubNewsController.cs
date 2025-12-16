using BusinessObject.DTOs.Club;
using BusinessObject.DTOs.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.ClubNews;
using Services.Clubs;
using Services.Evidences;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/club-news")]
public class ClubNewsController : ControllerBase
{
	private readonly IClubNewsService _service;
	private readonly IClubService _clubService;
	private readonly ICloudinaryService _cloudinaryService;
	private readonly ILogger<ClubNewsController> _logger;

	public ClubNewsController(
		IClubNewsService service,
		IClubService clubService,
		ICloudinaryService cloudinaryService,
		ILogger<ClubNewsController> logger)
	{
		_service = service;
		_clubService = clubService;
		_cloudinaryService = cloudinaryService;
		_logger = logger;
	}

	/// <summary>
	/// Get all club news (public can see approved only, admin can see all)
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<ClubNewsListItemDto>>> GetAll(
		[FromQuery] int? clubId = null,
		[FromQuery] bool? approvedOnly = null)
	{
		// Public users can only see approved news
		var isAdmin = User.IsInRole("Admin");
		var filterApproved = approvedOnly ?? !isAdmin;
		
		var items = await _service.GetAllAsync(clubId, filterApproved);
		return Ok(items);
	}

	/// <summary>
	/// Get pending approval news (Admin only)
	/// </summary>
	[HttpGet("pending")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<IEnumerable<ClubNewsListItemDto>>> GetPendingApproval()
	{
		var items = await _service.GetPendingApprovalAsync();
		return Ok(items);
	}

	/// <summary>
	/// Get all clubs managed by current user (Club Manager only)
	/// </summary>
	[HttpGet("managed-clubs")]
	[Authorize(Roles = "ClubManager")]
	public async Task<ActionResult<IEnumerable<ClubListItemDto>>> GetManagedClubs()
	{
		var userId = GetCurrentUserId();
		if (userId == null) return Unauthorized();
		
		var clubs = await _clubService.GetAllManagedClubsByUserIdAsync(userId.Value);
		return Ok(clubs);
	}

	/// <summary>
	/// Get club news by ID
	/// </summary>
	[HttpGet("{id}")]
	public async Task<ActionResult<ClubNewsDetailDto>> GetById(int id)
	{
		var item = await _service.GetByIdAsync(id);
		if (item == null) return NotFound();
		
		// Public users can only see approved news
		if (!item.IsApproved && !User.IsInRole("Admin"))
		{
			var userId = GetCurrentUserId();
			// Allow creator to see their own pending news
			if (userId != item.CreatedById)
			{
				return NotFound();
			}
		}
		
		return Ok(item);
	}

	/// <summary>
	/// Create club news (Club Manager only)
	/// </summary>
	[HttpPost]
	[Authorize(Roles = "ClubManager")]
	public async Task<ActionResult<ClubNewsDetailDto>> Create([FromBody] CreateClubNewsRequest request, [FromQuery] int clubId)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		
		var userId = GetCurrentUserId();
		if (userId == null) return Unauthorized();
		
		if (clubId <= 0)
			return BadRequest(new { message = "Missing clubId" });
		
		// Verify user is manager of the specified club
		var managedClubs = await _clubService.GetAllManagedClubsByUserIdAsync(userId.Value);
		if (!managedClubs.Any(c => c.Id == clubId))
		{
			return Forbid("You are not managing the specified club");
		}
		
		try
		{
			var created = await _service.CreateAsync(userId.Value, clubId, request);
			_logger.LogInformation("Club Manager {UserId} created news {NewsId} for club {ClubId}", 
				userId, created.Id, clubId);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(ex.Message);
		}
	}

	/// <summary>
	/// Update club news (Creator or Admin only)
	/// </summary>
	[HttpPut("{id}")]
	[Authorize]
	public async Task<ActionResult<ClubNewsDetailDto>> Update(int id, [FromBody] UpdateClubNewsRequest request)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		
		var userId = GetCurrentUserId();
		if (userId == null) return Unauthorized();
		
		try
		{
			var existing = await _service.GetByIdAsync(id);
			if (existing == null) return NotFound();
			
			// Only creator or admin can update
			var isAdmin = User.IsInRole("Admin");
			if (!isAdmin && existing.CreatedById != userId.Value)
			{
				return Forbid("You can only update your own news");
			}
			
			// If already approved, only admin can update
			if (existing.IsApproved && !isAdmin)
			{
				return Forbid("Cannot update approved news. Please contact admin.");
			}
			
			var updated = await _service.UpdateAsync(id, userId.Value, request);
			_logger.LogInformation("User {UserId} updated club news {NewsId}", userId, id);
			return Ok(updated);
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
	}

	/// <summary>
	/// Approve or reject club news (Admin only)
	/// </summary>
	[HttpPost("{id}/approve")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ClubNewsDetailDto>> Approve(int id, [FromBody] ApproveClubNewsRequest request)
	{
		try
		{
			var updated = await _service.ApproveAsync(id, request.Approve);
			var action = request.Approve ? "approved" : "rejected";
			_logger.LogInformation("Admin approved club news {NewsId}: {Action}", id, action);
			return Ok(updated);
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
	}

	/// <summary>
	/// Delete club news (Creator or Admin only)
	/// </summary>
	[HttpDelete("{id}")]
	[Authorize]
	public async Task<IActionResult> Delete(int id)
	{
		var userId = GetCurrentUserId();
		if (userId == null) return Unauthorized();
		
		var isAdmin = User.IsInRole("Admin");
		
		try
		{
			var ok = await _service.DeleteAsync(id, userId.Value, isAdmin);
			if (!ok) return NotFound();
			
			_logger.LogInformation("User {UserId} deleted club news {NewsId}", userId, id);
			return NoContent();
		}
		catch (UnauthorizedAccessException ex)
		{
			return Forbid(ex.Message);
		}
	}

	/// <summary>
	/// Upload image for club news
	/// </summary>
	[HttpPost("upload-image")]
	[Authorize(Roles = "ClubManager,Admin")]
	public async Task<IActionResult> UploadNewsImage(IFormFile file)
	{
		try
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file provided" });

			var url = await _cloudinaryService.UploadNewsImageAsync(file);
			_logger.LogInformation("Uploaded club news image to Cloudinary: {Url}", url);
			return Ok(new { url });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading club news image");
			return BadRequest(new { message = ex.Message });
		}
	}

	private int? GetCurrentUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (userIdClaim != null && int.TryParse(userIdClaim, out var id))
		{
			return id;
		}
		return null;
	}
}
