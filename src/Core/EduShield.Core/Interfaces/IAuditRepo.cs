using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IAuditRepo
{
    Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetLogsAsync(Guid? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<int> DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}