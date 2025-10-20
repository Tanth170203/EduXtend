using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Majors;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/majors")]
[Authorize(Roles = "Admin")]
public class MajorController : ControllerBase
{
    private readonly IMajorRepository _majorRepo;

    public MajorController(IMajorRepository majorRepo)
    {
        _majorRepo = majorRepo;
    }

    /// <summary>
    /// Get all active majors
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var majors = await _majorRepo.GetAllAsync();
            return Ok(majors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving majors", error = ex.Message });
        }
    }
}

