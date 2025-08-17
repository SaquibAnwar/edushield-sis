using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IAuditRepo
{
    Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetLogsAsync(Guid? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<int> DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByActionsAsync(string[] actions, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
}