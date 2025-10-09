using BusinessObject.DTOs.Student;
using BusinessObject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Students;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        /// <summary>
        /// Get all students
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                var studentDtos = students.Select(s => MapToDto(s)).ToList();

                return Ok(new { success = true, data = studentDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get student by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound(new { success = false, message = "Student not found" });
                }

                return Ok(new { success = true, data = MapToDto(student) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get users without student profile (for dropdown)
        /// </summary>
        [HttpGet("available-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAvailableUsers()
        {
            try
            {
                var users = await _studentService.GetUsersWithoutStudentProfileAsync();
                var userDtos = users.Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                }).ToList();

                return Ok(new { success = true, data = userDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create new student
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var student = new Student
                {
                    UserId = dto.UserId,
                    StudentCode = dto.StudentCode,
                    FullName = dto.FullName,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    EnrollmentDate = dto.EnrollmentDate,
                    Status = dto.Status,
                    MajorId = dto.MajorId
                };

                var success = await _studentService.CreateStudentAsync(student);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Failed to create student. Student code may already exist or user already has a student profile." });
                }

                return Ok(new { success = true, message = "Student created successfully", data = MapToDto(student) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update student
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateStudentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound(new { success = false, message = "Student not found" });
                }

                student.StudentCode = dto.StudentCode;
                student.FullName = dto.FullName;
                student.DateOfBirth = dto.DateOfBirth;
                student.Gender = dto.Gender;
                student.Email = dto.Email;
                student.Phone = dto.Phone;
                student.EnrollmentDate = dto.EnrollmentDate;
                student.Status = dto.Status;
                student.MajorId = dto.MajorId;

                var success = await _studentService.UpdateStudentAsync(student);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Failed to update student" });
                }

                return Ok(new { success = true, message = "Student updated successfully", data = MapToDto(student) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete student
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _studentService.DeleteStudentAsync(id);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Student not found" });
                }

                return Ok(new { success = true, message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private StudentDto MapToDto(Student student)
        {
            return new StudentDto
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Email = student.Email,
                Phone = student.Phone,
                EnrollmentDate = student.EnrollmentDate,
                Status = student.Status,
                UserId = student.UserId,
                MajorId = student.MajorId,
                User = student.User != null ? new UserInfoDto
                {
                    Id = student.User.Id,
                    FullName = student.User.FullName,
                    Email = student.User.Email,
                    AvatarUrl = student.User.AvatarUrl,
                    IsActive = student.User.IsActive
                } : null,
                Major = student.Major != null ? new MajorDto
                {
                    Id = student.Major.Id,
                    Code = student.Major.Code,
                    Name = student.Major.Name
                } : null
            };
        }
    }
}

