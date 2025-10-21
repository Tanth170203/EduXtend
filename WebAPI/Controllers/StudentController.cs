using BusinessObject.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Students;
using Services.Students;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/students")]
public class StudentController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(IStudentRepository studentRepository, IStudentService studentService, ILogger<StudentController> logger)
    {
        _studentRepository = studentRepository;
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active students (for dropdowns in scoring modal) - Repository approach
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<StudentDropdownDto>>> GetAllActive()
    {
        try
        {
            _logger.LogInformation("📡 GET /api/students/active - Fetching all active students");
            
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
    /// Get all students - Service approach
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        try
        {
            var students = await _studentService.GetAllAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving students", error = ex.Message });
        }
    }

    /// <summary>
    /// Get student by ID (for editing score) - Repository approach
    /// </summary>
    [HttpGet("detail/{id}")]
    public async Task<ActionResult<StudentDetailDto>> GetDetailById(int id)
    {
        try
        {
            _logger.LogInformation($"📡 GET /api/students/detail/{id} - Fetching student details");
            
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
    /// Get student by ID - Service approach
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        try
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound(new { message = $"Student with ID {id} not found" });

            return Ok(student);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving student", error = ex.Message });
        }
    }

    /// <summary>
    /// Search students by name or code - Repository approach
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

    /// <summary>
    /// Get users with Student role but no student information - Service approach
    /// </summary>
    [HttpGet("users-without-info")]
    public async Task<IActionResult> GetUsersWithoutStudentInfo()
    {
        try
        {
            var users = await _studentService.GetUsersWithoutStudentInfoAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
        }
    }

    /// <summary>
    /// Create student information - Service approach
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create([FromBody] CreateStudentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _studentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error creating student", error = ex.Message });
        }
    }

    /// <summary>
    /// Update student information - Service approach
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<StudentDto>> Update(int id, [FromBody] UpdateStudentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest(new { message = "ID mismatch" });

            var student = await _studentService.UpdateAsync(id, dto);
            return Ok(student);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating student", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete student information - Service approach
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _studentService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = $"Student with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error deleting student", error = ex.Message });
        }
    }
}

/// <summary>
/// DTO for student dropdown (minimal info)
/// </summary>
public class StudentDropdownDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cohort { get; set; } = string.Empty;
}

/// <summary>
/// DTO for student detail
/// </summary>
public class StudentDetailDto
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cohort { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
