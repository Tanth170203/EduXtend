
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Clubs;

[ApiController]
[Route("api/[controller]")]
public class ClubController : ControllerBase
{
    private readonly IClubService _service;
    public ClubController(IClubService service) => _service = service;

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
}
