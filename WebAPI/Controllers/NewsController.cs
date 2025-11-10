using BusinessObject.DTOs.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Evidences;
using Services.News;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
	private readonly INewsService _service;
	private readonly ICloudinaryService _cloudinaryService;
	private readonly ILogger<NewsController> _logger;

	public NewsController(INewsService service, ICloudinaryService cloudinaryService, ILogger<NewsController> logger)
	{
		_service = service;
		_cloudinaryService = cloudinaryService;
		_logger = logger;
	}

	/// Public list of published news
	[HttpGet]
	public async Task<ActionResult<IEnumerable<NewsListItemDto>>> GetAll([FromQuery] bool publishedOnly = true)
	{
		var items = await _service.GetAllAsync(publishedOnly);
		return Ok(items);
	}

	/// Public details (only published). Admins can pass includeUnpublished=true to view drafts
	[HttpGet("{id}")]
	public async Task<ActionResult<NewsDetailDto>> GetById(int id, [FromQuery] bool includeUnpublished = false)
	{
		var allowUnpublished = includeUnpublished && User.IsInRole("Admin");
		var item = await _service.GetByIdAsync(id, allowUnpublished);
		if (item == null) return NotFound();
		return Ok(item);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<NewsDetailDto>> Create([FromBody] CreateNewsRequest request)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrWhiteSpace(userIdStr)) return Unauthorized();
		var created = await _service.CreateAsync(int.Parse(userIdStr), request);
		return CreatedAtAction(nameof(GetById), new { id = created.Id, includeUnpublished = true }, created);
	}

	[HttpPut("{id}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<NewsDetailDto>> Update(int id, [FromBody] UpdateNewsRequest request)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		try
		{
			var updated = await _service.UpdateAsync(id, request);
			return Ok(updated);
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
	}

	[HttpPost("{id}/publish")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<NewsDetailDto>> Publish(int id, [FromBody] PublishNewsRequest request)
	{
		var updated = await _service.PublishAsync(id, request.Publish);
		return Ok(updated);
	}

	[HttpDelete("{id}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(int id)
	{
		var ok = await _service.DeleteAsync(id);
		if (!ok) return NotFound();
		return NoContent();
	}

	// ================= NEWS IMAGE UPLOAD =================
	// POST api/news/upload-image
	[HttpPost("upload-image")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UploadNewsImage(IFormFile file)
	{
		try
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file provided" });

			// Upload to "news" folder on Cloudinary (root level)
			var url = await _cloudinaryService.UploadNewsImageAsync(file);
			_logger.LogInformation("Uploaded news image to Cloudinary: {Url}", url);
			return Ok(new { url });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading news image");
			return BadRequest(new { message = ex.Message });
		}
	}
}


