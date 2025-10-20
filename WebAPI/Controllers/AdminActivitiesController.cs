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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.AdminDeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}



