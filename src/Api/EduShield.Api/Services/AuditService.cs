using EduShield.Core.Configuration;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace EduShield.Api.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepo _auditRepo;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditRepo auditRepo,
        IOptions<AuthenticationConfiguration> authConfig,
        ILogger<AuditService> logger)
    {
        _auditRepo = auditRepo;
        _authConfig = authConfig.Value;
        _logger = logger;
    }

    public async Task LogAsync(string action, string resource, Guid? userId = null, bool success = true, string? errorMessage = null, string? additionalData = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (!_authConfig.EnableAuditLogging)
        {
            return;
        }

        try
        {
            var auditLog = new AuditLog
            {
                AuditId = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                Resource = resource,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent ?? "Unknown",
                Success = success,
                ErrorMessage = errorMessage,
                AdditionalData = additionalData
            };

            await _auditRepo.CreateAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action} on resource {Resource}", action, resource);
        }
    }

    public async Task LogAuthenticationAsync(Guid? userId, string action, bool success, string? errorMessage = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        await LogAsync(action, "Authentication", userId, success, errorMessage, null, ipAddress, userAgent, cancellationToken);
    }

    public async Task LogAuthorizationAsync(Guid userId, string resource, string action, bool success, string? errorMessage = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        await LogAsync(action, $"Authorization:{resource}", userId, success, errorMessage, null, ipAddress, userAgent, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _auditRepo.GetLogsAsync(userId, action, fromDate, toDate, skip, take, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        var securityActions = new[] { "Login", "Logout", "TokenValidation", "Authorization", "SessionCreated", "SessionInvalidated" };
        var allLogs = new List<AuditLog>();

        foreach (var action in securityActions)
        {
            var logs = await _auditRepo.GetLogsAsync(null, action, fromDate, toDate, 0, take, cancellationToken);
            allLogs.AddRange(logs);
        }

        return allLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take);
    }

    public async Task CleanupOldLogsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
            var deletedCount = await _auditRepo.DeleteOldLogsAsync(cutoffDate, cancellationToken);
            
            _logger.LogInformation("Cleaned up {Count} audit logs older than {CutoffDate}", deletedCount, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit log cleanup");
        }
    }
}