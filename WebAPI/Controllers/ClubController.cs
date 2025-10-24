
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Clubs;
using System.Security.Claims;
using BusinessObject.DTOs;
using System.Linq;
using BusinessObject.DTOs.Club;

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

    // GET api/club/paged?page=1&pageSize=9&sortBy=az
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 9, [FromQuery] string? sortBy = null)
    {
        var list = await _service.GetAllClubsAsync();
        list = SortClubs(list, sortBy);
        return Ok(BuildPaged(list, page, pageSize));
    }

    // GET api/club/search-paged?searchTerm=...&categoryName=...&isActive=true&page=1&pageSize=9&sortBy=az
    [HttpGet("search-paged")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPaged(
        [FromQuery] string? searchTerm,
        [FromQuery] string? categoryName,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 9,
        [FromQuery] string? sortBy = null)
    {
        var list = await _service.SearchClubsAsync(searchTerm, categoryName, isActive);
        list = SortClubs(list, sortBy);
        return Ok(BuildPaged(list, page, pageSize));
    }

    private static List<BusinessObject.DTOs.Club.ClubListItemDto> SortClubs(List<BusinessObject.DTOs.Club.ClubListItemDto> clubs, string? sortBy)
    {
        return sortBy switch
        {
            "newest" => clubs.OrderByDescending(c => c.FoundedDate).ToList(),
            "members" => clubs.OrderByDescending(c => c.MemberCount).ToList(),
            "az" => clubs.OrderBy(c => c.Name).ToList(),
            _ => clubs.OrderBy(c => c.Name).ToList()
        };
    }

    private static PagedResult<T> BuildPaged<T>(List<T> source, int page, int pageSize)
    {
        if (pageSize <= 0) pageSize = 9;
        if (page <= 0) page = 1;
        var totalItems = source.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
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

    // GET api/club/my
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyClubs()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user ID" });

        var list = await _service.GetMyClubsAsync(userId);
        return Ok(list);
    }

    // GET api/club/{clubId}/members
    [HttpGet("{clubId:int}/members")]
    [Authorize]
    public async Task<IActionResult> GetMembers(int clubId)
    {
        var members = await _service.GetClubMembersAsync(clubId);
        return Ok(members);
    }

    // POST api/club/{clubId}/leave
    [HttpPost("{clubId:int}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveClub(int clubId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid user ID" });

        var ok = await _service.LeaveClubAsync(userId, clubId);
        if (!ok) return BadRequest(new { message = "You are not an active member of this club" });
        return Ok(new { message = "Left club successfully" });
    }

    // GET api/club/{clubId}/members/manage
    [HttpGet("{clubId:int}/members/manage")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> GetMembersManage(int clubId)
    {
        var list = await _service.GetMembersForManageAsync(clubId);
        return Ok(list);
    }

    // PUT api/club/{clubId}/members/{studentId}/role
    [HttpPut("{clubId:int}/members/{studentId:int}/role")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> UpdateMemberRole(int clubId, int studentId, [FromBody] string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return BadRequest(new { message = "Role is required" });
        var ok = await _service.UpdateMemberRoleAsync(clubId, studentId, role);
        if (!ok) return NotFound(new { message = "Member not found" });
        return Ok(new { message = "Role updated" });
    }

    // DELETE api/club/{clubId}/members/{studentId}
    [HttpDelete("{clubId:int}/members/{studentId:int}")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> RemoveMember(int clubId, int studentId)
    {
        var ok = await _service.RemoveMemberAsync(clubId, studentId);
        if (!ok) return NotFound(new { message = "Member not found" });
        return Ok(new { message = "Member removed" });
    }

    // PUT api/club/{clubId}
    [HttpPut("{clubId:int}")]
    [Authorize(Roles = "ClubManager")]
    public async Task<IActionResult> UpdateClubInfo(int clubId, [FromBody] UpdateClubInfoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ok = await _service.UpdateClubInfoAsync(clubId, dto);
        if (!ok) return NotFound(new { message = "Club not found" });
        return Ok(new { message = "Club updated" });
    }

    // GET api/club/categories-lite
    [HttpGet("categories-lite")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoriesLite()
    {
        var items = await _service.GetAllCategoriesAsyncLite();
        return Ok(items);
    }
}
