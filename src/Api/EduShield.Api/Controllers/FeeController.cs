using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Core.Enums;
using EduShield.Core.Exceptions;
using FluentValidation;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/fees")]
[Authorize]
public class FeeController : ControllerBase
{
    private readonly IFeeService _feeService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<FeeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public FeeController(
        IFeeService feeService, 
        IAuthorizationService authorizationService,
        ILogger<FeeController> logger, 
        IWebHostEnvironment environment)
    {
        _feeService = feeService;
        _authorizationService = authorizationService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get all fees
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetAllFees(CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetAllFeesAsync(cancellationToken);
            return Ok(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fees");
            return StatusCode(500, "An error occurred while retrieving fees");
        }
    }

    /// <summary>
    /// Get fee by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FeeDto>> GetFeeById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var fee = await _feeService.GetFeeByIdAsync(id, cancellationToken);
            if (fee == null)
            {
                return NotFound($"Fee with ID '{id}' was not found");
            }

            // Check if user has access to this fee
            var authResult = await _authorizationService.AuthorizeAsync(User, fee, "FeeAccess");
            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            return Ok(fee);
        }
        catch (FeeNotFoundException)
        {
            return NotFound($"Fee with ID '{id}' was not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fee with ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while retrieving the fee");
        }
    }

    /// <summary>
    /// Get fees by student ID
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByStudentId(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetFeesByStudentIdAsync(studentId, cancellationToken);
            
            // Filter fees based on user's access level
            var filteredFees = new List<FeeDto>();
            foreach (var fee in fees)
            {
                var authResult = await _authorizationService.AuthorizeAsync(User, fee, "FeeAccess");
                if (authResult.Succeeded)
                {
                    filteredFees.Add(fee);
                }
            }

            return Ok(filteredFees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fees for student ID: {StudentId}", studentId);
            return StatusCode(500, "An error occurred while retrieving student fees");
        }
    }

    /// <summary>
    /// Get fees by type
    /// </summary>
    [HttpGet("type/{feeType}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByType(FeeType feeType, CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetFeesByTypeAsync(feeType, cancellationToken);
            return Ok(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fees by type: {FeeType}", feeType);
            return StatusCode(500, "An error occurred while retrieving fees by type");
        }
    }

    /// <summary>
    /// Get fees by status
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByStatus(FeeStatus status, CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetFeesByStatusAsync(status, cancellationToken);
            return Ok(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fees by status: {Status}", status);
            return StatusCode(500, "An error occurred while retrieving fees by status");
        }
    }

    /// <summary>
    /// Get overdue fees
    /// </summary>
    [HttpGet("overdue")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetOverdueFees(CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetOverdueFeesAsync(cancellationToken);
            return Ok(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue fees");
            return StatusCode(500, "An error occurred while retrieving overdue fees");
        }
    }

    /// <summary>
    /// Create a new fee
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult<Guid>> CreateFee([FromBody] CreateFeeReq request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var feeId = await _feeService.CreateFeeAsync(request, cancellationToken);
            _logger.LogInformation("Fee created with ID: {FeeId} for student: {StudentId}", feeId, request.StudentId);
            
            return CreatedAtAction(nameof(GetFeeById), new { id = feeId }, new { id = feeId });
        }
        catch (StudentNotFoundException ex)
        {
            _logger.LogWarning(ex, "Student not found when creating fee: {StudentId}", request.StudentId);
            return BadRequest("Student not found");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating fee: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when creating fee: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fee for student: {StudentId}", request.StudentId);
            if (_environment.IsDevelopment())
            {
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
            return StatusCode(500, "An error occurred while creating the fee");
        }
    }

    /// <summary>
    /// Update an existing fee
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TeacherOrAdmin")]
    public async Task<ActionResult> UpdateFee(Guid id, [FromBody] UpdateFeeReq request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var success = await _feeService.UpdateFeeAsync(id, request, cancellationToken);
            if (!success)
            {
                return NotFound($"Fee with ID '{id}' was not found");
            }

            _logger.LogInformation("Fee updated with ID: {FeeId}", id);
            return NoContent();
        }
        catch (FeeNotFoundException)
        {
            return NotFound($"Fee with ID '{id}' was not found");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating fee: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fee with ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while updating the fee");
        }
    }

    /// <summary>
    /// Delete a fee
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SchoolAdminOnly")]
    public async Task<ActionResult> DeleteFee(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _feeService.DeleteFeeAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound($"Fee with ID '{id}' was not found");
            }

            _logger.LogInformation("Fee deleted with ID: {FeeId}", id);
            return NoContent();
        }
        catch (FeeNotFoundException)
        {
            return NotFound($"Fee with ID '{id}' was not found");
        }
        catch (FeeBusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when deleting fee: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting fee: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fee with ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while deleting the fee");
        }
    }

    /// <summary>
    /// Record a payment for a fee
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> RecordPayment(Guid id, [FromBody] PaymentReq request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var payment = await _feeService.RecordPaymentAsync(id, request, cancellationToken);
            _logger.LogInformation("Payment recorded for fee ID: {FeeId}, Amount: {Amount}", id, request.Amount);
            
            return CreatedAtAction(nameof(GetPaymentsByFeeId), new { id }, payment);
        }
        catch (FeeNotFoundException)
        {
            return NotFound($"Fee with ID '{id}' was not found");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when recording payment for fee: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when recording payment for fee: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for fee ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while recording the payment");
        }
    }

    /// <summary>
    /// Get payment history for a fee
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByFeeId(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _feeService.GetPaymentsByFeeIdAsync(id, cancellationToken);
            return Ok(payments);
        }
        catch (FeeNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for fee ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while retrieving payments");
        }
    }

    /// <summary>
    /// Get student fee summary
    /// </summary>
    [HttpGet("student/{studentId:guid}/summary")]
    [Authorize]
    public async Task<ActionResult<FeesSummaryDto>> GetStudentFeesSummary(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _feeService.GetStudentFeesSummaryAsync(studentId, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fee summary for student ID: {StudentId}", studentId);
            return StatusCode(500, "An error occurred while retrieving student fee summary");
        }
    }

    /// <summary>
    /// Get payment history for a student
    /// </summary>
    [HttpGet("student/{studentId:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByStudentId(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _feeService.GetPaymentsByStudentIdAsync(studentId, cancellationToken);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for student ID: {StudentId}", studentId);
            return StatusCode(500, "An error occurred while retrieving student payments");
        }
    }

    /// <summary>
    /// Mark a fee as paid (administrative action)
    /// </summary>
    [HttpPatch("{id:guid}/mark-paid")]
    [Authorize]
    public async Task<ActionResult> MarkFeeAsPaid(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _feeService.MarkFeeAsPaidAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound($"Fee with ID '{id}' was not found");
            }

            _logger.LogInformation("Fee marked as paid with ID: {FeeId}", id);
            return NoContent();
        }
        catch (FeeNotFoundException)
        {
            return NotFound($"Fee with ID '{id}' was not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when marking fee as paid: {FeeId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking fee as paid with ID: {FeeId}", id);
            return StatusCode(500, "An error occurred while marking the fee as paid");
        }
    }

    /// <summary>
    /// Update fee statuses (administrative maintenance action)
    /// </summary>
    [HttpPost("update-statuses")]
    [Authorize]
    public async Task<ActionResult> UpdateFeeStatuses(CancellationToken cancellationToken)
    {
        try
        {
            await _feeService.UpdateFeeStatusesAsync(cancellationToken);
            _logger.LogInformation("Fee statuses updated successfully");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fee statuses");
            return StatusCode(500, "An error occurred while updating fee statuses");
        }
    }
}