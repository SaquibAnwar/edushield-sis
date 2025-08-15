using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Core.Enums;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/fees")]
public class FeeController : ControllerBase
{
    private readonly IFeeService _feeService;
    private readonly ILogger<FeeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public FeeController(IFeeService feeService, ILogger<FeeController> logger, IWebHostEnvironment environment)
    {
        _feeService = feeService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Get all fees
    /// </summary>
    [HttpGet]
    [Authorize]
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
            return StatusCode(500, new { error = "An error occurred while retrieving fees" });
        }
    }

    /// <summary>
    /// Get fee by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<FeeDto>> GetFeeById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var fee = await _feeService.GetFeeByIdAsync(id, cancellationToken);
            if (fee == null)
            {
                return NotFound(new { error = "Fee not found" });
            }
            return Ok(fee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fee with ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the fee" });
        }
    }

    /// <summary>
    /// Get fees by student ID
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByStudentId(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _feeService.GetFeesByStudentIdAsync(studentId, cancellationToken);
            return Ok(fees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fees for student ID: {StudentId}", studentId);
            return StatusCode(500, new { error = "An error occurred while retrieving student fees" });
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
            _logger.LogError(ex, "Error retrieving fees for type: {FeeType}", feeType);
            return StatusCode(500, new { error = "An error occurred while retrieving fees by type" });
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
            _logger.LogError(ex, "Error retrieving fees for status: {Status}", status);
            return StatusCode(500, new { error = "An error occurred while retrieving fees by status" });
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
            return StatusCode(500, new { error = "An error occurred while retrieving overdue fees" });
        }
    }

    /// <summary>
    /// Create a new fee
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateFee([FromBody] CreateFeeReq request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var feeId = await _feeService.CreateFeeAsync(request, cancellationToken);
            _logger.LogInformation("Fee created with ID: {FeeId} for student: {StudentId}", feeId, request.StudentId);
            
            return CreatedAtAction(nameof(GetFeeById), new { id = feeId }, new { id = feeId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error creating fee: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Business rule violation creating fee: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fee for student: {StudentId}", request.StudentId);
            
            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                });
            }
            
            return StatusCode(500, new { error = "An error occurred while creating the fee" });
        }
    }

    /// <summary>
    /// Update an existing fee
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateFee(Guid id, [FromBody] UpdateFeeReq request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _feeService.UpdateFeeAsync(id, request, cancellationToken);
            if (!success)
            {
                return NotFound(new { error = "Fee not found" });
            }

            _logger.LogInformation("Fee updated with ID: {FeeId}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error updating fee: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Business rule violation updating fee: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fee with ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the fee" });
        }
    }

    /// <summary>
    /// Delete a fee
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteFee(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _feeService.DeleteFeeAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { error = "Fee not found" });
            }

            _logger.LogInformation("Fee deleted with ID: {FeeId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Business rule violation deleting fee: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fee with ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the fee" });
        }
    }

    /// <summary>
    /// Record a payment for a fee
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> RecordPayment(Guid id, [FromBody] PaymentReq request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var payment = await _feeService.RecordPaymentAsync(id, request, cancellationToken);
            _logger.LogInformation("Payment recorded for fee ID: {FeeId}, Amount: {Amount}", id, request.Amount);
            
            return CreatedAtAction(nameof(GetPaymentsByFeeId), new { id }, payment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error recording payment: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Business rule violation recording payment: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for fee ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while recording the payment" });
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for fee ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving payments" });
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
            return StatusCode(500, new { error = "An error occurred while retrieving the fee summary" });
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
            return StatusCode(500, new { error = "An error occurred while retrieving student payments" });
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
                return NotFound(new { error = "Fee not found" });
            }

            _logger.LogInformation("Fee marked as paid with ID: {FeeId}", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Business rule violation marking fee as paid: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking fee as paid with ID: {FeeId}", id);
            return StatusCode(500, new { error = "An error occurred while marking the fee as paid" });
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
            return StatusCode(500, new { error = "An error occurred while updating fee statuses" });
        }
    }
}