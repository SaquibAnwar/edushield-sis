using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/faculty")]
[Authorize]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _facultyService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<FacultyController> _logger;

    public FacultyController(
        IFacultyService facultyService, 
        IAuthorizationService authorizationService,
        ILogger<FacultyController> logger)
    {
        _facultyService = facultyService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFacultyReq request, CancellationToken cancellationToken)
    {
        try
        {
            var facultyId = await _facultyService.CreateAsync(request, cancellationToken);
            _logger.LogInformation("Faculty created with ID: {FacultyId}", facultyId);
            return CreatedAtAction(nameof(Get), new { id = facultyId }, facultyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating faculty");
            return StatusCode(500, "An error occurred while creating the faculty");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FacultyDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var faculty = await _facultyService.GetAsync(id, cancellationToken);
            if (faculty == null)
                return NotFound();

            // Check if user has access to this faculty record
            var authResult = await _authorizationService.AuthorizeAsync(User, faculty, "FacultyAccess");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            return Ok(faculty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving faculty with ID: {FacultyId}", id);
            return StatusCode(500, "An error occurred while retrieving the faculty");
        }
    }

    [HttpGet]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<FacultyDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var faculties = await _facultyService.GetAllAsync(cancellationToken);
            
            // Filter faculties based on user's access level
            var filteredFaculties = new List<FacultyDto>();
            foreach (var faculty in faculties)
            {
                var authResult = await _authorizationService.AuthorizeAsync(User, faculty, "FacultyAccess");
                if (authResult.Succeeded)
                {
                    filteredFaculties.Add(faculty);
                }
            }

            return Ok(filteredFaculties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all faculties");
            return StatusCode(500, "An error occurred while retrieving faculties");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<ActionResult> Update(Guid id, [FromBody] CreateFacultyReq request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _facultyService.UpdateAsync(id, request, cancellationToken);
            if (!success)
                return NotFound();

            _logger.LogInformation("Faculty updated with ID: {FacultyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating faculty with ID: {FacultyId}", id);
            return StatusCode(500, "An error occurred while updating the faculty");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _facultyService.DeleteAsync(id, cancellationToken);
            if (!success)
                return NotFound();

            _logger.LogInformation("Faculty deleted with ID: {FacultyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting faculty with ID: {FacultyId}", id);
            return StatusCode(500, "An error occurred while deleting the faculty");
        }
    }
}
