using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Core.Interfaces;

public interface IFeeRepo : IBaseRepository<Fee>
{
    // Query methods
    Task<IEnumerable<Fee>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> GetByFeeTypeAsync(FeeType feeType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> GetOverdueFeesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> GetByStatusAsync(FeeStatus status, CancellationToken cancellationToken = default);
    
    // Payment-related methods
    Task<Payment> AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByFeeIdAsync(Guid feeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    
    // Specialized queries
    Task<bool> ExistsAsync(Guid feeId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalOutstandingByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPaidByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
}