using EduShield.Core.Interfaces;
using System.Collections.Concurrent;

namespace EduShield.Api.Services;

public interface ISecurityMonitoringService
{
    Task MonitorLoginAttemptAsync(string email, string ipAddress, bool isSuccessful);
    Task MonitorSuspiciousActivityAsync(string eventType, string details, string? userId, string ipAddress);
    Task<bool> IsIpAddressSuspiciousAsync(string ipAddress);
    Task<bool> IsUserSuspiciousAsync(string userId);
    Task<IEnumerable<SecurityAlert>> GetActiveAlertsAsync();
    Task ResolveAlertAsync(Guid alertId, string resolvedBy);
}

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, LoginAttemptTracker> _loginAttempts = new();
    private readonly ConcurrentDictionary<string, SuspiciousActivityTracker> _suspiciousActivities = new();
    private readonly List<SecurityAlert> _activeAlerts = new();
    private readonly Timer _cleanupTimer;

    // Configuration
    private const int MaxFailedLoginAttempts = 5;
    private const int LoginAttemptWindowMinutes = 15;
    private const int SuspiciousActivityThreshold = 10;
    private const int SuspiciousActivityWindowMinutes = 30;

    public SecurityMonitoringService(
        IAuditService auditService,
        ILogger<SecurityMonitoringService> logger)
    {
        _auditService = auditService;
        _logger = logger;

        // Cleanup expired entries every 10 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
            TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public async Task MonitorLoginAttemptAsync(string email, string ipAddress, bool isSuccessful)
    {
        var key = $"{email}:{ipAddress}";
        var tracker = _loginAttempts.GetOrAdd(key, _ => new LoginAttemptTracker());

        lock (tracker)
        {
            var now = DateTime.UtcNow;
            
            // Reset if window has passed
            if (now - tracker.WindowStart > TimeSpan.FromMinutes(LoginAttemptWindowMinutes))
            {
                tracker.FailedAttempts = 0;
                tracker.WindowStart = now;
            }

            if (!isSuccessful)
            {
                tracker.FailedAttempts++;
                tracker.LastFailedAttempt = now;

                if (tracker.FailedAttempts >= MaxFailedLoginAttempts)
                {
                    // Schedule alert creation outside the lock
                    _ = Task.Run(async () =>
                    {
                        await CreateSecurityAlertAsync(
                            "Multiple Failed Login Attempts",
                            $"User {email} has {tracker.FailedAttempts} failed login attempts from IP {ipAddress}",
                            "BruteForce",
                            ipAddress,
                            email);

                        await _auditService.LogSecurityEventAsync(
                            "BruteForceDetected",
                            $"Multiple failed login attempts detected for {email} from {ipAddress}",
                            null,
                            ipAddress,
                            "SecurityMonitoring");
                    });
                }
            }
            else
            {
                // Successful login resets the counter
                tracker.FailedAttempts = 0;
            }
        }
    }

    public async Task MonitorSuspiciousActivityAsync(string eventType, string details, string? userId, string ipAddress)
    {
        var key = $"{eventType}:{ipAddress}:{userId}";
        var tracker = _suspiciousActivities.GetOrAdd(key, _ => new SuspiciousActivityTracker());

        lock (tracker)
        {
            var now = DateTime.UtcNow;
            
            // Reset if window has passed
            if (now - tracker.WindowStart > TimeSpan.FromMinutes(SuspiciousActivityWindowMinutes))
            {
                tracker.EventCount = 0;
                tracker.WindowStart = now;
            }

            tracker.EventCount++;
            tracker.LastEvent = now;

            if (tracker.EventCount >= SuspiciousActivityThreshold)
            {
                // Schedule alert creation outside the lock
                _ = Task.Run(async () =>
                {
                    await CreateSecurityAlertAsync(
                        "Suspicious Activity Detected",
                        $"High frequency of {eventType} events from IP {ipAddress}. Details: {details}",
                        eventType,
                        ipAddress,
                        userId);

                    await _auditService.LogSecurityEventAsync(
                        "SuspiciousActivityDetected",
                        $"High frequency {eventType} events detected from {ipAddress}",
                        userId != null ? Guid.Parse(userId) : null,
                        ipAddress,
                        "SecurityMonitoring");
                });
            }
        }
    }

    public Task<bool> IsIpAddressSuspiciousAsync(string ipAddress)
    {
        var now = DateTime.UtcNow;
        
        // Check for recent failed login attempts
        var hasRecentFailedLogins = _loginAttempts.Values.Any(tracker =>
            tracker.LastFailedAttempt.HasValue &&
            now - tracker.LastFailedAttempt.Value < TimeSpan.FromMinutes(LoginAttemptWindowMinutes) &&
            tracker.FailedAttempts >= MaxFailedLoginAttempts);

        // Check for suspicious activities
        var hasSuspiciousActivity = _suspiciousActivities
            .Where(kvp => kvp.Key.Contains(ipAddress))
            .Any(kvp => now - kvp.Value.LastEvent < TimeSpan.FromMinutes(SuspiciousActivityWindowMinutes) &&
                       kvp.Value.EventCount >= SuspiciousActivityThreshold);

        return Task.FromResult(hasRecentFailedLogins || hasSuspiciousActivity);
    }

    public Task<bool> IsUserSuspiciousAsync(string userId)
    {
        var now = DateTime.UtcNow;
        
        // Check for suspicious activities related to this user
        var hasSuspiciousActivity = _suspiciousActivities
            .Where(kvp => kvp.Key.Contains(userId))
            .Any(kvp => now - kvp.Value.LastEvent < TimeSpan.FromMinutes(SuspiciousActivityWindowMinutes) &&
                       kvp.Value.EventCount >= SuspiciousActivityThreshold);

        return Task.FromResult(hasSuspiciousActivity);
    }

    public Task<IEnumerable<SecurityAlert>> GetActiveAlertsAsync()
    {
        lock (_activeAlerts)
        {
            return Task.FromResult(_activeAlerts.Where(alert => !alert.IsResolved).AsEnumerable());
        }
    }

    public Task ResolveAlertAsync(Guid alertId, string resolvedBy)
    {
        lock (_activeAlerts)
        {
            var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedBy = resolvedBy;

                _logger.LogInformation("Security alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);
            }
        }

        return Task.CompletedTask;
    }

    private async Task CreateSecurityAlertAsync(string title, string description, string alertType, string ipAddress, string? userId)
    {
        var alert = new SecurityAlert
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            AlertType = alertType,
            IpAddress = ipAddress,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Severity = GetAlertSeverity(alertType),
            IsResolved = false
        };

        lock (_activeAlerts)
        {
            _activeAlerts.Add(alert);
        }

        _logger.LogWarning("Security alert created: {Title} - {Description}", title, description);

        // Log to audit service
        await _auditService.LogSecurityEventAsync(
            "SecurityAlertCreated",
            $"Security alert: {title}",
            userId != null ? Guid.Parse(userId) : null,
            ipAddress,
            "SecurityMonitoring");
    }

    private static string GetAlertSeverity(string alertType)
    {
        return alertType switch
        {
            "BruteForce" => "High",
            "SuspiciousActivity" => "Medium",
            "UnauthorizedAccess" => "High",
            "DataBreach" => "Critical",
            _ => "Low"
        };
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredLoginKeys = new List<string>();
        var expiredActivityKeys = new List<string>();

        // Cleanup login attempts
        foreach (var kvp in _loginAttempts)
        {
            var tracker = kvp.Value;
            if (now - tracker.WindowStart > TimeSpan.FromHours(1))
            {
                expiredLoginKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredLoginKeys)
        {
            _loginAttempts.TryRemove(key, out _);
        }

        // Cleanup suspicious activities
        foreach (var kvp in _suspiciousActivities)
        {
            var tracker = kvp.Value;
            if (now - tracker.WindowStart > TimeSpan.FromHours(1))
            {
                expiredActivityKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredActivityKeys)
        {
            _suspiciousActivities.TryRemove(key, out _);
        }

        // Cleanup old resolved alerts
        lock (_activeAlerts)
        {
            var oldAlerts = _activeAlerts
                .Where(alert => alert.IsResolved && 
                               alert.ResolvedAt.HasValue && 
                               now - alert.ResolvedAt.Value > TimeSpan.FromDays(30))
                .ToList();

            foreach (var alert in oldAlerts)
            {
                _activeAlerts.Remove(alert);
            }
        }

        if (expiredLoginKeys.Count > 0 || expiredActivityKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {LoginCount} login trackers and {ActivityCount} activity trackers",
                expiredLoginKeys.Count, expiredActivityKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private class LoginAttemptTracker
    {
        public int FailedAttempts { get; set; }
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public DateTime? LastFailedAttempt { get; set; }
    }

    private class SuspiciousActivityTracker
    {
        public int EventCount { get; set; }
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public DateTime LastEvent { get; set; } = DateTime.UtcNow;
    }
}

public class SecurityAlert
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}