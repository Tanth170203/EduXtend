<<<<<<< HEAD
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Students;
=======
using BusinessObject.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Students;
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414

namespace WebAPI.Controllers;

[ApiController]
[Route("api/students")]
<<<<<<< HEAD
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
=======
[Authorize(Roles = "Admin")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
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
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414
        }
    }

    /// <summary>
<<<<<<< HEAD
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
=======
    /// Get student by ID
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
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414
        }
    }

    /// <summary>
<<<<<<< HEAD
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
=======
    /// Get users with Student role but no student information
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
    /// Create student information
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
    /// Update student information
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
    /// Delete student information
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
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414
        }
    }
}

<<<<<<< HEAD
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
=======
>>>>>>> 13b7d842a613df7cf55b3363fc7fe76a1800a414
