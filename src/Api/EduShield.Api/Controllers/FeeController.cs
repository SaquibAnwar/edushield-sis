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
        var fees = await _feeService.GetAllFeesAsync(cancellationToken);
        return Ok(fees);
    }

    /// <summary>
    /// Get fee by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<FeeDto>> GetFeeById(Guid id, CancellationToken cancellationToken)
    {
        var fee = await _feeService.GetFeeByIdAsync(id, cancellationToken);
        if (fee == null)
        {
            throw new FeeNotFoundException(id);
        }
        return Ok(fee);
    }

    /// <summary>
    /// Get fees by student ID
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByStudentId(Guid studentId, CancellationToken cancellationToken)
    {
        var fees = await _feeService.GetFeesByStudentIdAsync(studentId, cancellationToken);
        return Ok(fees);
    }

    /// <summary>
    /// Get fees by type
    /// </summary>
    [HttpGet("type/{feeType}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByType(FeeType feeType, CancellationToken cancellationToken)
    {
        var fees = await _feeService.GetFeesByTypeAsync(feeType, cancellationToken);
        return Ok(fees);
    }

    /// <summary>
    /// Get fees by status
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByStatus(FeeStatus status, CancellationToken cancellationToken)
    {
        var fees = await _feeService.GetFeesByStatusAsync(status, cancellationToken);
        return Ok(fees);
    }

    /// <summary>
    /// Get overdue fees
    /// </summary>
    [HttpGet("overdue")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetOverdueFees(CancellationToken cancellationToken)
    {
        var fees = await _feeService.GetOverdueFeesAsync(cancellationToken);
        return Ok(fees);
    }

    /// <summary>
    /// Create a new fee
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateFee([FromBody] CreateFeeReq request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var feeId = await _feeService.CreateFeeAsync(request, cancellationToken);
        _logger.LogInformation("Fee created with ID: {FeeId} for student: {StudentId}", feeId, request.StudentId);
        
        return CreatedAtAction(nameof(GetFeeById), new { id = feeId }, new { id = feeId });
    }

    /// <summary>
    /// Update an existing fee
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateFee(Guid id, [FromBody] UpdateFeeReq request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _feeService.UpdateFeeAsync(id, request, cancellationToken);
        if (!success)
        {
            throw new FeeNotFoundException(id);
        }

        _logger.LogInformation("Fee updated with ID: {FeeId}", id);
        return NoContent();
    }

    /// <summary>
    /// Delete a fee
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteFee(Guid id, CancellationToken cancellationToken)
    {
        var success = await _feeService.DeleteFeeAsync(id, cancellationToken);
        if (!success)
        {
            throw new FeeNotFoundException(id);
        }

        _logger.LogInformation("Fee deleted with ID: {FeeId}", id);
        return NoContent();
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

        var payment = await _feeService.RecordPaymentAsync(id, request, cancellationToken);
        _logger.LogInformation("Payment recorded for fee ID: {FeeId}, Amount: {Amount}", id, request.Amount);
        
        return CreatedAtAction(nameof(GetPaymentsByFeeId), new { id }, payment);
    }

    /// <summary>
    /// Get payment history for a fee
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByFeeId(Guid id, CancellationToken cancellationToken)
    {
        var payments = await _feeService.GetPaymentsByFeeIdAsync(id, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// Get student fee summary
    /// </summary>
    [HttpGet("student/{studentId:guid}/summary")]
    [Authorize]
    public async Task<ActionResult<FeesSummaryDto>> GetStudentFeesSummary(Guid studentId, CancellationToken cancellationToken)
    {
        var summary = await _feeService.GetStudentFeesSummaryAsync(studentId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Get payment history for a student
    /// </summary>
    [HttpGet("student/{studentId:guid}/payments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPaymentsByStudentId(Guid studentId, CancellationToken cancellationToken)
    {
        var payments = await _feeService.GetPaymentsByStudentIdAsync(studentId, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// Mark a fee as paid (administrative action)
    /// </summary>
    [HttpPatch("{id:guid}/mark-paid")]
    [Authorize]
    public async Task<ActionResult> MarkFeeAsPaid(Guid id, CancellationToken cancellationToken)
    {
        var success = await _feeService.MarkFeeAsPaidAsync(id, cancellationToken);
        if (!success)
        {
            throw new FeeNotFoundException(id);
        }

        _logger.LogInformation("Fee marked as paid with ID: {FeeId}", id);
        return NoContent();
    }

    /// <summary>
    /// Update fee statuses (administrative maintenance action)
    /// </summary>
    [HttpPost("update-statuses")]
    [Authorize]
    public async Task<ActionResult> UpdateFeeStatuses(CancellationToken cancellationToken)
    {
        await _feeService.UpdateFeeStatusesAsync(cancellationToken);
        _logger.LogInformation("Fee statuses updated successfully");
        return NoContent();
    }
}