using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class FeeRepo : IFeeRepo
{
    private readonly EduShieldDbContext _context;

    public FeeRepo(EduShieldDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Fee> CreateAsync(Fee fee, CancellationToken cancellationToken = default)
    {
        if (fee == null)
            throw new ArgumentNullException(nameof(fee));

        _context.Fees.Add(fee);
        await _context.SaveChangesAsync(cancellationToken);
        return fee;
    }

    public async Task<Fee?> GetByIdAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.FeeId == feeId, cancellationToken);
    }

    public async Task<IEnumerable<Fee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .ToListAsync(cancellationToken);
    }

    public async Task<Fee> UpdateAsync(Fee fee, CancellationToken cancellationToken = default)
    {
        if (fee == null)
            throw new ArgumentNullException(nameof(fee));

        _context.Fees.Update(fee);
        await _context.SaveChangesAsync(cancellationToken);
        return fee;
    }

    public async Task<bool> DeleteAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        var fee = await _context.Fees.FindAsync(new object[] { feeId }, cancellationToken);
        if (fee == null)
            return false;

        _context.Fees.Remove(fee);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Fee>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .Where(f => f.StudentId == studentId)
            .OrderBy(f => f.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Fee>> GetByFeeTypeAsync(FeeType feeType, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .Where(f => f.FeeType == feeType)
            .OrderBy(f => f.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Fee>> GetOverdueFeesAsync(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow;
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .Where(f => f.DueDate < currentDate && f.PaidAmount < f.Amount)
            .OrderBy(f => f.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Fee>> GetByStatusAsync(FeeStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Include(f => f.Student)
            .Include(f => f.Payments)
            .Where(f => f.Status == status)
            .OrderBy(f => f.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment> AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        // Verify the fee exists
        var fee = await _context.Fees.FindAsync(new object[] { payment.FeeId }, cancellationToken);
        if (fee == null)
            throw new InvalidOperationException($"Fee with ID {payment.FeeId} not found");

        // Validate payment amount doesn't exceed outstanding amount
        var outstandingAmount = fee.Amount - fee.PaidAmount;
        if (payment.Amount > outstandingAmount)
            throw new InvalidOperationException($"Payment amount {payment.Amount:C} exceeds outstanding amount {outstandingAmount:C}");

        // Add payment
        _context.Payments.Add(payment);
        
        // Update fee paid amount and status
        fee.PaidAmount += payment.Amount;
        
        if (fee.PaidAmount >= fee.Amount)
        {
            fee.Status = FeeStatus.Paid;
            fee.IsPaid = true;
            fee.PaidDate = payment.PaymentDate;
        }
        else if (fee.PaidAmount > 0)
        {
            fee.Status = FeeStatus.PartiallyPaid;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByFeeIdAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Fee)
            .Where(p => p.FeeId == feeId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Fee)
                .ThenInclude(f => f!.Student)
            .Where(p => p.Fee!.StudentId == studentId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid feeId, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .AnyAsync(f => f.FeeId == feeId, cancellationToken);
    }

    public async Task<decimal> GetTotalOutstandingByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Where(f => f.StudentId == studentId)
            .SumAsync(f => f.Amount - f.PaidAmount, cancellationToken);
    }

    public async Task<decimal> GetTotalPaidByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Fees
            .Where(f => f.StudentId == studentId)
            .SumAsync(f => f.PaidAmount, cancellationToken);
    }
}