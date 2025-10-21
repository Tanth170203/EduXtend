using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Students;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentRepository studentRepository, ILogger<StudentController> logger)
    {
        _studentRepository = studentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active students (for dropdowns in scoring modal)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDropdownDto>>> GetAllActive()
    {
        try
        {
            _logger.LogInformation("📡 GET /api/students - Fetching all active students");
            
            // Query students from context through repository pattern
            var students = await _studentRepository.GetAllActiveStudentsAsync();
            
            _logger.LogInformation($"✅ Found {students.Count} active students");
            
            var dtos = students.Select(s => new StudentDropdownDto
            {
                Id = s.Id,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                Email = s.User?.Email ?? "",
                Cohort = s.Cohort
            }).OrderBy(s => s.FullName).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving active students");
            return StatusCode(500, new { message = "Error retrieving student list." });
        }
    }

    /// <summary>
    /// Get student by ID (for editing score)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDetailDto>> GetById(int id)
    {
        try
        {
            _logger.LogInformation($"📡 GET /api/students/{id} - Fetching student details");
            
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return NotFound(new { message = $"Student with ID {id} not found." });

            var dto = new StudentDetailDto
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                Email = student.User?.Email ?? "",
                Cohort = student.Cohort,
                Phone = student.Phone,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender.ToString(),
                Status = student.Status.ToString()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving student");
            return StatusCode(500, new { message = "Error retrieving student information." });
        }
    }

    /// <summary>
    /// Search students by name or code
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<StudentDropdownDto>>> Search([FromQuery] string query)
    {
        try
        {
            _logger.LogInformation($"📡 GET /api/students/search?query={query}");
            
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required." });

            var students = await _studentRepository.SearchStudentsAsync(query);
            
            _logger.LogInformation($"✅ Found {students.Count} students matching query: {query}");
            
            var dtos = students
                .Where(s => s.Status.ToString() == "Active") // Only active
                .Select(s => new StudentDropdownDto
                {
                    Id = s.Id,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Email = s.User?.Email ?? "",
                    Cohort = s.Cohort
                })
                .OrderBy(s => s.FullName)
                .ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error searching students");
            return StatusCode(500, new { message = "Error searching students." });
        }
    }
}

/// <summary>
/// DTO for student dropdown (minimal info)
/// </summary>
public class StudentDropdownDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Cohort { get; set; }
}

/// <summary>
/// DTO for student detail
/// </summary>
public class StudentDetailDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Cohort { get; set; }
    public string Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string Status { get; set; }
}
