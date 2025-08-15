using EduShield.Core.Dtos;
using EduShield.Api.Infra;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/student")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;
    private readonly IWebHostEnvironment _environment;

    public StudentController(IStudentService studentService, ILogger<StudentController> logger, IWebHostEnvironment environment)
    {
        _studentService = studentService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentReq request, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _studentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetStudent), new { id }, new { id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error creating student: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student: {Error}", ex.Message);
            
            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                });
            }
            
            return StatusCode(500, new { error = "An error occurred while creating the student" });
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStudent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _studentService.GetAsync(id, cancellationToken);
            if (student == null)
            {
                return NotFound(new { error = "Student not found" });
            }
            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student with ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the student" });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllStudents(CancellationToken cancellationToken)
    {
        try
        {
            var students = await _studentService.GetAllAsync(cancellationToken);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all students");
            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                return StatusCode(500, new { error = ex.Message, details = ex.ToString(), stackTrace = ex.StackTrace });
            }
            return StatusCode(500, new { error = "An error occurred while retrieving students" });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] CreateStudentReq request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _studentService.UpdateAsync(id, request, cancellationToken);
            if (!updated)
            {
                return NotFound(new { error = "Student not found" });
            }
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error updating student: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student with ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the student" });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteStudent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _studentService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(new { error = "Student not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student with ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the student" });
        }
    }
}
