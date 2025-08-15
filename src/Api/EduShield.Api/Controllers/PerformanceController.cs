using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/performance")]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePerformanceReq request, CancellationToken cancellationToken)
    {
        try
        {
            var performanceId = await _performanceService.CreateAsync(request, cancellationToken);
            _logger.LogInformation("Performance record created with ID: {PerformanceId}", performanceId);
            return CreatedAtAction(nameof(Get), new { id = performanceId }, performanceId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error creating performance record: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance record");
            return StatusCode(500, "An error occurred while creating the performance record");
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PerformanceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var performance = await _performanceService.GetAsync(id, cancellationToken);
            if (performance == null)
                return NotFound();

            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance record with ID: {PerformanceId}", id);
            return StatusCode(500, "An error occurred while retrieving the performance record");
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PerformanceDto>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var performances = await _performanceService.GetAllAsync(cancellationToken);
            return Ok(performances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all performance records");
            return StatusCode(500, "An error occurred while retrieving performance records");
        }
    }

    [HttpGet("student/{studentId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PerformanceDto>>> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var performances = await _performanceService.GetByStudentIdAsync(studentId, cancellationToken);
            return Ok(performances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance records for student: {StudentId}", studentId);
            return StatusCode(500, "An error occurred while retrieving student performance records");
        }
    }

    [HttpGet("faculty/{facultyId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PerformanceDto>>> GetByFaculty(Guid facultyId, CancellationToken cancellationToken)
    {
        try
        {
            var performances = await _performanceService.GetByFacultyIdAsync(facultyId, cancellationToken);
            return Ok(performances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance records for faculty: {FacultyId}", facultyId);
            return StatusCode(500, "An error occurred while retrieving faculty performance records");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Update(Guid id, [FromBody] CreatePerformanceReq request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _performanceService.UpdateAsync(id, request, cancellationToken);
            if (!success)
                return NotFound();

            _logger.LogInformation("Performance record updated with ID: {PerformanceId}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error updating performance record: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance record with ID: {PerformanceId}", id);
            return StatusCode(500, "An error occurred while updating the performance record");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _performanceService.DeleteAsync(id, cancellationToken);
            if (!success)
                return NotFound();

            _logger.LogInformation("Performance record deleted with ID: {PerformanceId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance record with ID: {PerformanceId}", id);
            return StatusCode(500, "An error occurred while deleting the performance record");
        }
    }
}