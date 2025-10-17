using BusinessObject.DTOs.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _service;

    public ActivitiesController(IActivityService service)
    {
        _service = service;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic()
        => Ok(await _service.GetPublicAsync());

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = "AllUsers")]
    public async Task<IActionResult> GetById(int id)
    {
        var a = await _service.GetByIdAsync(id);
        if (a == null) return NotFound();
        return Ok(a);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateAsAdmin([FromBody] CreateActivityDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var adminUserId)) return Unauthorized();

        var created = await _service.CreateByAdminAsync(adminUserId, dto);
        return CreatedAtAction(nameof(GetAllForAdmin), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateActivityDto dto)
        => Ok(await _service.UpdateByAdminAsync(id, dto));

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}


