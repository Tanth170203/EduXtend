
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
