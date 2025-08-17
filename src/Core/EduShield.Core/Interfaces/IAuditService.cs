using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string resource, Guid? userId = null, bool success = true, string? errorMessage = null, string? additionalData = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogAuthenticationAsync(Guid? userId, string action, bool success, string? errorMessage = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogAuthorizationAsync(Guid userId, string resource, string action, bool success, string? errorMessage = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(string eventType, string description, Guid? userId = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task CleanupOldLogsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId, CancellationToken cancellationToken = default);
}