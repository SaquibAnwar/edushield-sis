using AutoMapper;
using FluentValidation;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using EduShield.Core.Validators;
using EduShield.Core.Exceptions;

namespace EduShield.Api.Services;

public class FeeService : IFeeService
{
    private readonly IFeeRepo _feeRepo;
    private readonly IStudentRepo _studentRepo;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateFeeReq> _createFeeValidator;
    private readonly IValidator<UpdateFeeReq> _updateFeeValidator;
    private readonly IValidator<PaymentReq> _paymentValidator;
    private readonly IValidator<PaymentValidationContext> _paymentBusinessValidator;
    private readonly IValidator<UpdateFeeValidationContext> _updateFeeBusinessValidator;

    public FeeService(
        IFeeRepo feeRepo,
        IStudentRepo studentRepo,
        IMapper mapper,
        IValidator<CreateFeeReq> createFeeValidator,
        IValidator<UpdateFeeReq> updateFeeValidator,
        IValidator<PaymentReq> paymentValidator,
        IValidator<PaymentValidationContext> paymentBusinessValidator,
        IValidator<UpdateFeeValidationContext> updateFeeBusinessValidator)
    {
        _feeRepo = feeRepo;
        _studentRepo = studentRepo;
        _mapper = mapper;
        _createFeeValidator = createFeeValidator;
        _updateFeeValidator = updateFeeValidator;
        _paymentValidator = paymentValidator;
        _paymentBusinessValidator = paymentBusinessValidator;
        _updateFeeBusinessValidator = updateFeeBusinessValidator;
    }

    public async Task<Guid> CreateFeeAsync(CreateFeeReq request, CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await _createFeeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verify student exists
        var student = await _studentRepo.GetByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            throw new StudentNotFoundException(request.StudentId);
        }

        // Create fee entity
        var fee = _mapper.Map<Fee>(request);
        fee.FeeId = Guid.NewGuid();
        fee.PaidAmount = 0;
        fee.Status = FeeStatus.Pending;
        fee.IsPaid = false;
        fee.CreatedAt = DateTime.UtcNow;
        fee.UpdatedAt = DateTime.UtcNow;

        // Save fee
        var createdFee = await _feeRepo.CreateAsync(fee, cancellationToken);
        return createdFee.FeeId;
    }

    public async Task<FeeDto?> GetFeeByIdAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        var fee = await _feeRepo.GetByIdAsync(feeId, cancellationToken);
        if (fee == null)
        {
            return null;
        }

        var feeDto = _mapper.Map<FeeDto>(fee);
        
        // Load payments
        var payments = await _feeRepo.GetPaymentsByFeeIdAsync(feeId, cancellationToken);
        feeDto.Payments = _mapper.Map<List<PaymentDto>>(payments);

        return feeDto;
    }

    public async Task<IEnumerable<FeeDto>> GetAllFeesAsync(CancellationToken cancellationToken = default)
    {
        var fees = await _feeRepo.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<FeeDto>>(fees);
    }

    public async Task<bool> UpdateFeeAsync(Guid feeId, UpdateFeeReq request, CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await _updateFeeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Get existing fee
        var existingFee = await _feeRepo.GetByIdAsync(feeId, cancellationToken);
        if (existingFee == null)
        {
            return false;
        }

        // Business validation
        var businessValidationContext = new UpdateFeeValidationContext
        {
            UpdateRequest = request,
            Fee = existingFee
        };
        var businessValidationResult = await _updateFeeBusinessValidator.ValidateAsync(businessValidationContext, cancellationToken);
        if (!businessValidationResult.IsValid)
        {
            throw new ValidationException(businessValidationResult.Errors);
        }

        // Update fee properties
        existingFee.FeeType = request.FeeType;
        existingFee.Amount = request.Amount;
        existingFee.DueDate = request.DueDate;
        existingFee.Description = request.Description;
        existingFee.UpdatedAt = DateTime.UtcNow;

        // Recalculate status based on new amount
        UpdateFeeStatus(existingFee);

        await _feeRepo.UpdateAsync(existingFee, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteFeeAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        var existingFee = await _feeRepo.GetByIdAsync(feeId, cancellationToken);
        if (existingFee == null)
        {
            return false;
        }

        // Delete the fee - cascade deletion will handle associated payments
        return await _feeRepo.DeleteAsync(feeId, cancellationToken);
    }

    public async Task<IEnumerable<FeeDto>> GetFeesByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var fees = await _feeRepo.GetByStudentIdAsync(studentId, cancellationToken);
        return _mapper.Map<IEnumerable<FeeDto>>(fees);
    }

    public async Task<IEnumerable<FeeDto>> GetFeesByTypeAsync(FeeType feeType, CancellationToken cancellationToken = default)
    {
        var fees = await _feeRepo.GetByFeeTypeAsync(feeType, cancellationToken);
        return _mapper.Map<IEnumerable<FeeDto>>(fees);
    }

    public async Task<IEnumerable<FeeDto>> GetOverdueFeesAsync(CancellationToken cancellationToken = default)
    {
        var fees = await _feeRepo.GetOverdueFeesAsync(cancellationToken);
        return _mapper.Map<IEnumerable<FeeDto>>(fees);
    }

    public async Task<IEnumerable<FeeDto>> GetFeesByStatusAsync(FeeStatus status, CancellationToken cancellationToken = default)
    {
        var fees = await _feeRepo.GetByStatusAsync(status, cancellationToken);
        return _mapper.Map<IEnumerable<FeeDto>>(fees);
    }

    public async Task<PaymentDto> RecordPaymentAsync(Guid feeId, PaymentReq request, CancellationToken cancellationToken = default)
    {
        // Validate payment request
        var validationResult = await _paymentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Get fee
        var fee = await _feeRepo.GetByIdAsync(feeId, cancellationToken);
        if (fee == null)
        {
            throw new FeeNotFoundException(feeId);
        }

        // Business validation
        var businessValidationContext = new PaymentValidationContext
        {
            PaymentRequest = request,
            Fee = fee
        };
        var businessValidationResult = await _paymentBusinessValidator.ValidateAsync(businessValidationContext, cancellationToken);
        if (!businessValidationResult.IsValid)
        {
            throw new ValidationException(businessValidationResult.Errors);
        }

        // Create payment
        var payment = _mapper.Map<Payment>(request);
        payment.PaymentId = Guid.NewGuid();
        payment.FeeId = feeId;
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        // Add payment
        var createdPayment = await _feeRepo.AddPaymentAsync(payment, cancellationToken);

        // Update fee paid amount and status
        fee.PaidAmount += request.Amount;
        UpdateFeeStatus(fee);
        
        if (fee.Status == FeeStatus.Paid)
        {
            fee.IsPaid = true;
            fee.PaidDate = DateTime.UtcNow;
        }

        fee.UpdatedAt = DateTime.UtcNow;
        await _feeRepo.UpdateAsync(fee, cancellationToken);

        return _mapper.Map<PaymentDto>(createdPayment);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByFeeIdAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        // Check if fee exists first
        var feeExists = await _feeRepo.ExistsAsync(feeId, cancellationToken);
        if (!feeExists)
        {
            throw new FeeNotFoundException(feeId);
        }

        var payments = await _feeRepo.GetPaymentsByFeeIdAsync(feeId, cancellationToken);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var payments = await _feeRepo.GetPaymentsByStudentIdAsync(studentId, cancellationToken);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<FeesSummaryDto> GetStudentFeesSummaryAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        // Verify student exists
        var student = await _studentRepo.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            throw new StudentNotFoundException(studentId);
        }

        // Get all fees for student
        var fees = await _feeRepo.GetByStudentIdAsync(studentId, cancellationToken);
        var feesList = fees.ToList();

        // Get recent payments (last 10)
        var allPayments = await _feeRepo.GetPaymentsByStudentIdAsync(studentId, cancellationToken);
        var recentPayments = allPayments
            .OrderByDescending(p => p.PaymentDate)
            .Take(10)
            .ToList();

        // Calculate summary
        var summary = new FeesSummaryDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            TotalFees = feesList.Sum(f => f.Amount),
            TotalPaid = feesList.Sum(f => f.PaidAmount),
            TotalOutstanding = feesList.Sum(f => f.OutstandingAmount),
            TotalOverdue = feesList.Where(f => f.IsOverdue).Sum(f => f.OutstandingAmount),
            TotalFeeCount = feesList.Count,
            PaidFeeCount = feesList.Count(f => f.Status == FeeStatus.Paid),
            OverdueFeeCount = feesList.Count(f => f.IsOverdue),
            PendingFeeCount = feesList.Count(f => f.Status == FeeStatus.Pending),
            Fees = _mapper.Map<List<FeeDto>>(feesList),
            RecentPayments = _mapper.Map<List<PaymentDto>>(recentPayments)
        };

        return summary;
    }

    public async Task<bool> MarkFeeAsPaidAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        var fee = await _feeRepo.GetByIdAsync(feeId, cancellationToken);
        if (fee == null)
        {
            return false;
        }

        fee.PaidAmount = fee.Amount;
        fee.Status = FeeStatus.Paid;
        fee.IsPaid = true;
        fee.PaidDate = DateTime.UtcNow;
        fee.UpdatedAt = DateTime.UtcNow;

        await _feeRepo.UpdateAsync(fee, cancellationToken);
        return true;
    }

    public async Task UpdateFeeStatusesAsync(CancellationToken cancellationToken = default)
    {
        var allFees = await _feeRepo.GetAllAsync(cancellationToken);
        var feesToUpdate = new List<Fee>();

        foreach (var fee in allFees)
        {
            var originalStatus = fee.Status;
            UpdateFeeStatus(fee);
            
            if (fee.Status != originalStatus)
            {
                fee.UpdatedAt = DateTime.UtcNow;
                feesToUpdate.Add(fee);
            }
        }

        // Update fees that had status changes
        foreach (var fee in feesToUpdate)
        {
            await _feeRepo.UpdateAsync(fee, cancellationToken);
        }
    }

    private static void UpdateFeeStatus(Fee fee)
    {
        if (fee.PaidAmount >= fee.Amount)
        {
            fee.Status = FeeStatus.Paid;
            fee.IsPaid = true;
            if (fee.PaidDate == null)
            {
                fee.PaidDate = DateTime.UtcNow;
            }
        }
        else if (fee.PaidAmount > 0)
        {
            fee.Status = fee.IsOverdue ? FeeStatus.Overdue : FeeStatus.PartiallyPaid;
            fee.IsPaid = false;
            fee.PaidDate = null;
        }
        else
        {
            fee.Status = fee.IsOverdue ? FeeStatus.Overdue : FeeStatus.Pending;
            fee.IsPaid = false;
            fee.PaidDate = null;
        }
    }
}