using Microsoft.AspNetCore.Mvc;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/faculty")]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _facultyService;
    private readonly ILogger<FacultyController> _logger;

    public FacultyController(IFacultyService facultyService, ILogger<FacultyController> logger)
    {
        _facultyService = facultyService;
        _logger = logger;
    }

    [HttpPost]
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

            return Ok(faculty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving faculty with ID: {FacultyId}", id);
            return StatusCode(500, "An error occurred while retrieving the faculty");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FacultyDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var faculties = await _facultyService.GetAllAsync(cancellationToken);
            return Ok(faculties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all faculties");
            return StatusCode(500, "An error occurred while retrieving faculties");
        }
    }

    [HttpPut("{id:guid}")]
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
