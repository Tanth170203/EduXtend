using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Activities;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _service;
        public ActivityController(IActivityService service) => _service = service;

        // GET api/activity
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllActivitiesAsync();
            return Ok(data);
        }

        // GET api/activity/search?searchTerm=workshop&type=Workshop&status=Approved&isPublic=true&clubId=1
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchTerm,
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] bool? isPublic,
            [FromQuery] int? clubId)
        {
            var data = await _service.SearchActivitiesAsync(searchTerm, type, status, isPublic, clubId);
            return Ok(data);
        }

        // GET api/activity/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var activity = await _service.GetActivityByIdAsync(id);
            if (activity == null) return NotFound();
            return Ok(activity);
        }

        // GET api/activity/club/{clubId}
        [HttpGet("club/{clubId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByClubId(int clubId)
        {
            var activities = await _service.GetActivitiesByClubIdAsync(clubId);
            return Ok(activities);
        }
    }
}

