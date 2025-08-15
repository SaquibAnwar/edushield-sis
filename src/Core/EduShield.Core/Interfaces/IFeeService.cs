using EduShield.Core.Dtos;
using EduShield.Core.Enums;

namespace EduShield.Core.Interfaces;

public interface IFeeService
{
    // Basic CRUD operations
    Task<Guid> CreateFeeAsync(CreateFeeReq request, CancellationToken cancellationToken = default);
    Task<FeeDto?> GetFeeByIdAsync(Guid feeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeeDto>> GetAllFeesAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateFeeAsync(Guid feeId, UpdateFeeReq request, CancellationToken cancellationToken = default);
    Task<bool> DeleteFeeAsync(Guid feeId, CancellationToken cancellationToken = default);
    
    // Query operations
    Task<IEnumerable<FeeDto>> GetFeesByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeeDto>> GetFeesByTypeAsync(FeeType feeType, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeeDto>> GetOverdueFeesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FeeDto>> GetFeesByStatusAsync(FeeStatus status, CancellationToken cancellationToken = default);
    
    // Payment operations
    Task<PaymentDto> RecordPaymentAsync(Guid feeId, PaymentReq request, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetPaymentsByFeeIdAsync(Guid feeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetPaymentsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    
    // Summary and reporting
    Task<FeesSummaryDto> GetStudentFeesSummaryAsync(Guid studentId, CancellationToken cancellationToken = default);
    
    // Business logic operations
    Task<bool> MarkFeeAsPaidAsync(Guid feeId, CancellationToken cancellationToken = default);
    Task UpdateFeeStatusesAsync(CancellationToken cancellationToken = default);
}