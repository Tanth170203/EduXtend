
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Clubs;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ClubController : ControllerBase
{
    private readonly IClubService _service;
    private readonly ILogger<ClubController> _logger;
    
    public ClubController(IClubService service, ILogger<ClubController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET api/club
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllClubsAsync();
        return Ok(data);
    }

    // GET api/club/search?searchTerm=tech&categoryName=Technology&isActive=true
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm, 
        [FromQuery] string? categoryName, 
        [FromQuery] bool? isActive)
    {
        var data = await _service.SearchClubsAsync(searchTerm, categoryName, isActive);
        return Ok(data);
    }

    // GET api/club/categories
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _service.GetAllCategoryNamesAsync();
        return Ok(categories);
    }

    // GET api/club/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var club = await _service.GetClubByIdAsync(id);
        if (club == null) return NotFound();
        return Ok(club);
    }

    // GET api/club/my-managed-club
    [HttpGet("my-managed-club")]
    [Authorize]
    public async Task<IActionResult> GetMyManagedClub()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            var club = await _service.GetManagedClubByUserIdAsync(userId);
            if (club == null)
            {
                return NotFound(new { message = "You are not managing any club" });
            }

            return Ok(club);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting managed club");
            return StatusCode(500, new { message = "Failed to retrieve club information" });
        }
    }

    // POST api/club/{clubId}/toggle-recruitment
    [HttpPost("{clubId:int}/toggle-recruitment")]
    [Authorize]
    public async Task<IActionResult> ToggleRecruitment(int clubId, [FromBody] BusinessObject.DTOs.Club.ToggleRecruitmentDto dto)
    {
        try
        {
            var success = await _service.ToggleRecruitmentAsync(clubId, dto.IsOpen);
            if (!success)
            {
                return NotFound(new { message = "Club not found" });
            }

            var status = dto.IsOpen ? "opened" : "closed";
            _logger.LogInformation("Club {ClubId} recruitment {Status}", clubId, status);

            return Ok(new { message = $"Recruitment {status} successfully", isOpen = dto.IsOpen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling recruitment for club {ClubId}", clubId);
            return StatusCode(500, new { message = "Failed to update recruitment status" });
        }
    }

    // GET api/club/{clubId}/recruitment-status
    [HttpGet("{clubId:int}/recruitment-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecruitmentStatus(int clubId)
    {
        try
        {
            var status = await _service.GetRecruitmentStatusAsync(clubId);
            if (status == null)
            {
                return NotFound(new { message = "Club not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recruitment status for club {ClubId}", clubId);
            return StatusCode(500, new { message = "Failed to retrieve recruitment status" });
        }
    }
}
