
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Clubs;
using Services.Evidences;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ClubController : ControllerBase
{
    private readonly IClubService _service;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ClubController> _logger;
    
    public ClubController(IClubService service, ICloudinaryService cloudinaryService, ILogger<ClubController> logger)
    {
        _service = service;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    // GET api/club
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var data = await _service.GetAllClubsAsync(userId);
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
        var userId = GetCurrentUserId();
        var data = await _service.SearchClubsAsync(searchTerm, categoryName, isActive, userId);
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

    // GET api/club/my-managed-clubs
    [HttpGet("my-managed-clubs")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> GetMyManagedClubs()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            var clubs = await _service.GetAllManagedClubsByUserIdAsync(userId);
            return Ok(clubs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting managed clubs");
            return StatusCode(500, new { message = "Failed to retrieve clubs information" });
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

    // GET api/club/{clubId}/is-member
    [HttpGet("{clubId:int}/is-member")]
    [Authorize]
    public async Task<IActionResult> IsUserMemberOfClub(int clubId)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            var isMember = await _service.IsUserMemberOfClubAsync(userId, clubId);
            return Ok(new { isMember });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is member of club {ClubId}", clubId);
            return StatusCode(500, new { message = "Failed to check membership status" });
        }
    }

    // GET api/club/my-clubs
    [HttpGet("my-clubs")]
    [Authorize]
    public async Task<IActionResult> GetMyClubs()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            var clubs = await _service.GetClubsByUserIdAsync(userId);
            return Ok(clubs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clubs for user");
            return StatusCode(500, new { message = "Failed to retrieve clubs" });
        }
    }

    // GET api/club/{clubId}/members
    [HttpGet("{clubId:int}/members")]
    [Authorize]
    public async Task<IActionResult> GetClubMembers(int clubId)
    {
        try
        {
            var members = await _service.GetClubMembersAsync(clubId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for club {ClubId}", clubId);
            return StatusCode(500, new { message = "Failed to retrieve members" });
        }
    }

    // GET api/club/my-club-members
    [HttpGet("my-club-members")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> GetMyClubMembers()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            // Get the club managed by this user
            var club = await _service.GetManagedClubByUserIdAsync(userId);
            if (club == null)
            {
                return NotFound(new { message = "You are not managing any club" });
            }

            // Get members of that club
            var members = await _service.GetClubMembersAsync(club.Id);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for managed club");
            return StatusCode(500, new { message = "Failed to retrieve members" });
        }
    }

    // GET api/club/{clubId}/departments
    [HttpGet("{clubId:int}/departments")]
    [Authorize]
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

    // GET api/club/{clubId}/awards
    [HttpGet("{clubId:int}/awards")]
    public async Task<IActionResult> GetClubAwards(int clubId)
    {
        try
        {
            var awards = await _service.GetClubAwardsAsync(clubId);
            return Ok(awards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting awards for club {ClubId}", clubId);
            return StatusCode(500, new { message = "Failed to retrieve awards" });
        }
    }

    // ================= CLUB IMAGE UPLOAD =================
    // POST api/club/upload-image
    [HttpPost("upload-image")]
    [Authorize]
    public async Task<IActionResult> UploadClubImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            // Upload to "clubs" folder on Cloudinary
            var url = await _cloudinaryService.UploadClubImageAsync(file);
            _logger.LogInformation("Uploaded club image to Cloudinary: {Url}", url);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading club image");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ================= CLUB INFO UPDATE =================
    // PUT api/club/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> UpdateClubInfo(int id, [FromBody] BusinessObject.DTOs.Club.UpdateClubInfoDto dto)
    {
        try
        {
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var result = await _service.UpdateClubInfoAsync(id, dto);
            if (result == null)
            {
                return NotFound(new { message = "Club not found" });
            }

            _logger.LogInformation("Updated club info for ClubId={ClubId}", id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating club info for ClubId={ClubId}", id);
            return StatusCode(500, new { message = "Failed to update club information" });
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
